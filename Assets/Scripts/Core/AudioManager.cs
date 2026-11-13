// Game Engines – Projekt 1
// Verwaltet Musik und Spielsounds über eine persistente Singleton-Instanz.
using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public bool IsMuted { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip kickoffSound;
    [SerializeField] private AudioClip goalSound;
    [SerializeField] private AudioClip deathSound;

    private void Awake()
    {
        // Ein Duplikat darf weder Quellen initialisieren noch die bestehende Instanz ersetzen.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        musicSource.Play();
    }

    public void PlayJump() => effectsSource.PlayOneShot(jumpSound);
    public void PlayKickoff() => effectsSource.PlayOneShot(kickoffSound);
    public void PlayGoal() => effectsSource.PlayOneShot(goalSound);
    public void PlayDeath() => effectsSource.PlayOneShot(deathSound);
    public void StopEffects() => effectsSource.Stop();

    public void SetPaused(bool paused)
    {
        // Time.timeScale hält die Physik an; AudioSources werden zusätzlich pausiert.
        if (paused)
        {
            musicSource.Pause();
            effectsSource.Pause();
        }
        else
        {
            musicSource.UnPause();
            effectsSource.UnPause();
        }
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        musicSource.mute = IsMuted;
        effectsSource.mute = IsMuted;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
