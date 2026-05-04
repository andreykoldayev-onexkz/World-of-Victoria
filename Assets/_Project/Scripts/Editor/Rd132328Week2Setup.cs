using System.IO;
using UnityEditor;
using UnityEngine;
using WorldOfVictoria.Rendering.Character;

public static class Rd132328Week2Setup
{
    private const string RiggedModelPath = "Assets/_Project/Prefabs/Characters/Zombie_Generated_Rigged.glb";
    private const string WalkingModelPath = "Assets/_Project/Prefabs/Characters/Zombie_Generated_Rigged_walking.glb";
    private const string RunningModelPath = "Assets/_Project/Prefabs/Characters/Zombie_Generated_Rigged_running.glb";
    private const string MaterialPath = "Assets/_Project/Materials/Characters/ZombieGenerated_URP.mat";
    private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Zombie.prefab";

    public static void Execute()
    {
        var modelPrefab = LoadModelAsset(RiggedModelPath);
        var idleClip = LoadFirstAnimationClip(RiggedModelPath);
        var walkClip = LoadFirstAnimationClip(WalkingModelPath);
        var runClip = LoadFirstAnimationClip(RunningModelPath);
        var material = EnsureMaterial();

        EnsurePrefab(modelPrefab, idleClip, walkClip, runClip, material);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string Report()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var modelPrefab = LoadModelAsset(RiggedModelPath);
        var walkClip = LoadFirstAnimationClip(WalkingModelPath);
        var runClip = LoadFirstAnimationClip(RunningModelPath);

        return $"Prefab={(prefab != null ? prefab.name : "missing")}; Model={(modelPrefab != null ? modelPrefab.name : "missing")}; Material={(material != null ? material.name : "missing")}; WalkClip={(walkClip != null ? walkClip.name : "missing")}; RunClip={(runClip != null ? runClip.name : "missing")}";
    }

    private static Material EnsureMaterial()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath) ?? "Assets");

        var baseTexture = LoadFirstTexture(RiggedModelPath);
        var sourceMaterial = LoadFirstMaterial(RiggedModelPath);

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.name = "ZombieGenerated_URP";
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", baseTexture);
        material.SetColor("_BaseColor", new Color(1.01f, 1.01f, 1.01f, 1f));
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_Metallic", 0.01f);
        material.SetFloat("_Smoothness", 0.22f);
        material.SetFloat("_OcclusionStrength", 1f);
        material.enableInstancing = true;

        if (sourceMaterial != null && sourceMaterial.HasProperty("_BumpMap"))
        {
            var bumpMap = sourceMaterial.GetTexture("_BumpMap");
            if (bumpMap != null)
            {
                material.SetTexture("_BumpMap", bumpMap);
                material.EnableKeyword("_NORMALMAP");
            }
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsurePrefab(GameObject modelPrefab, AnimationClip idleClip, AnimationClip walkClip, AnimationClip runClip, Material material)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");

        var root = new GameObject("Zombie");
        try
        {
            var view = root.AddComponent<ZombieModelView>();
            view.Configure(modelPrefab, idleClip, walkClip, runClip, material);
            view.SetAnimationPose(0f, 0.75f, 0f, 0f, 0f);

            if (root.GetComponent<Zombie>() == null)
            {
                root.AddComponent<Zombie>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject LoadModelAsset(string assetPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is GameObject gameObject)
            {
                return gameObject;
            }
        }

        return null;
    }

    private static AnimationClip LoadFirstAnimationClip(string assetPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        return null;
    }

    private static Texture2D LoadFirstTexture(string assetPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Texture2D texture)
            {
                return texture;
            }
        }

        return null;
    }

    private static Material LoadFirstMaterial(string assetPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Material material)
            {
                return material;
            }
        }

        return null;
    }
}
