using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldOfVictoria.Rendering.Character;

public static class Rd132328Week3Setup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string ZombiePrefabPath = "Assets/_Project/Prefabs/Characters/Zombie.prefab";

    public static void Execute()
    {
        EnsureSceneIsOpen(GameScenePath);
        CleanupLegacySceneObjects();
        EnsureZombiePrefabHasController();
        EnsureSceneTestZombie();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        var prefabHasZombie = prefab != null && prefab.GetComponent<Zombie>() != null;
        var sceneZombie = GameObject.Find("Zombie_Test");
        var steveLeftovers = FindLegacySceneObjectCount();
        return $"PrefabHasZombie={prefabHasZombie}; SceneZombie={(sceneZombie != null ? sceneZombie.name : "missing")}; LegacySceneObjects={steveLeftovers}";
    }

    private static void EnsureSceneIsOpen(string scenePath)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static void EnsureZombiePrefabHasController()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(ZombiePrefabPath);
        try
        {
            if (prefabRoot.GetComponent<Zombie>() == null)
            {
                prefabRoot.AddComponent<Zombie>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ZombiePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void CleanupLegacySceneObjects()
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            CleanupLegacyRecursive(root.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void CleanupLegacyRecursive(Transform current)
    {
        for (var i = current.childCount - 1; i >= 0; i--)
        {
            CleanupLegacyRecursive(current.GetChild(i));
        }

        if (IsLegacyObject(current.gameObject))
        {
            Object.DestroyImmediate(current.gameObject);
        }
    }

    private static int FindLegacySceneObjectCount()
    {
        var count = 0;
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            CountLegacyRecursive(root.transform, ref count);
        }

        return count;
    }

    private static void CountLegacyRecursive(Transform current, ref int count)
    {
        if (IsLegacyObject(current.gameObject))
        {
            count++;
        }

        for (var i = 0; i < current.childCount; i++)
        {
            CountLegacyRecursive(current.GetChild(i), ref count);
        }
    }

    private static bool IsLegacyObject(GameObject gameObject)
    {
        var objectName = gameObject.name;
        return objectName.Contains("RealisticSteve") || objectName.Contains("SteveVisual");
    }

    private static void EnsureSceneTestZombie()
    {
        var existing = GameObject.Find("Zombie_Test");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return;
        }

        instance.name = "Zombie_Test";
        instance.transform.position = new Vector3(136f, 46f, 136f);
        instance.transform.rotation = Quaternion.identity;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
