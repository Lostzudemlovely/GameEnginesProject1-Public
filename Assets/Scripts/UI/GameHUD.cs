// Game Engines – Projekt 1
// Zeigt Laufzeit, Fortschritt, Rekord und Ergebnis aus dem GameManager an.
using UnityEngine;
using UnityEngine.UI;

public sealed class GameHUD : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Text currentTimeText;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text progressText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultScoreText;
    [SerializeField] private Text resultStatusText;
    [SerializeField] private Text resultSummaryText;

    private void LateUpdate()
    {
        currentTimeText.text = FormatTime(gameManager.ElapsedTime);
        bestScoreText.text = gameManager.BestScore.ToString();
        levelText.text = "LEVEL " + gameManager.CurrentLevel;
        if (gameManager.IsWaitingForKickoff)
            progressText.text = gameManager.RequiresKickoffSteering
                ? "ANSTOSS  ·  Wähle gleich eine Landefläche"
                : "ANSTOSS  ·  Gleich geht's los";
        else if (gameManager.IsKickoffFlight)
            progressText.text = gameManager.RequiresKickoffSteering
                ? "A / D oder Pfeile  ·  Landefläche wählen"
                : "WASD / Pfeile  ·  Landefläche ansteuern";
        else
            progressText.text = gameManager.CourseName + "  ·  Geschafft: " + gameManager.CompletedLevels;

        if (resultPanel.activeSelf != gameManager.IsGameOver)
        {
            resultPanel.SetActive(gameManager.IsGameOver);
        }

        if (gameManager.IsGameOver)
        {
            resultScoreText.text = gameManager.CompletedLevels.ToString();
            resultStatusText.text = gameManager.IsNewBest ? "Neuer Rekord!" : "Versuch es noch einmal";
            resultSummaryText.text = "Rekord: " + gameManager.BestScore + " Level  ·  Zeit: " + FormatTime(gameManager.ElapsedTime);
        }
    }

    private static string FormatTime(float time)
    {
        int hundredths = Mathf.FloorToInt(time * 100f);
        return $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
    }
}
