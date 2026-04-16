using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WorldOfVictoria.Rendering;

public static class UrpPhase3PbrSetup
{
    private const int TextureSize = 1024;

    private const string PbrTextureRoot = "Assets/_Project/Textures/PBR";
    private const string StoneTextureRoot = PbrTextureRoot + "/Stone";
    private const string GrassTextureRoot = PbrTextureRoot + "/Grass";
    private const string PbrMaterialRoot = "Assets/_Project/Materials/PBR";
    private const string RenderingConfigRoot = "Assets/_Project/Config/Rendering";

    private const string LibraryAssetPath = RenderingConfigRoot + "/VoxelPbrTextureLibrary.asset";
    private const string AlbedoArrayPath = RenderingConfigRoot + "/VoxelPbr_AlbedoArray.asset";
    private const string NormalArrayPath = RenderingConfigRoot + "/VoxelPbr_NormalArray.asset";
    private const string RoughnessArrayPath = RenderingConfigRoot + "/VoxelPbr_RoughnessArray.asset";
    private const string StoneMaterialPath = PbrMaterialRoot + "/StonePBR.mat";
    private const string GrassMaterialPath = PbrMaterialRoot + "/GrassPBR.mat";

    private const string StoneColorSource = StoneTextureRoot + "/Ground091_1K-PNG/Ground091_1K-PNG_Color.png";
    private const string StoneNormalSource = StoneTextureRoot + "/Ground091_1K-PNG/Ground091_1K-PNG_NormalGL.png";
    private const string StoneHeightSource = StoneTextureRoot + "/Ground091_1K-PNG/Ground091_1K-PNG_Displacement.png";
    private const string StoneAoSource = StoneTextureRoot + "/Ground091_1K-PNG/Ground091_1K-PNG_AmbientOcclusion.png";
    private const string StoneRoughnessSource = StoneTextureRoot + "/Ground091_1K-PNG/Ground091_1K-PNG_Roughness.png";

    private const string GrassTopColorSource = GrassTextureRoot + "/Grass005_1K-PNG/Grass005_1K-PNG_Color.png";
    private const string GrassTopNormalSource = GrassTextureRoot + "/Grass005_1K-PNG/Grass005_1K-PNG_NormalGL.png";
    private const string GrassTopHeightSource = GrassTextureRoot + "/Grass005_1K-PNG/Grass005_1K-PNG_Displacement.png";
    private const string GrassTopAoSource = GrassTextureRoot + "/Grass005_1K-PNG/Grass005_1K-PNG_AmbientOcclusion.png";
    private const string GrassTopRoughnessSource = GrassTextureRoot + "/Grass005_1K-PNG/Grass005_1K-PNG_Roughness.png";

    private const string DirtColorSource = GrassTextureRoot + "/Ground036_1K-PNG/Ground036_1K-PNG_Color.png";
    private const string DirtNormalSource = GrassTextureRoot + "/Ground036_1K-PNG/Ground036_1K-PNG_NormalGL.png";
    private const string DirtHeightSource = GrassTextureRoot + "/Ground036_1K-PNG/Ground036_1K-PNG_Displacement.png";
    private const string DirtAoSource = GrassTextureRoot + "/Ground036_1K-PNG/Ground036_1K-PNG_AmbientOcclusion.png";
    private const string DirtRoughnessSource = GrassTextureRoot + "/Ground036_1K-PNG/Ground036_1K-PNG_Roughness.png";

