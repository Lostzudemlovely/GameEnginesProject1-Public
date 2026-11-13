// Game Engines – Projekt 1
// Einmaliges Editor-Werkzeug zum Trennen der vorhandenen Menü- und Spielszene.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SubmissionSetup
{
    public static void SplitScenes()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity") != null)
            throw new Exception("MainMenu already exists. This setup only runs once.");

        Scene game = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        GameObject oldMenu = GameObject.Find("Main Menu");
        GameObject menuCanvas = Object.Instantiate(GameObject.Find("Menu Canvas"));
        menuCanvas.name = "Main Menu Canvas";
        Object.DestroyImmediate(menuCanvas.transform.Find("Pause Menu").gameObject);
        GameObject events = Object.Instantiate(Object.FindFirstObjectByType<EventSystem>().gameObject);
        events.name = "EventSystem";
        GameObject audioPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            Object.FindFirstObjectByType<AudioManager>().gameObject,
            "Assets/Prefabs/AudioManager.prefab", InteractionMode.AutomatedAction);

        Scene main = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(menuCanvas, main);
        SceneManager.MoveGameObjectToScene(events, main);
        SceneManager.SetActiveScene(main);
        PrefabUtility.InstantiatePrefab(audioPrefab, main);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.10f, 0.14f);

        var systems = new GameObject("Main Menu Systems");
        HighscoreManager score = systems.AddComponent<HighscoreManager>();
        MainMenuController controller = systems.AddComponent<MainMenuController>();
        Button play = Find<Button>(menuCanvas, "Play Button");
        Bind(play, controller.PlayGame);
        Bind(Find<Button>(menuCanvas, "Exit Button"), controller.QuitGame);
        Bind(Find<Button>(menuCanvas, "Main Audio Button"), controller.ToggleAudio);
        Ref(controller, "highscore", score);
        Ref(controller, "playButton", play);
        Ref(controller, "recordText", Find<Text>(menuCanvas, "Menu Record"));
        Ref(controller, "audioText", Find<Text>(menuCanvas, "Main Audio Button Label"));
        EditorSceneManager.SaveScene(main, "Assets/Scenes/MainMenu.unity");

        SceneManager.SetActiveScene(game);
        Object.DestroyImmediate(oldMenu);
        GameObject.Find("Game HUD").GetComponent<Canvas>().enabled = true;
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
        PlayerSettings.bundleVersion = "1.0.0";
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("SUBMISSION_SETUP_PASSED: MainMenu and Game saved; shared audio prefab and both build scenes configured.");
        EditorApplication.Exit(0);
    }

    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Build/FinalWindows/Project1.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("SUBMISSION_BUILD: " + report.summary.result + "; errors=" + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static T Find<T>(GameObject root, string name) where T : Component =>
        root.GetComponentsInChildren<T>(true).Single(c => c.name == name);

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static void Ref(Object target, string field, Object value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
