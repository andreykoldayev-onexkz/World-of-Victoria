using UnityEditor;
using UnityEngine;

public class InstantiateZombie
{
    public static void Execute()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Zombie_Generated.glb");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Zombie_Generated";
            instance.transform.position = new Vector3(0, 0, 0);
            Debug.Log("Successfully instantiated Zombie_Generated");
        }
        else
        {
            Debug.LogError("Failed to load Zombie_Generated.glb");
        }
    }
}