    public static void Execute()
    {
        EnsureFolders();

        GeneratePhase3Textures();
        AssetDatabase.Refresh();

        ConfigureGeneratedTextureImports();

        var stoneAlbedo = LoadTexture(StoneTextureRoot + "/stone_albedo_512.png");
        var stoneNormal = LoadTexture(StoneTextureRoot + "/stone_normal_512.png");
        var stoneHeight = LoadTexture(StoneTextureRoot + "/stone_height_512.png");
        var stoneAo = LoadTexture(StoneTextureRoot + "/stone_ao_512.png");
        var stoneRoughness = LoadTexture(StoneTextureRoot + "/stone_roughness_512.png");
        var stoneMetallic = LoadTexture(StoneTextureRoot + "/stone_metallic_512.png");

        var grassTopAlbedo = LoadTexture(GrassTextureRoot + "/grass_top_albedo_512.png");
        var grassTopNormal = LoadTexture(GrassTextureRoot + "/grass_top_normal_512.png");
        var grassTopHeight = LoadTexture(GrassTextureRoot + "/grass_top_height_512.png");
        var grassTopAo = LoadTexture(GrassTextureRoot + "/grass_top_ao_512.png");
        var grassTopRoughness = LoadTexture(GrassTextureRoot + "/grass_top_roughness_512.png");
        var grassTopMetallic = LoadTexture(GrassTextureRoot + "/grass_top_metallic_512.png");

        var grassSideAlbedo = LoadTexture(GrassTextureRoot + "/grass_side_albedo_512.png");
        var grassSideNormal = LoadTexture(GrassTextureRoot + "/grass_side_normal_512.png");
        var grassSideHeight = LoadTexture(GrassTextureRoot + "/grass_side_height_512.png");
        var grassSideAo = LoadTexture(GrassTextureRoot + "/grass_side_ao_512.png");
        var grassSideRoughness = LoadTexture(GrassTextureRoot + "/grass_side_roughness_512.png");
        var grassSideMetallic = LoadTexture(GrassTextureRoot + "/grass_side_metallic_512.png");

        var grassBottomAlbedo = LoadTexture(GrassTextureRoot + "/grass_bottom_albedo_512.png");
        var grassBottomNormal = LoadTexture(GrassTextureRoot + "/grass_bottom_normal_512.png");
        var grassBottomHeight = LoadTexture(GrassTextureRoot + "/grass_bottom_height_512.png");
        var grassBottomAo = LoadTexture(GrassTextureRoot + "/grass_bottom_ao_512.png");
        var grassBottomRoughness = LoadTexture(GrassTextureRoot + "/grass_bottom_roughness_512.png");
        var grassBottomMetallic = LoadTexture(GrassTextureRoot + "/grass_bottom_metallic_512.png");

        var albedoArray = CreateOrUpdateTextureArray(
            AlbedoArrayPath,
            new[] { stoneAlbedo, grassTopAlbedo, grassSideAlbedo, grassBottomAlbedo },
            TextureFormat.RGBA32,
            true);

        var normalArray = CreateOrUpdateTextureArray(
            NormalArrayPath,
            new[] { stoneNormal, grassTopNormal, grassSideNormal, grassBottomNormal },
            TextureFormat.RGBA32,
            false);

        var roughnessArray = CreateOrUpdateTextureArray(
            RoughnessArrayPath,
            new[] { stoneRoughness, grassTopRoughness, grassSideRoughness, grassBottomRoughness },
            TextureFormat.RGBA32,
            false);

        var stoneMaterial = CreateOrUpdateLitMaterial(StoneMaterialPath, stoneAlbedo, stoneNormal, stoneAo, stoneRoughness);
        var grassMaterial = CreateOrUpdateLitMaterial(GrassMaterialPath, grassTopAlbedo, grassTopNormal, grassTopAo, grassTopRoughness);

        var library = AssetDatabase.LoadAssetAtPath<VoxelPbrTextureLibrary>(LibraryAssetPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<VoxelPbrTextureLibrary>();
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
        }

        var serializedLibrary = new SerializedObject(library);
        serializedLibrary.FindProperty("stonePbr").objectReferenceValue = stoneMaterial;
        serializedLibrary.FindProperty("grassPbr").objectReferenceValue = grassMaterial;

        serializedLibrary.FindProperty("stoneAlbedo").objectReferenceValue = stoneAlbedo;
        serializedLibrary.FindProperty("stoneNormal").objectReferenceValue = stoneNormal;
        serializedLibrary.FindProperty("stoneHeight").objectReferenceValue = stoneHeight;
        serializedLibrary.FindProperty("stoneAo").objectReferenceValue = stoneAo;
        serializedLibrary.FindProperty("stoneRoughness").objectReferenceValue = stoneRoughness;
        serializedLibrary.FindProperty("stoneMetallic").objectReferenceValue = stoneMetallic;

        serializedLibrary.FindProperty("grassTopAlbedo").objectReferenceValue = grassTopAlbedo;
        serializedLibrary.FindProperty("grassTopNormal").objectReferenceValue = grassTopNormal;
        serializedLibrary.FindProperty("grassTopHeight").objectReferenceValue = grassTopHeight;
        serializedLibrary.FindProperty("grassTopAo").objectReferenceValue = grassTopAo;
        serializedLibrary.FindProperty("grassTopRoughness").objectReferenceValue = grassTopRoughness;
        serializedLibrary.FindProperty("grassTopMetallic").objectReferenceValue = grassTopMetallic;

        serializedLibrary.FindProperty("grassSideAlbedo").objectReferenceValue = grassSideAlbedo;
        serializedLibrary.FindProperty("grassSideNormal").objectReferenceValue = grassSideNormal;
        serializedLibrary.FindProperty("grassSideHeight").objectReferenceValue = grassSideHeight;
        serializedLibrary.FindProperty("grassSideAo").objectReferenceValue = grassSideAo;
        serializedLibrary.FindProperty("grassSideRoughness").objectReferenceValue = grassSideRoughness;
        serializedLibrary.FindProperty("grassSideMetallic").objectReferenceValue = grassSideMetallic;

        serializedLibrary.FindProperty("grassBottomAlbedo").objectReferenceValue = grassBottomAlbedo;
        serializedLibrary.FindProperty("grassBottomNormal").objectReferenceValue = grassBottomNormal;
        serializedLibrary.FindProperty("grassBottomHeight").objectReferenceValue = grassBottomHeight;
        serializedLibrary.FindProperty("grassBottomAo").objectReferenceValue = grassBottomAo;
        serializedLibrary.FindProperty("grassBottomRoughness").objectReferenceValue = grassBottomRoughness;
        serializedLibrary.FindProperty("grassBottomMetallic").objectReferenceValue = grassBottomMetallic;

        serializedLibrary.FindProperty("albedoArray").objectReferenceValue = albedoArray;
        serializedLibrary.FindProperty("normalArray").objectReferenceValue = normalArray;
        serializedLibrary.FindProperty("roughnessArray").objectReferenceValue = roughnessArray;
        serializedLibrary.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string Report()
    {
        var library = AssetDatabase.LoadAssetAtPath<VoxelPbrTextureLibrary>(LibraryAssetPath);
        if (library == null)
        {
            return "VoxelPbrTextureLibrary missing.";
        }

        return
            $"Library={LibraryAssetPath}; " +
            $"StoneMaterial={(library.StonePbr != null ? library.StonePbr.name : "null")}; " +
            $"GrassMaterial={(library.GrassPbr != null ? library.GrassPbr.name : "null")}; " +
            $"AlbedoArraySlices={(library.AlbedoArray != null ? library.AlbedoArray.depth : 0)}; " +
            $"NormalArraySlices={(library.NormalArray != null ? library.NormalArray.depth : 0)}; " +
            $"RoughnessArraySlices={(library.RoughnessArray != null ? library.RoughnessArray.depth : 0)}; " +
            $"StoneAlbedo={(library.StoneAlbedo != null ? library.StoneAlbedo.width : 0)}x{(library.StoneAlbedo != null ? library.StoneAlbedo.height : 0)}; " +
            $"GrassTopAlbedo={(library.GrassTopAlbedo != null ? library.GrassTopAlbedo.width : 0)}x{(library.GrassTopAlbedo != null ? library.GrassTopAlbedo.height : 0)}";
    }

    private static void GeneratePhase3Textures()
    {
        if (HasAmbientCgSourceTextures())
        {
            GenerateTexturesFromAmbientCgSources();
            return;
        }

        EnsureTextureFile(StoneTextureRoot + "/stone_albedo_512.png", CreateStoneAlbedoTexture);
        EnsureTextureFile(StoneTextureRoot + "/stone_normal_512.png", CreateStoneNormalTexture);
        EnsureTextureFile(StoneTextureRoot + "/stone_height_512.png", () => CreateGrayscaleNoiseTexture(0.38f, 0.18f, 31));
        EnsureTextureFile(StoneTextureRoot + "/stone_ao_512.png", () => CreateGrayscaleNoiseTexture(0.82f, 0.12f, 37));
        EnsureTextureFile(StoneTextureRoot + "/stone_roughness_512.png", () => CreateGrayscaleNoiseTexture(0.74f, 0.10f, 43));
        EnsureTextureFile(StoneTextureRoot + "/stone_metallic_512.png", () => CreateGrayscaleConstantTexture(0.02f));

        EnsureTextureFile(GrassTextureRoot + "/grass_top_albedo_512.png", CreateGrassTopAlbedoTexture);
        EnsureTextureFile(GrassTextureRoot + "/grass_top_normal_512.png", CreateGrassNormalTexture);
        EnsureTextureFile(GrassTextureRoot + "/grass_top_height_512.png", () => CreateGrayscaleNoiseTexture(0.28f, 0.18f, 53));
        EnsureTextureFile(GrassTextureRoot + "/grass_top_ao_512.png", () => CreateGrayscaleNoiseTexture(0.90f, 0.06f, 59));
        EnsureTextureFile(GrassTextureRoot + "/grass_top_roughness_512.png", () => CreateGrayscaleNoiseTexture(0.88f, 0.06f, 61));
        EnsureTextureFile(GrassTextureRoot + "/grass_top_metallic_512.png", () => CreateGrayscaleConstantTexture(0.0f));

        EnsureTextureFile(GrassTextureRoot + "/grass_side_albedo_512.png", CreateGrassSideAlbedoTexture);
        EnsureTextureFile(GrassTextureRoot + "/grass_side_normal_512.png", CreateGrassNormalTexture);
        EnsureTextureFile(GrassTextureRoot + "/grass_side_height_512.png", () => CreateGrayscaleNoiseTexture(0.24f, 0.16f, 67));
        EnsureTextureFile(GrassTextureRoot + "/grass_side_ao_512.png", () => CreateGrayscaleNoiseTexture(0.88f, 0.08f, 71));
        EnsureTextureFile(GrassTextureRoot + "/grass_side_roughness_512.png", () => CreateGrayscaleNoiseTexture(0.72f, 0.12f, 73));
        EnsureTextureFile(GrassTextureRoot + "/grass_side_metallic_512.png", () => CreateGrayscaleConstantTexture(0.0f));

        EnsureTextureFile(GrassTextureRoot + "/grass_bottom_albedo_512.png", CreateDirtAlbedoTexture);
        EnsureTextureFile(GrassTextureRoot + "/grass_bottom_normal_512.png", CreateDirtNormalTexture);
        EnsureTextureFile(GrassTextureRoot + "/grass_bottom_height_512.png", () => CreateGrayscaleNoiseTexture(0.34f, 0.18f, 79));
        EnsureTextureFile(GrassTextureRoot + "/grass_bottom_ao_512.png", () => CreateGrayscaleNoiseTexture(0.83f, 0.10f, 83));
        EnsureTextureFile(GrassTextureRoot + "/grass_bottom_roughness_512.png", () => CreateGrayscaleNoiseTexture(0.78f, 0.10f, 89));
        EnsureTextureFile(GrassTextureRoot + "/grass_bottom_metallic_512.png", () => CreateGrayscaleConstantTexture(0.0f));
    }

    private static bool HasAmbientCgSourceTextures()
    {
        return File.Exists(StoneColorSource)
            && File.Exists(StoneNormalSource)
            && File.Exists(StoneRoughnessSource)
            && File.Exists(GrassTopColorSource)
            && File.Exists(GrassTopNormalSource)
            && File.Exists(GrassTopRoughnessSource)
            && File.Exists(DirtColorSource)
            && File.Exists(DirtNormalSource)
            && File.Exists(DirtRoughnessSource);
    }

    private static void GenerateTexturesFromAmbientCgSources()
    {
        var stoneColor = ResizeTexture(LoadSourceTexture(StoneColorSource), TextureSize);
        var stoneNormal = ResizeTexture(LoadSourceTexture(StoneNormalSource), TextureSize);
        var stoneHeight = ResizeTexture(LoadSourceTexture(StoneHeightSource), TextureSize);
        var stoneAo = ResizeTexture(LoadSourceTexture(StoneAoSource), TextureSize);
        var stoneRoughness = ResizeTexture(LoadSourceTexture(StoneRoughnessSource), TextureSize);

        var grassTopColor = ResizeTexture(LoadSourceTexture(GrassTopColorSource), TextureSize);
        var grassTopNormal = ResizeTexture(LoadSourceTexture(GrassTopNormalSource), TextureSize);
        var grassTopHeight = ResizeTexture(LoadSourceTexture(GrassTopHeightSource), TextureSize);
        var grassTopAo = ResizeTexture(LoadSourceTexture(GrassTopAoSource), TextureSize);
        var grassTopRoughness = ResizeTexture(LoadSourceTexture(GrassTopRoughnessSource), TextureSize);

        var dirtColor = ResizeTexture(LoadSourceTexture(DirtColorSource), TextureSize);
        var dirtNormal = ResizeTexture(LoadSourceTexture(DirtNormalSource), TextureSize);
        var dirtHeight = ResizeTexture(LoadSourceTexture(DirtHeightSource), TextureSize);
        var dirtAo = ResizeTexture(LoadSourceTexture(DirtAoSource), TextureSize);
        var dirtRoughness = ResizeTexture(LoadSourceTexture(DirtRoughnessSource), TextureSize);

        SaveTexture(StoneTextureRoot + "/stone_albedo_512.png", stoneColor);
        SaveTexture(StoneTextureRoot + "/stone_normal_512.png", stoneNormal);
        SaveTexture(StoneTextureRoot + "/stone_height_512.png", stoneHeight);
        SaveTexture(StoneTextureRoot + "/stone_ao_512.png", stoneAo);
        SaveTexture(StoneTextureRoot + "/stone_roughness_512.png", stoneRoughness);
        SaveTexture(StoneTextureRoot + "/stone_metallic_512.png", CreateGrayscaleConstantTexture(0.0f));

        SaveTexture(GrassTextureRoot + "/grass_top_albedo_512.png", grassTopColor);
        SaveTexture(GrassTextureRoot + "/grass_top_normal_512.png", grassTopNormal);
        SaveTexture(GrassTextureRoot + "/grass_top_height_512.png", grassTopHeight);
        SaveTexture(GrassTextureRoot + "/grass_top_ao_512.png", grassTopAo);
        SaveTexture(GrassTextureRoot + "/grass_top_roughness_512.png", grassTopRoughness);
        SaveTexture(GrassTextureRoot + "/grass_top_metallic_512.png", CreateGrayscaleConstantTexture(0.0f));

        SaveTexture(GrassTextureRoot + "/grass_side_albedo_512.png", CreateGrassSideFromSources(grassTopColor, dirtColor));
        SaveTexture(GrassTextureRoot + "/grass_side_normal_512.png", CreateGrassSideFromSources(grassTopNormal, dirtNormal));
        SaveTexture(GrassTextureRoot + "/grass_side_height_512.png", CreateGrassSideFromSources(grassTopHeight, dirtHeight));
        SaveTexture(GrassTextureRoot + "/grass_side_ao_512.png", CreateGrassSideFromSources(grassTopAo, dirtAo));
        SaveTexture(GrassTextureRoot + "/grass_side_roughness_512.png", CreateGrassSideFromSources(grassTopRoughness, dirtRoughness));
        SaveTexture(GrassTextureRoot + "/grass_side_metallic_512.png", CreateGrayscaleConstantTexture(0.0f));

        SaveTexture(GrassTextureRoot + "/grass_bottom_albedo_512.png", dirtColor);
        SaveTexture(GrassTextureRoot + "/grass_bottom_normal_512.png", dirtNormal);
        SaveTexture(GrassTextureRoot + "/grass_bottom_height_512.png", dirtHeight);
        SaveTexture(GrassTextureRoot + "/grass_bottom_ao_512.png", dirtAo);
        SaveTexture(GrassTextureRoot + "/grass_bottom_roughness_512.png", dirtRoughness);
        SaveTexture(GrassTextureRoot + "/grass_bottom_metallic_512.png", CreateGrayscaleConstantTexture(0.0f));
    }

    private static void ConfigureGeneratedTextureImports()
    {
        var texturePaths = Directory.GetFiles(Path.GetFullPath(PbrTextureRoot), "*.png", SearchOption.AllDirectories);
        foreach (var absolutePath in texturePaths)
        {
            var assetPath = absolutePath.Replace(Path.GetFullPath(Application.dataPath), "Assets").Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.maxTextureSize = TextureSize;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.isReadable = true;

            var platformSettings = importer.GetPlatformTextureSettings("Standalone");
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = TextureSize;
            platformSettings.textureCompression = TextureImporterCompression.CompressedHQ;
            platformSettings.compressionQuality = 100;

            if (assetPath.Contains("_albedo") || assetPath.Contains("_Color"))
            {
                importer.sRGBTexture = true;
                importer.textureType = TextureImporterType.Default;
                platformSettings.format = TextureImporterFormat.BC7;
            }
            else if (assetPath.Contains("_normal") || assetPath.Contains("_NormalGL") || assetPath.Contains("_NormalDX"))
            {
                importer.sRGBTexture = false;
                importer.textureType = TextureImporterType.NormalMap;
                platformSettings.format = TextureImporterFormat.BC5;
            }
            else
            {
                importer.sRGBTexture = false;
                importer.textureType = TextureImporterType.Default;
                platformSettings.format = TextureImporterFormat.BC4;
            }

            importer.SetPlatformTextureSettings(platformSettings);
            importer.SaveAndReimport();
        }
    }

    private static Material CreateOrUpdateLitMaterial(string assetPath, Texture2D albedo, Texture2D normal, Texture2D ao, Texture2D roughness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(assetPath)
            };
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", albedo);
        material.SetTexture("_BumpMap", normal);
        material.SetTexture("_OcclusionMap", ao);
        material.SetFloat("_BumpScale", 1f);
        material.SetFloat("_OcclusionStrength", 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", SampleAverageSmoothness(roughness));
        material.EnableKeyword("_NORMALMAP");

        EditorUtility.SetDirty(material);
        return material;
    }

    private static float SampleAverageSmoothness(Texture2D roughnessTexture)
    {
        if (roughnessTexture == null)
        {
            return 0.35f;
        }

        var pixels = roughnessTexture.GetPixels32();
        if (pixels == null || pixels.Length == 0)
        {
            return 0.35f;
        }

        var sum = 0f;
        for (var i = 0; i < pixels.Length; i += 97)
        {
            sum += pixels[i].r / 255f;
        }

        var sampleCount = Mathf.Max(1, Mathf.CeilToInt(pixels.Length / 97f));
        return 1f - Mathf.Clamp01(sum / sampleCount);
    }

    private static Texture2DArray CreateOrUpdateTextureArray(string assetPath, Texture2D[] sources, TextureFormat format, bool linear)
    {
        var textureArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(assetPath);
        if (textureArray == null
            || textureArray.depth != sources.Length
            || textureArray.width != TextureSize
            || textureArray.height != TextureSize
            || textureArray.format != format)
        {
            if (textureArray != null)
            {
                Object.DestroyImmediate(textureArray, true);
            }

            textureArray = new Texture2DArray(TextureSize, TextureSize, sources.Length, format, true, linear)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 4
            };
            AssetDatabase.CreateAsset(textureArray, assetPath);
        }

        for (var slice = 0; slice < sources.Length; slice++)
        {
            var source = sources[slice];
            if (source == null)
            {
                continue;
            }

            textureArray.SetPixels(source.GetPixels(), slice, 0);
        }

        textureArray.Apply(true, false);
        EditorUtility.SetDirty(textureArray);
        return textureArray;
    }

