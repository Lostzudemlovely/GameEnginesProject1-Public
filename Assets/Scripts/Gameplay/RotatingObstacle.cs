// Game Engines – Projekt 1
// Dreht einen kinematischen Hinderniskörper mit vorzeichenabhängiger Drehrichtung.
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class RotatingObstacle : MonoBehaviour
{
    [Tooltip("Grad pro Sekunde. Ein negativer Wert kehrt die Drehrichtung um.")]
    [SerializeField] private float rotationSpeed = 60f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void SetSpeed(float degreesPerSecond)
    {
        rotationSpeed = degreesPerSecond;
    }

    private void FixedUpdate()
    {
        Quaternion turn = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, Vector3.up);
        body.MoveRotation(turn * body.rotation);
    }

}
