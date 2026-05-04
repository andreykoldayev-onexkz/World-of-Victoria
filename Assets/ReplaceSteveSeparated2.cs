using UnityEditor;
using UnityEngine;

public class ReplaceSteveSeparated2
{
    public static void Execute()
    {
        GameObject oldSteve = GameObject.Find("RealisticSteve_SeparatedLegs");
        if (oldSteve != null)
        {
            GameObject.DestroyImmediate(oldSteve);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/RealisticSteve_SeparatedLegs2.glb");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RealisticSteve_SeparatedLegs2";
            instance.transform.position = new Vector3(0, 0, 0);
            Debug.Log("Instantiated RealisticSteve_SeparatedLegs2");
        }
        else
        {
            Debug.LogError("Could not find prefab at Assets/_Project/Prefabs/Characters/RealisticSteve_SeparatedLegs2.glb");
        }
    }
}