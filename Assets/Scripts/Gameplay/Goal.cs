// Game Engines – Projekt 1
// Meldet den Eintritt des Players in den Zieltrigger an den GameManager.
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class Goal : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
    }

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // attachedRigidbody ordnet auch einen untergeordneten Collider dem Player-Körper zu.
        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out PlayerController player))
        {
            gameManager.CompleteLevel(player);
        }
    }
}
