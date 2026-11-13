// Game Engines – Projekt 1
// Bedient die eigene Hauptmenüszene und startet die Spielszene.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private HighscoreManager highscore;
    [SerializeField] private Button playButton;
    [SerializeField] private Text recordText;
    [SerializeField] private Text audioText;

    private void Start()
    {
        Time.timeScale = 1f;
        recordText.text = "DEIN REKORD: " + highscore.BestScore + " LEVEL";
        EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    private void LateUpdate()
    {
        audioText.text = AudioManager.Instance.IsMuted ? "Ton: Aus" : "Ton: An";
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void ToggleAudio()
    {
        AudioManager.Instance.ToggleMute();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
