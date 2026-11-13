// Game Engines – Projekt 1
// Koordiniert Laufzustand, Levelwechsel, Wertung und Neustart des Endless-Parcours.
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { MainMenu, Playing, Paused, GameOver }

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private HighscoreManager highscore;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private CameraController followCamera;
    [SerializeField] private GameFeedback feedback;
    [SerializeField] private KickoffFoot kickoffFoot;
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    public float ElapsedTime { get; private set; }
    public int CompletedLevels { get; private set; }
    public int CurrentLevel => CompletedLevels + 1;
    public string CourseName => levelGenerator.CourseName;
    public bool IsWaitingForKickoff => player.IsWaitingForKickoff;
    public bool IsKickoffFlight => player.IsKickoffFlight;
    public bool RequiresKickoffSteering => levelGenerator.RequiresKickoffSteering;
    public GameState State { get; private set; } = GameState.MainMenu;
    public bool IsGameOver => State == GameState.GameOver;
    public bool IsNewBest { get; private set; }
    public int BestScore => highscore.BestScore;

    private bool nextLevelPending;

    private void OnEnable()
    {
        player.Fell += EndRun;
        player.Jumped += OnJumped;
        player.KickedOff += OnKickedOff;
    }

    private void OnDisable()
    {
        player.Fell -= EndRun;
        player.Jumped -= OnJumped;
        player.KickedOff -= OnKickedOff;
    }

    private void Start()
    {
        RestartRun();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing) PauseRun();
            else if (State == GameState.Paused) ResumeRun();
            return;
        }

        if (Input.GetKeyDown(restartKey) && (State == GameState.Playing || IsGameOver))
        {
            RestartRun();
            return;
        }

        if (State == GameState.Playing)
        {
            ElapsedTime += Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (nextLevelPending && State == GameState.Playing)
        {
            nextLevelPending = false;
            StartLevel();
        }
    }

    public void CompleteLevel(PlayerController finisher)
    {
        if (State != GameState.Playing || nextLevelPending || finisher != player)
        {
            return;
        }

        CompletedLevels++;
        // Ein einmal erreichter neuer Rekord bleibt bis zum Ende dieses Laufs markiert.
        IsNewBest |= highscore.TrySaveBest(CompletedLevels);
        player.SetMovementEnabled(false);
        AudioManager.Instance?.PlayGoal();
        // Erst außerhalb des Physik-Callbacks alte Collider entfernen und neu bauen.
        nextLevelPending = true;
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.SetPaused(false);
        AudioManager.Instance?.StopEffects();
        nextLevelPending = false;
        ElapsedTime = 0f;
        CompletedLevels = 0;
        State = GameState.Playing;
        IsNewBest = false;
        feedback.ResetFeedback();
        levelGenerator.BeginRun();
        StartLevel();
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.SetPaused(false);
        AudioManager.Instance?.StopEffects();
        State = GameState.MainMenu;
        nextLevelPending = false;
        player.SetMovementEnabled(false);
        SceneManager.LoadScene("MainMenu");
    }

    public void PauseRun()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        player.SetInputEnabled(false);
        Time.timeScale = 0f;
        AudioManager.Instance?.SetPaused(true);
    }

    public void ResumeRun()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        player.SetInputEnabled(true);
        Time.timeScale = 1f;
        AudioManager.Instance?.SetPaused(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) PauseRun();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void StartLevel()
    {
        player.SetMovementEnabled(false);
        levelGenerator.BuildLevel(CurrentLevel, this);
        player.Respawn();
        player.SetMovementEnabled(true);
        float delay = CurrentLevel == 1 ? 1f : 0.65f;
        player.PrepareKickoff(levelGenerator.KickoffTarget, delay, RequiresKickoffSteering);
        kickoffFoot.Prepare(player, levelGenerator.KickoffTarget, delay);
        followCamera.SnapToTarget();
        if (CompletedLevels > 0) feedback.PlayGoal(player.transform.position);
    }

    private void EndRun()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        nextLevelPending = false;
        player.SetMovementEnabled(false);
        kickoffFoot.Hide();
        feedback.ShowGameOver();
        AudioManager.Instance?.PlayDeath();
    }

    private void OnJumped(Vector3 position)
    {
        if (State != GameState.Playing) return;
        feedback.PlayJump(position);
        AudioManager.Instance?.PlayJump();
    }

    private void OnKickedOff(Vector3 position)
    {
        kickoffFoot.Strike(position);
        feedback.PlayJump(position);
        AudioManager.Instance?.PlayKickoff();
    }
}
