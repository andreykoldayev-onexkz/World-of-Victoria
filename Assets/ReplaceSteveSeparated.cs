using UnityEditor;
using UnityEngine;

public class ReplaceSteveSeparated
{
    public static void Execute()
    {
        GameObject oldSteve = GameObject.Find("RealisticSteve_Animated");
        if (oldSteve != null)
        {
            GameObject.DestroyImmediate(oldSteve);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/RealisticSteve_SeparatedLegs.glb");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RealisticSteve_SeparatedLegs";
            instance.transform.position = new Vector3(0, 0, 0);
            Debug.Log("Instantiated RealisticSteve_SeparatedLegs");
        }
        else
        {
            Debug.LogError("Could not find prefab at Assets/_Project/Prefabs/Characters/RealisticSteve_SeparatedLegs.glb");
        }
    }
}