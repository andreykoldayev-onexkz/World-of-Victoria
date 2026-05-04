using UnityEditor;
using UnityEngine;

public class InstantiateSteve
{
    public static string Execute()
    {
        string prefabPath = "Assets/_Project/Prefabs/Characters/RealisticSteve_Modified.glb";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RealisticSteve_Modified_Instance";
            instance.transform.position = new Vector3(0, 10, 0);
            Selection.activeGameObject = instance;
            return "Instantiated prefab";
        }
        return "Prefab not found";
    }
}