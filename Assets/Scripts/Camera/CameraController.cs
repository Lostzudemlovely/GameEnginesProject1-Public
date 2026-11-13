// Game Engines – Projekt 1
// Folgt dem Player geglättet und richtet die Kamera nach einem Respawn sofort aus.
using UnityEngine;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -9f);
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;
    [SerializeField] private float lookHeight = 0.3f;
    [Tooltip("Große Positionswechsel wie einen Respawn sofort nachziehen.")]
    [SerializeField, Min(1f)] private float snapDistance = 12f;

    private Vector3 followVelocity;

    private void Start()
    {
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + offset;
        followVelocity = Vector3.zero;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        if (Vector3.Distance(transform.position, desiredPosition) > snapDistance)
        {
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
                ref followVelocity, followSmoothTime);
        }

        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
