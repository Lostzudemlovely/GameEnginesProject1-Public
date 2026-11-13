// Game Engines – Projekt 1
// Bewegt eine kinematische Plattform sinusförmig um ihre Startposition.
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 travelOffset = Vector3.right;
    [SerializeField, Min(0.5f)] private float cycleDuration = 4f;
    private Rigidbody body;
    private Vector3 origin;
    private float elapsed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        origin = body.position;
    }

    public void ConfigureMotion(Vector3 offset, float duration)
    {
        travelOffset = offset;
        cycleDuration = Mathf.Max(0.5f, duration);
    }

    private void FixedUpdate()
    {
        elapsed = Mathf.Repeat(elapsed + Time.fixedDeltaTime, cycleDuration);
        float wave = Mathf.Sin(elapsed / cycleDuration * Mathf.PI * 2f);
        // MovePosition bewegt den Rigidbody im Physiktakt, damit Kontakte mitgeführt werden.
        body.MovePosition(origin + travelOffset * wave);
    }
}
