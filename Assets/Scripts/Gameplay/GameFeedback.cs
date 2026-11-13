// Game Engines – Projekt 1
// Verknüpft Sprung, Ziel und Verlust mit Partikeln sowie Bloom und Vignette.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class GameFeedback : MonoBehaviour
{
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem goalParticles;
    [SerializeField, Min(0f)] private float baseBloom = 0.18f;
    [SerializeField, Min(0f)] private float goalBloomBoost = 1.2f;
    [SerializeField, Min(0.1f)] private float flashFadeSpeed = 2.5f;
    [SerializeField, Range(0f, 1f)] private float baseVignette = 0.12f;
    [SerializeField, Range(0f, 1f)] private float lossVignette = 0.48f;

    private VolumeProfile runtimeProfile;
    private Bloom bloom;
    private Vignette vignette;
    private float goalFlash;
    private bool gameOver;

    private void Awake()
    {
        // Eigene Laufzeitkopie: Der Effekt verändert das gespeicherte Asset nicht.
        runtimeProfile = postProcessVolume.profile;
        runtimeProfile.TryGet(out bloom);
        runtimeProfile.TryGet(out vignette);
    }

    private void Update()
    {
        goalFlash = Mathf.MoveTowards(goalFlash, 0f, Time.deltaTime * flashFadeSpeed);
        bloom.intensity.value = baseBloom + goalFlash * goalBloomBoost;
        vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value,
            gameOver ? lossVignette : baseVignette, Time.deltaTime * 1.5f);
    }

    public void PlayJump(Vector3 position)
    {
        PlayAt(jumpParticles, position + Vector3.down * 0.45f);
    }

    public void PlayGoal(Vector3 position)
    {
        goalFlash = 1f;
        // Der Levelwechsel ist sofort: Die Feier erscheint sichtbar am neuen Spawn.
        PlayAt(goalParticles, position);
    }

    public void ShowGameOver()
    {
        gameOver = true;
    }

    public void ResetFeedback()
    {
        goalFlash = 0f;
        gameOver = false;
        bloom.intensity.value = baseBloom;
        vignette.intensity.value = baseVignette;
        jumpParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void PlayAt(ParticleSystem particles, Vector3 position)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.position = position;
        particles.Play();
    }

    private void OnDestroy()
    {
        if (runtimeProfile != null) Destroy(runtimeProfile);
    }
}
