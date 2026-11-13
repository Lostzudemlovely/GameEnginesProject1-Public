// Game Engines – Projekt 1
// Editor-Prüfung der Abgabefunktionen; wird nur in eine separate Prüfkopie eingebunden.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SubmissionChecks
{
    private const string Active = "Submission.QA.Active";
    private const string Passed = "Submission.QA.Passed";
    private const string TestKey = "GameEnginesProject1.QA.Submission.20261027";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static IEnumerator checks;
    private static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    private static readonly List<string> results = new List<string>();
    private static int errors;

    static SubmissionChecks()
    {
        EditorApplication.update += Tick;
        SceneManager.sceneLoaded += (scene, mode) => {
            if (!SessionState.GetBool(Active, false)) return;
            // Jede Szene liest/schreibt nur den separaten QA-Rekord, niemals den Spielstand.
            foreach (HighscoreManager score in Object.FindObjectsByType<HighscoreManager>(FindObjectsSortMode.None))
            {
                Set(score, "playerPrefsKey", TestKey);
                Call(score, "Awake");
            }
        };
        EditorApplication.playModeStateChanged += state => {
            if (!SessionState.GetBool(Active, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) checks = CheckPlay();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Active, false);
                EditorApplication.Exit(SessionState.GetBool(Passed, false) ? 0 : 1);
            }
        };
        Application.logMessageReceived += (message, trace, type) => {
            if (SessionState.GetBool(Active, false) &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) errors++;
        };
    }

    public static void Validate()
    {
        Directory.CreateDirectory("Logs");
        PhysicsChecks.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Require(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(
            new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" }), "Build scene order invalid.");
        Require(!ShaderUtil.ShaderHasError(Shader.Find("Project1/PulseGlow")), "Own shader compilation failed.");
        PlayerPrefs.DeleteKey(TestKey); PlayerPrefs.Save();
        SessionState.SetBool(Active, true); SessionState.SetBool(Passed, false);
        EditorApplication.EnterPlaymode();
    }

    private static IEnumerator CheckPlay()
    {
        errors = 0; results.Clear();
        yield return Frames(5);
        Require(SceneManager.GetActiveScene().name == "MainMenu" && Object.FindFirstObjectByType<GameManager>() == null,
            "Menu is not a separate scene without gameplay.");
        CheckBinding("Play Button", "PlayGame"); CheckBinding("Exit Button", "QuitGame");
        var audio = AudioManager.Instance;
        var music = (AudioSource)Get(audio, "musicSource");
        var sounds = (AudioSource)Get(audio, "effectsSource");
        Require(music.clip.length >= 31.9f && music.loop, "Music clip/loop missing.");
        yield return Until(() => music.isPlaying && music.timeSamples > 0, 3, "Music did not start.");
        int sample = music.timeSamples;
        yield return Seconds(0.25f);
        Require(music.timeSamples > sample, "Music transport did not advance.");
        var duplicate = new GameObject("QA Duplicate Audio").AddComponent<AudioManager>();
        yield return Frames(3);
        Require(duplicate == null, "Audio duplicate survived.");
        CheckAudio(audio);
        Capture("MainMenu", 1280, 720); Capture("MainMenuSmall", 960, 540);
        Pass("Separate MainMenu scene; Play/Exit bindings; looping music; one persistent audio singleton.");

        Click("Play Button"); yield return Scene("Game");
        var manager = Object.FindFirstObjectByType<GameManager>();
        var player = Object.FindFirstObjectByType<PlayerController>();
        var generator = Object.FindFirstObjectByType<LevelGenerator>();
        var feedback = Object.FindFirstObjectByType<GameFeedback>();
        var foot = Object.FindFirstObjectByType<KickoffFoot>();
        var body = player.GetComponent<Rigidbody>();
        Require(manager.State == GameState.Playing && !body.isKinematic && manager.CurrentLevel == 1,
            "Game scene did not start a new run.");
        CheckAudio(audio);
        Require(Get(audio, "kickoffSound") is AudioClip, "Kickoff sound is not assigned.");
        Require(foot.IsVisible && GameObject.Find("Boot Upper").GetComponent<MeshRenderer>().enabled,
            "Football boot is not visible during the windup.");
        Require(GameObject.Find("Kickoff Foot").GetComponentsInChildren<Collider>().Length == 0,
            "Football boot can interfere with the ball's physics.");
        Capture("KickoffWindup", 1280, 720);
        yield return Until(() => player.IsKickoffFlight && sounds.isPlaying, 4, "Kickoff sound did not play.");
        yield return Until(() => player.IsKickoffFlight && body.position.y > 1.8f, 4, "Automatic kickoff did not launch.");
        Require(GameObject.Find("Level Progress").GetComponent<Text>().text.Contains("Landefläche"),
            "The kickoff steering hint is missing.");
        Capture("Gameplay", 1280, 720);
        manager.PauseRun(); yield return Frames(3);
        float pausedClock = manager.ElapsedTime;
        Vector3 pausedPosition = body.position, pausedVelocity = body.linearVelocity;
        yield return Seconds(0.1f); sample = music.timeSamples;
        yield return Seconds(0.25f);
        Require(manager.State == GameState.Paused && Time.timeScale == 0f && GameObject.Find("Pause Menu") != null,
            "Pause state/menu invalid.");
        Require(body.position == pausedPosition && body.linearVelocity == pausedVelocity && !body.isKinematic &&
            manager.ElapsedTime == pausedClock, "Pause changed airborne body/clock.");
        Require(!music.isPlaying && music.timeSamples == sample, "Paused music advanced.");
        Capture("Pause", 1280, 720);
        Click("Pause Audio Button"); yield return Frames(2);
        Require(audio.IsMuted && music.mute && sounds.mute, "Mute missed an audio source.");
        float fixedTime = Time.fixedTime;
        Click("Resume Button");
        Require(manager.State == GameState.Playing && Time.timeScale == 1f, "Resume state/time scale invalid.");
        // Renderframes garantieren keinen Physikschritt. Auf die tatsächlichen Ergebnisse warten.
        yield return Until(() => Time.fixedTime > fixedTime && body.position != pausedPosition, 2,
            "Resume did not advance physics.");
        yield return Until(() => music.isPlaying && music.timeSamples != sample, 2, "Resume did not advance music.");
        Require(audio.IsMuted && music.mute, "Resume discarded mute.");
        yield return Until(() => !player.IsKickoffFlight && (bool)Call(player, "IsGrounded"), 4,
            "Kickoff did not land after resume.");
        Require(Vector3.Distance(body.position, generator.KickoffTarget) < 2f && !foot.IsVisible,
            "Ball missed the default landing or the foot stayed in view.");
        Pass("Visible boot swings at kickoff and withdraws; pause freezes the flight, timer and music; resume preserves mute.");

        manager.PauseRun(); yield return Frames(2);
        Click("Pause Main Button"); yield return Scene("MainMenu");
        Require(Time.timeScale == 1f && Object.FindFirstObjectByType<GameManager>() == null, "Paused return to menu failed.");
        CheckAudio(audio); Require(audio.IsMuted && music.mute, "Menu transition discarded mute.");
        Click("Play Button"); yield return Scene("Game");
        manager = Object.FindFirstObjectByType<GameManager>();
        player = Object.FindFirstObjectByType<PlayerController>(); body = player.GetComponent<Rigidbody>();
        generator = Object.FindFirstObjectByType<LevelGenerator>();
        feedback = Object.FindFirstObjectByType<GameFeedback>();
        CheckAudio(audio); Require(audio.IsMuted, "New Game scene discarded mute."); audio.ToggleMute();
        int previousSeed = generator.RunSeed;
        manager.PauseRun(); yield return Frames(2); Click("Pause Restart Button"); yield return Frames(3);
        Require(manager.State == GameState.Playing && manager.CompletedLevels == 0 && manager.ElapsedTime < 0.5f &&
            !body.isKinematic && Time.timeScale == 1f && player.IsWaitingForKickoff &&
            generator.RunSeed != previousSeed, "Pause restart failed.");
        Pass("MainMenu -> Game -> paused MainMenu -> Game keeps audio identity/mute; restart resets run/time scale.");

        yield return Until(() => !player.IsWaitingForKickoff && !player.IsKickoffFlight &&
            (bool)Call(player, "IsGrounded"), 4, "Restart kickoff did not land.");
        int runSeed = generator.RunSeed;
        float previousTime = manager.ElapsedTime;
        Goal goal = Object.FindFirstObjectByType<Goal>();
        body.position = goal.transform.TransformPoint(new Vector3(0f, 1.5f, 0f));
        body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
        yield return Until(() => manager.CurrentLevel == 2, 4, "Real goal trigger did not advance level.");
        yield return Frames(3);
        Require(manager.CompletedLevels == 1 && manager.ElapsedTime >= previousTime && generator.RunSeed == runSeed,
            "Goal reset score, clock or seed incorrectly.");
        VolumeProfile runtime = (VolumeProfile)Get(feedback, "runtimeProfile");
        runtime.TryGet(out Bloom bloom); runtime.TryGet(out Vignette vignette);
        Require(bloom.intensity.value > 0.3f && ((ParticleSystem)Get(feedback, "goalParticles")).particleCount > 0,
            "Goal bloom/particles missing.");
        Require(sounds.isPlaying, "Goal sound missing."); Capture("GoalVFX", 1280, 720);
        yield return Seconds(1.1f);
        Require(Mathf.Abs(bloom.intensity.value - 0.18f) < 0.01f, "Bloom did not fade.");
        Volume volume = (Volume)Get(feedback, "postProcessVolume");
        volume.sharedProfile.TryGet(out Bloom sharedBloom);
        Require(runtime != volume.sharedProfile && Mathf.Abs(sharedBloom.intensity.value - 0.18f) < 0.01f,
            "Feedback mutated saved Volume profile.");
        yield return Until(() => !player.IsWaitingForKickoff && !player.IsKickoffFlight &&
            (bool)Call(player, "IsGrounded"), 4, "Next level kickoff did not land.");
        Set(player, "jumpHeld", true); Set(player, "jumpRequested", true); Call(player, "FixedUpdate");
        yield return Frames(3);
        Require(((ParticleSystem)Get(feedback, "jumpParticles")).particleCount > 0 && sounds.isPlaying,
            "Jump particles/sound missing."); Capture("JumpVFX", 1280, 720);
        Require(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Goal.mat").shader.name == "Project1/PulseGlow",
            "Own shader is not used by goal.");
        Pass("Actual goal trigger advances level; goal/jump events emit particles and SFX; bloom fades on cloned profile; PulseGlow compiled/used.");

        for (int i = 0; i < 20; i++)
        {
            int count = manager.CompletedLevels;
            manager.CompleteLevel(null); Require(manager.CompletedLevels == count, "Non-player counted.");
            manager.CompleteLevel(player); manager.CompleteLevel(player);
            Require(manager.CompletedLevels == count + 1, "Duplicate goal counted.");
            yield return Frames(3);
            Require(generator.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Generated Course") == 1,
                "Old course objects accumulated.");
        }
        Require(manager.CurrentLevel == 22 && manager.BestScore == 21 && manager.RequiresKickoffSteering,
            "Endless score/progression failed.");
        yield return Until(() => manager.IsGameOver, 5, "A late kickoff without steering should miss the platforms.");
        float stoppedClock = manager.ElapsedTime; Vector3 stoppedBody = body.position;
        yield return Seconds(0.5f);
        Require(body.isKinematic && vignette.intensity.value > 0.47f && manager.ElapsedTime == stoppedClock &&
            body.position == stoppedBody, "Game Over freeze/vignette failed.");
        manager.PauseRun(); manager.CompleteLevel(player);
        Require(manager.IsGameOver && Time.timeScale == 1f && manager.CompletedLevels == 21, "Game Over accepted pause/goal.");
        Capture("GameOver", 1280, 720); Capture("GameOverSmall", 960, 540);
        Click("Result Main Button"); yield return Scene("MainMenu");
        Require(Object.FindFirstObjectByType<HighscoreManager>().BestScore == 21, "Menu did not reload QA highscore.");
        Require(GameObject.Find("Menu Record").GetComponent<Text>().text.Contains("21"), "Menu highscore label stale.");
        Click("Play Button"); yield return Scene("Game");
        manager = Object.FindFirstObjectByType<GameManager>(); player = Object.FindFirstObjectByType<PlayerController>();
        Require(manager.CurrentLevel == 1 && manager.CompletedLevels == 0 && manager.BestScore == 21, "New run lost score/reset.");
        manager.PauseRun(); int pausedCount = manager.CompletedLevels; manager.CompleteLevel(player);
        Require(manager.CompletedLevels == pausedCount, "Paused goal counted.");
        manager.ShowMainMenu(); yield return Scene("MainMenu"); CheckAudio(audio);
        Require(errors == 0, "Runtime console errors: " + errors);
        Pass("21 completions reach level 22; duplicate/non-player/paused guards; one course; natural fall freezes run and raises vignette; menu/new run retain score.");
        Pass("Repeated real scene changes retain exactly one AudioManager and one AudioListener. Console errors: 0.");
        Pass("QA record 21 saved for fresh-process verification. Production record untouched. Button UnityEvents invoked programmatically in Unity Play Mode.");
        File.WriteAllLines("Logs/SubmissionPlayReport.txt", results);
        PlayerPrefs.Save(); SessionState.SetBool(Passed, true);
        Debug.Log("SUBMISSION_PLAY_PASSED"); EditorApplication.ExitPlaymode();
    }

    public static void ReadPersistenceProbe()
    {
        Require(PlayerPrefs.GetInt(TestKey, -1) == 21, "Fresh process did not load QA score 21.");
        PlayerPrefs.DeleteKey(TestKey); PlayerPrefs.Save();
        File.WriteAllText("Logs/SubmissionPersistence.txt", "PASS: fresh process loaded isolated QA score 21, then removed QA key. Production record unchanged.");
        Debug.Log("SUBMISSION_PERSISTENCE_PASSED"); EditorApplication.Exit(0);
    }

    private static void CheckAudio(AudioManager expected)
    {
        Require(AudioManager.Instance == expected && expected.gameObject.scene.name == "DontDestroyOnLoad" &&
            Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1, "Audio singleton changed/duplicated.");
        Require(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Listener count invalid.");
    }
    private static IEnumerator Scene(string name)
    {
        yield return Until(() => SceneManager.GetActiveScene().name == name, 6, "Scene did not load: " + name);
        yield return Frames(5);
    }
    private static IEnumerator Until(Func<bool> condition, double timeout, string message)
    {
        double deadline = EditorApplication.timeSinceStartup + timeout;
        while (!condition() && EditorApplication.timeSinceStartup < deadline) yield return null;
        Require(condition(), message);
    }
    private static IEnumerator Frames(int count) { int start = Time.frameCount; while (Time.frameCount < start + count) yield return null; }
    private static IEnumerator Seconds(double seconds) { double end = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < end) yield return null; }
    private static void Click(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.name == name).onClick.Invoke();
    private static void CheckBinding(string name, string method)
    {
        Button b = GameObject.Find(name).GetComponent<Button>();
        Require(b.onClick.GetPersistentEventCount() == 1 && b.onClick.GetPersistentMethodName(0) == method, "Binding missing: " + name);
    }
    private static void Pass(string text) { results.Add("PASS: " + text); Debug.Log("SUBMISSION_PASS: " + text); }
    private static void Tick()
    {
        if (checks == null) return;
        try
        {
            if (stack.Count == 0) stack.Push(checks);
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext()) { stack.Pop(); continue; }
                if (current.Current is IEnumerator child) { stack.Push(child); continue; }
                return;
            }
            checks = null;
        }
        catch (Exception error)
        {
            checks = null; stack.Clear(); PlayerPrefs.DeleteKey(TestKey); PlayerPrefs.Save(); Time.timeScale = 1f;
            File.WriteAllText("Logs/SubmissionPlayReport.txt", string.Join("\n", results) + "\nFAIL: " + error);
            Debug.LogException(error); SessionState.SetBool(Passed, false); EditorApplication.ExitPlaymode();
        }
    }
    private static void Capture(string name, int width, int height)
    {
        Camera camera = Camera.main;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); target.Create();
        RenderTexture previous = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f; }
        camera.targetTexture = target; Canvas.ForceUpdateCanvases();
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (renderer.sharedMaterial != null) ShaderUtil.CompilePass(renderer.sharedMaterial, 0, true);
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
        RenderTexture.active = target;
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
        File.WriteAllBytes("Logs/Submission" + name + ".png", image.EncodeToPNG());
        camera.targetTexture = null;
        for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
        Canvas.ForceUpdateCanvases(); RenderTexture.active = previous; ShaderUtil.allowAsyncCompilation = async;
        target.Release(); Object.Destroy(image); Object.Destroy(target);
    }
    private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static object Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
