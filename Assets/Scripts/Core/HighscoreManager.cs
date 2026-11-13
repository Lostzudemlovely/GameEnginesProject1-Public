// Game Engines – Projekt 1
// Lädt und speichert die höchste Anzahl vollständig abgeschlossener Level.
using UnityEngine;

public sealed class HighscoreManager : MonoBehaviour
{
    [Tooltip("Endless-Rekord: höchste Zahl vollständig geschaffter Level.")]
    [SerializeField] private string playerPrefsKey = "GameEnginesProject1.Endless.Variety.CompletedLevels.v1";

    public int BestScore { get; private set; }

    private void Awake()
    {
        BestScore = Mathf.Max(0, PlayerPrefs.GetInt(playerPrefsKey, 0));
    }

    public bool TrySaveBest(int score)
    {
        // Gewertet wird die Levelanzahl: Nur ein größerer Wert verbessert den Rekord.
        if (score <= BestScore)
        {
            return false;
        }

        BestScore = score;
        PlayerPrefs.SetInt(playerPrefsKey, BestScore);
        // Schon am Ziel sichern, damit auch ein abgebrochener Lauf seinen Rekord behält.
        PlayerPrefs.Save();
        return true;
    }

}
