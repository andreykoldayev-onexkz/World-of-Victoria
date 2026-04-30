using UnityEditor;
using UnityEngine;

public class ReplaceSteve
{
    public static void Execute()
    {
        GameObject oldSteve = GameObject.Find("RealisticSteve");
        if (oldSteve != null)
        {
            GameObject.DestroyImmediate(oldSteve);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/RealisticSteve_Animated.glb");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RealisticSteve_Animated";
            instance.transform.position = new Vector3(0, 0, 0);
            Debug.Log("Instantiated RealisticSteve_Animated");
        }
        else
        {
            Debug.LogError("Could not find prefab at Assets/_Project/Prefabs/Characters/RealisticSteve_Animated.glb");
        }
    }
}