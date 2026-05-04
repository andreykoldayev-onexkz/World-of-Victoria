using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class OpenPrefab
{
    public static string Execute()
    {
        string prefabPath = "Assets/_Project/Prefabs/Characters/RealisticSteve_Modified.glb";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            AssetDatabase.OpenAsset(prefab);
            return "Opened prefab";
        }
        return "Prefab not found";
    }
}