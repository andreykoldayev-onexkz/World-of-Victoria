using UnityEditor;
using UnityEngine;

public class InstantiateSteve
{
    public static void Execute()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/RealisticSteve.glb");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RealisticSteve";
            instance.transform.position = new Vector3(0, 0, 0);
            Debug.Log("Instantiated RealisticSteve");
        }
        else
        {
            Debug.LogError("Could not find prefab at Assets/_Project/Prefabs/Characters/RealisticSteve.glb");
        }
    }
}