using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Rendering.Character;

public static class Rd132328Week2Setup
{
    private const string SourceTexturePath = "rd-132328/exports/root/char.png";
    private const string TargetTexturePath = "Assets/_Project/Textures/Characters/Zombie/char.png";
    private const string MaterialPath = "Assets/_Project/Materials/Characters/ZombieChar.mat";
    private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Zombie.prefab";

    public static void Execute()
    {
        EnsureTextureImported();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetTexturePath);
        var material = EnsureMaterial(texture);
        EnsurePrefab(material);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string Report()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetTexturePath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        return $"Texture={(texture != null ? texture.name : "missing")}; Material={(material != null ? material.name : "missing")}; Prefab={(prefab != null ? prefab.name : "missing")}";
    }

    private static void EnsureTextureImported()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TargetTexturePath) ?? "Assets");

        var fullSource = Path.GetFullPath(SourceTexturePath);
        var fullTarget = Path.GetFullPath(TargetTexturePath);
        if (!File.Exists(fullTarget))
        {
            File.Copy(fullSource, fullTarget, true);
        }

        AssetDatabase.ImportAsset(TargetTexturePath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(TargetTexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Material EnsureMaterial(Texture2D texture)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath) ?? "Assets");

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.name = "ZombieChar";
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(1.02f, 1.03f, 1f, 1f));
        material.SetFloat("_Smoothness", 0.08f);
        material.SetFloat("_Metallic", 0f);
        material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsurePrefab(Material material)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");

        var root = new GameObject("Zombie");
        try
        {
            var view = root.AddComponent<ZombieModelView>();
            view.CharacterMaterial = material;
            view.SetAnimationPose(0f, 1f, 0f, 0f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
