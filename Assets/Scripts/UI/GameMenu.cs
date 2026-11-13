// Game Engines – Projekt 1
// Zeigt das Pausemenü im Spiel und bedient dessen Tonschalter.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GameMenu : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Text pauseAudioText;

    private GameState lastState = (GameState)(-1);

    private void LateUpdate()
    {
        GameState state = gameManager.State;
        if (state != lastState)
        {
            // Panels und Tastaturfokus werden nur beim Zustandswechsel umgeschaltet.
            pauseMenu.SetActive(state == GameState.Paused);
            if (state == GameState.Paused) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            else EventSystem.current.SetSelectedGameObject(null);
            lastState = state;
        }
        string audioLabel = AudioManager.Instance.IsMuted ? "Ton: Aus" : "Ton: An";
        pauseAudioText.text = audioLabel;
    }

    public void ToggleAudio()
    {
        AudioManager.Instance.ToggleMute();
    }
}
