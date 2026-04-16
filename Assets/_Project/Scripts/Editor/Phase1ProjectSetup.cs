using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WorldOfVictoria.Core;

public static class Phase1ProjectSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string WorldConfigPath = "Assets/_Project/Config/WorldConfig.asset";
    private const string PhysicsConfigPath = "Assets/_Project/Config/PhysicsConfig.asset";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    public static void Execute()
    {
        EnsureSceneIsOpen(GameScenePath);

        RemoveIfPresent("Global Volume");

        var gameManagerGo = GetOrCreateRoot("GameManager");
        var worldRoot = GetOrCreateRoot("WorldRoot");
        var player = GetOrCreateRoot("Player");
        var lighting = GetOrCreateRoot("Lighting");
        var ui = GetOrCreateRoot("UI");

        player.transform.position = new Vector3(128f, 67f, 128f);
        worldRoot.transform.position = Vector3.zero;
        lighting.transform.position = Vector3.zero;
        ui.transform.position = Vector3.zero;

        var lightGo = FindInScene("Directional Light");
        if (lightGo != null)
        {
            lightGo.transform.SetParent(lighting.transform, true);
            lightGo.name = "Directional Light";
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        var cameraGo = FindInScene("Main Camera") ?? FindInScene("Camera");
        if (cameraGo != null)
        {
            cameraGo.transform.SetParent(player.transform, false);
            cameraGo.name = "Camera";
            cameraGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraGo.transform.localRotation = Quaternion.identity;

            var camera = cameraGo.GetComponent<Camera>();
            if (camera != null)
            {
                camera.fieldOfView = 70f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 1000f;
            }
        }

        var playerCollider = player.GetComponent<BoxCollider>();
        if (playerCollider == null)
        {
            playerCollider = player.AddComponent<BoxCollider>();
        }

        playerCollider.size = new Vector3(0.6f, 1.8f, 0.6f);
        playerCollider.center = new Vector3(0f, 0.9f, 0f);

        var canvas = ui.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = ui.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (ui.GetComponent<CanvasScaler>() == null)
        {
            ui.AddComponent<CanvasScaler>();
        }

        if (ui.GetComponent<GraphicRaycaster>() == null)
        {
            ui.AddComponent<GraphicRaycaster>();
        }

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        var worldConfig = EnsureAsset<WorldConfig>(WorldConfigPath);
        var physicsConfig = EnsureAsset<PhysicsConfig>(PhysicsConfigPath);
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        var manager = gameManagerGo.GetComponent<GameManager>();
        if (manager == null)
        {
            manager = gameManagerGo.AddComponent<GameManager>();
        }

        var serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("worldConfig").objectReferenceValue = worldConfig;
        serializedManager.FindProperty("physicsConfig").objectReferenceValue = physicsConfig;
        serializedManager.FindProperty("worldRoot").objectReferenceValue = worldRoot;
        serializedManager.FindProperty("playerRoot").objectReferenceValue = player.transform;
        serializedManager.FindProperty("playerCamera").objectReferenceValue = cameraGo != null ? cameraGo.GetComponent<Camera>() : null;
        serializedManager.FindProperty("inputActions").objectReferenceValue = inputActions;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };

        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    private static void EnsureSceneIsOpen(string scenePath)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static T EnsureAsset<T>(string assetPath) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        var existing = FindInScene(name);
        if (existing != null)
        {
            existing.transform.SetParent(null, false);
            existing.name = name;
            return existing;
        }

        return new GameObject(name);
    }

    private static GameObject FindInScene(string name)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name == name)
            {
                return root;
            }

            var child = root.transform.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }
        }

        return GameObject.Find(name);
    }

    private static void RemoveIfPresent(string name)
    {
        var go = FindInScene(name);
        if (go != null)
        {
            Object.DestroyImmediate(go);
        }
    }
}
