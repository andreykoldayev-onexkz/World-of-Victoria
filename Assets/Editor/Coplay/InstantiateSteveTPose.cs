using System;
using UnityEngine;
using UnityEditor;
using Coplay.Controllers.Functions;

public class InstantiateSteveTPose
{
    public static string Execute()
    {
        // Hide the previous instance
        var oldInstance = GameObject.Find("RealisticSteve_APose_Instance");
        if (oldInstance != null)
        {
            oldInstance.SetActive(false);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/RealisticSteve_TPose.glb");
        if (prefab == null)
        {
            return "Failed to load prefab.";
        }
        
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "RealisticSteve_TPose_Instance";
        instance.transform.position = new Vector3(2, 0, 0); // Offset slightly
        
        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(instance);
        
        return "Instantiated RealisticSteve_TPose.glb in the scene.";
    }
}