    private static Texture2D LoadTexture(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static Texture2D LoadSourceTexture(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            return CreateGrayscaleConstantTexture(0.5f);
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        texture.LoadImage(File.ReadAllBytes(assetPath), false);
        return texture;
    }

    private static Texture2D ResizeTexture(Texture2D source, int targetSize)
    {
        if (source.width == targetSize && source.height == targetSize)
        {
            return source;
        }

        var resized = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        for (var y = 0; y < targetSize; y++)
        {
            var v = y / (float)(targetSize - 1);
            for (var x = 0; x < targetSize; x++)
            {
                var u = x / (float)(targetSize - 1);
                resized.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }

        resized.Apply();
        return resized;
    }

    private static Texture2D CreateGrassSideFromSources(Texture2D topTexture, Texture2D dirtTexture)
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        for (var y = 0; y < TextureSize; y++)
        {
            var v = y / (float)(TextureSize - 1);
            var blend = Mathf.InverseLerp(0.64f, 0.78f, v);
            for (var x = 0; x < TextureSize; x++)
            {
                var u = x / (float)(TextureSize - 1);
                var topColor = topTexture.GetPixelBilinear(u, Mathf.Lerp(0.12f, 0.88f, v));
                var dirtColor = dirtTexture.GetPixelBilinear(u, v);

                Color finalColor;
                if (v >= 0.78f)
                {
                    finalColor = topColor;
                }
                else if (v >= 0.64f)
                {
                    finalColor = Color.Lerp(dirtColor, topColor, blend * 0.75f);
                }
                else
                {
                    finalColor = dirtColor;
                }

                texture.SetPixel(x, y, finalColor);
            }
        }

        texture.Apply();
        return texture;
    }

    private static void SaveTexture(string assetPath, Texture2D texture)
    {
        File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
    }

    private static void EnsureTextureFile(string assetPath, System.Func<Texture2D> textureFactory)
    {
        if (File.Exists(Path.GetFullPath(assetPath)))
        {
            return;
        }

        var texture = textureFactory.Invoke();
        File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static Texture2D CreateStoneAlbedoTexture()
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var noiseA = Mathf.PerlinNoise((x + 11f) / 39f, (y + 17f) / 39f);
                var noiseB = Mathf.PerlinNoise((x + 41f) / 13f, (y + 7f) / 13f);
                var value = Mathf.Clamp01(0.46f + (noiseA - 0.5f) * 0.24f + (noiseB - 0.5f) * 0.10f);
                var tint = new Color(0.58f, 0.57f, 0.55f) * Mathf.Lerp(0.82f, 1.18f, value);
                pixels[(y * TextureSize) + x] = tint;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateStoneNormalTexture()
    {
        return CreatePerturbedNormalTexture(0.08f, 97);
    }

    private static Texture2D CreateGrassTopAlbedoTexture()
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var broad = Mathf.PerlinNoise((x + 19f) / 58f, (y + 5f) / 58f);
                var fine = Mathf.PerlinNoise((x + 7f) / 9f, (y + 13f) / 9f);
                var mix = Mathf.Clamp01(0.5f + (broad - 0.5f) * 0.55f + (fine - 0.5f) * 0.18f);
                var baseColor = Color.Lerp(new Color(0.20f, 0.39f, 0.10f), new Color(0.42f, 0.63f, 0.18f), mix);
                pixels[(y * TextureSize) + x] = baseColor;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateGrassSideAlbedoTexture()
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            var normalizedY = y / (TextureSize - 1f);
            for (var x = 0; x < TextureSize; x++)
            {
                var index = (y * TextureSize) + x;
                if (normalizedY > 0.72f)
                {
                    var broad = Mathf.PerlinNoise((x + 11f) / 47f, (y + 17f) / 31f);
                    pixels[index] = Color.Lerp(new Color(0.18f, 0.34f, 0.09f), new Color(0.39f, 0.58f, 0.17f), broad);
                }
                else
                {
                    var dirt = CreateDirtColor(x, y);
                    pixels[index] = dirt;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateDirtAlbedoTexture()
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                pixels[(y * TextureSize) + x] = CreateDirtColor(x, y);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Color CreateDirtColor(int x, int y)
    {
        var broad = Mathf.PerlinNoise((x + 29f) / 35f, (y + 13f) / 35f);
        var fine = Mathf.PerlinNoise((x + 3f) / 8f, (y + 41f) / 8f);
        var mix = Mathf.Clamp01(0.48f + (broad - 0.5f) * 0.35f + (fine - 0.5f) * 0.15f);
        return Color.Lerp(new Color(0.25f, 0.17f, 0.10f), new Color(0.42f, 0.29f, 0.18f), mix);
    }

    private static Texture2D CreateGrassNormalTexture()
    {
        return CreatePerturbedNormalTexture(0.06f, 113);
    }

    private static Texture2D CreateDirtNormalTexture()
    {
        return CreatePerturbedNormalTexture(0.05f, 131);
    }

    private static Texture2D CreatePerturbedNormalTexture(float strength, int seedOffset)
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var nx = (Mathf.PerlinNoise((x + seedOffset) / 23f, (y + seedOffset) / 23f) - 0.5f) * strength;
                var ny = (Mathf.PerlinNoise((x + seedOffset * 2) / 17f, (y + seedOffset * 3) / 17f) - 0.5f) * strength;
                var normal = new Vector3(nx, ny, 1f).normalized;
                var encoded = new Color((normal.x * 0.5f) + 0.5f, (normal.y * 0.5f) + 0.5f, (normal.z * 0.5f) + 0.5f, 1f);
                pixels[(y * TextureSize) + x] = encoded;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateGrayscaleNoiseTexture(float baseValue, float amplitude, int seedOffset)
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var broad = Mathf.PerlinNoise((x + seedOffset) / 37f, (y + seedOffset) / 37f);
                var fine = Mathf.PerlinNoise((x + seedOffset * 2) / 11f, (y + seedOffset * 3) / 11f);
                var value = Mathf.Clamp01(baseValue + (broad - 0.5f) * amplitude + (fine - 0.5f) * amplitude * 0.45f);
                pixels[(y * TextureSize) + x] = new Color(value, value, value, 1f);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateGrayscaleConstantTexture(float value)
    {
        var texture = CreateTexture();
        var pixels = new Color32[TextureSize * TextureSize];
        var color = new Color(value, value, value, 1f);
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateTexture()
    {
        return new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat,
            anisoLevel = 4
        };
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "_Project");
        EnsureFolder("Assets/_Project", "Textures");
        EnsureFolder("Assets/_Project/Textures", "PBR");
        EnsureFolder(PbrTextureRoot, "Stone");
        EnsureFolder(PbrTextureRoot, "Grass");
        EnsureFolder("Assets/_Project", "Materials");
        EnsureFolder("Assets/_Project/Materials", "PBR");
        EnsureFolder("Assets/_Project", "Config");
        EnsureFolder("Assets/_Project/Config", "Rendering");
    }

    private static void EnsureFolder(string parent, string child)
    {
        var combined = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(combined))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
