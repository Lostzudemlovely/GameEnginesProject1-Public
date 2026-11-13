// Game Engines – Projekt 1
// Dreht nur das sichtbare Ballmodell anhand der zurückgelegten Strecke.
using UnityEngine;

public sealed class FootballVisual : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float radius = 0.5f;

    private Vector3 previousPosition;

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 movement = transform.position - previousPosition;
        previousPosition = transform.position;
        movement.y = 0f;
        float distance = movement.magnitude;
        // Große Positionswechsel beim Respawn sollen den Ball nicht drehen.
        if (distance < 0.0001f || distance > Mathf.Max(1f, 12f * Time.deltaTime)) return;

        transform.Rotate(Vector3.Cross(Vector3.up, movement), distance / radius * Mathf.Rad2Deg, Space.World);
    }
}
