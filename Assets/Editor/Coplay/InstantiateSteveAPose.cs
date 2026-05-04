using System;
using UnityEngine;
using UnityEditor;
using Coplay.Controllers.Functions;

public class InstantiateSteveAPose
{
    public static string Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/RealisticSteve_APose.glb");
        if (prefab == null)
        {
            return "Failed to load prefab.";
        }
        
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "RealisticSteve_APose_Instance";
        instance.transform.position = new Vector3(0, 0, 0);
        
        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(instance);
        
        return "Instantiated RealisticSteve_APose.glb in the scene.";
    }
}