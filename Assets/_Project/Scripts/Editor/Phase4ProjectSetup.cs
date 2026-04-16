using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Core;
using WorldOfVictoria.Player;

public static class Phase4ProjectSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager missing in Game scene.");
            return;
        }

        var player = GameObject.Find("Player");
        var camera = GameObject.Find("Player/Camera");
        if (player == null || camera == null)
        {
            Debug.LogError("Player or Player/Camera missing in Game scene.");
            return;
        }

        var playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = player.AddComponent<PlayerController>();
        }

        var mouseLook = camera.GetComponent<MouseLook>();
        if (mouseLook == null)
        {
            mouseLook = camera.AddComponent<MouseLook>();
        }

        mouseLook.Initialize(player.transform, 0.15f, 90f);

        var serializedController = new SerializedObject(playerController);
        serializedController.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedController.FindProperty("mouseLook").objectReferenceValue = mouseLook;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        player.transform.position = gameManager.GetDefaultSpawnPosition();
        camera.transform.localPosition = new Vector3(0f, 0f, 0f);
        camera.transform.localRotation = Quaternion.identity;

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }
}
