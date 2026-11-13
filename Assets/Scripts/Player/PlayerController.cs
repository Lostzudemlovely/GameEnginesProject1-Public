// Game Engines – Projekt 1
// Steuert Physikbewegung, variable Sprunghöhe, Bodenprüfung und Sturzmeldung.
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Bewegung")]
    [SerializeField, Min(0f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.1f)] private float moveAcceleration = 30f;

    [Header("Sprung und Boden")]
    [SerializeField, Min(0f)] private float jumpForce = 6f;
    [Tooltip("Restliche Aufwärtsgeschwindigkeit beim frühen Loslassen. 1 = unverändert.")]
    [SerializeField, Range(0.1f, 1f)] private float jumpReleaseMultiplier = 0.5f;
    [Tooltip("Nur begehbare Flächen auswählen, nicht den Player.")]
    [SerializeField] private LayerMask groundLayers;
    [Tooltip("Die Bodensuche beginnt in der Kugelmitte. Kleiner als der Player-Radius halten.")]
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.4f;
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.15f;
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 50f;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallRespawnHeight = -5f;

    // Verhindert einen zweiten Sprung, solange die Kugel gerade nach oben startet.
    private const float MaxGroundedUpwardSpeed = 0.1f;
    private const float KickoffFlightTime = 1.5f;
    private const float KickoffSteeringSpeed = 3.8f;
    private const float KickoffSteeringAcceleration = 14f;

    private Rigidbody body;
    private Vector2 movementInput;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool canCutJump;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool movementEnabled = true;
    private bool inputEnabled = true;
    private CollisionDetectionMode movementCollisionMode;
    private float kickoffCountdown;
    private Vector3 kickoffTarget;
    private Vector3 kickoffTravelVelocity;
    private bool kickoffRequiresSteering;

    public event Action Fell;
    public event Action<Vector3> Jumped;
    public event Action<Vector3> KickedOff;
    public float KickoffCountdown => kickoffCountdown;
    public bool IsWaitingForKickoff => KickoffCountdown > 0f;
    public bool IsKickoffFlight { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        movementCollisionMode = body.collisionDetectionMode;
        startingPosition = body.position;
        startingRotation = body.rotation;
    }

    private void Update()
    {
        if (!movementEnabled || !inputEnabled)
        {
            return;
        }

        movementInput = Vector2.ClampMagnitude(new Vector2(
            Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        jumpHeld = Input.GetButton("Jump");

        if (Input.GetButtonDown("Jump"))
        {
            // Die Anfrage bleibt bis zum nächsten Physikschritt erhalten.
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            return;
        }

        if (body.position.y < fallRespawnHeight)
        {
            SetMovementEnabled(false);
            Fell?.Invoke();
            return;
        }

        if (IsWaitingForKickoff)
        {
            kickoffCountdown -= Time.fixedDeltaTime;
            jumpRequested = false;
            if (!IsWaitingForKickoff) LaunchKickoff();
            return;
        }

        if (IsKickoffFlight)
        {
            IsKickoffFlight = !IsGrounded();
            if (IsKickoffFlight)
            {
                SteerKickoff();
                jumpRequested = false;
                return;
            }
        }

        Move();
        HandleJump();

        // Ein Druck gilt nur für diesen Physikschritt, auch wenn er in der Luft erfolgte.
        jumpRequested = false;
    }

    public void PrepareKickoff(Vector3 landingPoint, float delay, bool requiresSteering)
    {
        kickoffTarget = landingPoint;
        kickoffCountdown = Mathf.Max(Time.fixedDeltaTime, delay);
        kickoffRequiresSteering = requiresSteering;
    }

    private void LaunchKickoff()
    {
        float radius = GetComponent<SphereCollider>().radius * Mathf.Abs(transform.lossyScale.y);
        Vector3 travel = kickoffTarget + Vector3.up * radius - body.position;
        kickoffTravelVelocity = new Vector3(travel.x, 0f, travel.z) / KickoffFlightTime;
        // Der Impuls zielt auf die Plattform. Danach bestimmen Physik und Eingaben den Flug.
        float lift = travel.y / KickoffFlightTime - Physics.gravity.y * (KickoffFlightTime + Time.fixedDeltaTime) / 2f;
        body.linearVelocity = kickoffTravelVelocity + Vector3.up * lift;
        IsKickoffFlight = true;
        KickedOff?.Invoke(body.position);
    }

    private void SteerKickoff()
    {
        Vector3 horizontal = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        float sideSpeed = kickoffRequiresSteering ? 1.9f : KickoffSteeringSpeed;
        float depthSpeed = kickoffRequiresSteering ? 1.25f : KickoffSteeringSpeed;
        Vector3 steering = new Vector3(movementInput.x * sideSpeed, 0f, movementInput.y * depthSpeed);
        Vector3 acceleration = (kickoffTravelVelocity + steering - horizontal) / Time.fixedDeltaTime;
        body.AddForce(Vector3.ClampMagnitude(acceleration, KickoffSteeringAcceleration), ForceMode.Acceleration);
    }

    private void HandleJump()
    {
        if (jumpRequested && IsGrounded())
        {
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            body.linearVelocity = velocity;
            // Auch ein kompletter kurzer Tastendruck zwischen zwei Physikschritten zählt.
            float impulse = jumpHeld ? jumpForce : jumpForce * jumpReleaseMultiplier;
            body.AddForce(Vector3.up * impulse, ForceMode.Impulse);
            canCutJump = jumpHeld;
            Jumped?.Invoke(body.position);
        }
        else if (canCutJump && (!jumpHeld || body.linearVelocity.y <= 0f))
        {
            Vector3 velocity = body.linearVelocity;
            if (velocity.y > 0f)
            {
                velocity.y *= jumpReleaseMultiplier;
                body.linearVelocity = velocity;
            }
            // Nur einmal kürzen; Fallen und horizontale Bewegung bleiben unverändert.
            canCutJump = false;
        }
    }

    private void Move()
    {
        Vector3 targetVelocity = new Vector3(movementInput.x, 0f, movementInput.y) * moveSpeed;
        if (TryGetGround(out RaycastHit ground) && ground.rigidbody != null)
        {
            // Eingabe ist relativ zum Boden; kinematische Plattformen nehmen die Kugel mit.
            Vector3 supportVelocity = ground.rigidbody.GetPointVelocity(ground.point);
            targetVelocity += new Vector3(supportVelocity.x, 0f, supportVelocity.z);
        }
        Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        Vector3 acceleration = (targetVelocity - horizontalVelocity) / Time.fixedDeltaTime;

        body.AddForce(Vector3.ClampMagnitude(acceleration, moveAcceleration), ForceMode.Acceleration);
    }

    private bool IsGrounded()
    {
        return TryGetGround(out _);
    }

    private bool TryGetGround(out RaycastHit hit)
    {
        hit = default;
        if (body.linearVelocity.y > MaxGroundedUpwardSpeed)
        {
            return false;
        }

        bool hitGround = Physics.SphereCast(body.position, groundCheckRadius, Vector3.down,
            out hit, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);

        return hitGround && Vector3.Angle(hit.normal, Vector3.up) <= maxGroundAngle;
    }

    public void SetMovementEnabled(bool value)
    {
        movementEnabled = value;
        inputEnabled = value;
        movementInput = Vector2.zero;
        jumpRequested = false;
        jumpHeld = false;
        canCutJump = false;
        kickoffCountdown = 0f;
        IsKickoffFlight = false;
        if (value)
        {
            body.isKinematic = false;
            body.collisionDetectionMode = movementCollisionMode;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
        else
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
        }
    }

    public void Respawn()
    {
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.position = respawnPoint != null ? respawnPoint.position : startingPosition;
        body.rotation = respawnPoint != null ? respawnPoint.rotation : startingRotation;
        // Beim Teleport muss auch die sichtbare, sonst interpolierte Pose sofort stimmen.
        transform.SetPositionAndRotation(body.position, body.rotation);
        Physics.SyncTransforms();
        movementInput = Vector2.zero;
        jumpRequested = false;
        jumpHeld = false;
        canCutJump = false;
        kickoffCountdown = 0f;
        IsKickoffFlight = false;
        body.WakeUp();
    }

    public void SetInputEnabled(bool value)
    {
        // Pause sperrt nur Eingaben. Time.timeScale hält Physik samt Fluggeschwindigkeit an.
        inputEnabled = value;
        movementInput = Vector2.zero;
        jumpRequested = false;
        if (value) jumpHeld = Input.GetButton("Jump");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 probeEnd = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawLine(transform.position, probeEnd);
        Gizmos.DrawWireSphere(probeEnd, groundCheckRadius);
    }
}
