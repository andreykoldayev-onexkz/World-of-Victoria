using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class UrpPhase12SsaoSetup
{
    private const string RenderingFolder = "Assets/_Project/Config/Rendering";
    private const string BaseRendererPath = RenderingFolder + "/VoxelRemake_Renderer.asset";
    private const string SsaoRendererPath = RenderingFolder + "/VoxelRemake_Renderer_SSAO.asset";
    private const string UltraPipelinePath = RenderingFolder + "/VoxelRemake_Ultra.asset";
    private const string HighPipelinePath = RenderingFolder + "/VoxelRemake_High.asset";
    private const string MediumPipelinePath = RenderingFolder + "/VoxelRemake_Medium.asset";
    private const string LowPipelinePath = RenderingFolder + "/VoxelRemake_Low.asset";

    public static void Execute()
    {
        var baseRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(BaseRendererPath);
        if (baseRenderer == null)
        {
            Debug.LogError("Base URP renderer asset is missing.");
            return;
        }

        var ssaoRenderer = EnsureSsaoRenderer(baseRenderer);
        var feature = EnsureSsaoFeature(ssaoRenderer);
        ConfigureSsaoFeature(feature);

        AssignRenderer(UltraPipelinePath, ssaoRenderer);
        AssignRenderer(HighPipelinePath, ssaoRenderer);
        AssignRenderer(MediumPipelinePath, baseRenderer);
        AssignRenderer(LowPipelinePath, baseRenderer);

        EditorUtility.SetDirty(ssaoRenderer);
        EditorUtility.SetDirty(feature);
        AssetDatabase.SaveAssets();
    }

    public static void Disable()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SsaoRendererPath);
        var feature = FindSsaoFeature(renderer);
        if (feature == null)
        {
            return;
        }

        var serialized = new SerializedObject(feature);
        serialized.FindProperty("m_Active").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        AssetDatabase.SaveAssets();
    }

    public static string Report()
    {
        var ultra = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UltraPipelinePath);
        var high = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(HighPipelinePath);
        var medium = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MediumPipelinePath);
        var low = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(LowPipelinePath);
        var ssaoRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SsaoRendererPath);
        var feature = FindSsaoFeature(ssaoRenderer);

        return
            $"UltraRenderer={GetAssignedRendererName(ultra)}; " +
            $"HighRenderer={GetAssignedRendererName(high)}; " +
            $"MediumRenderer={GetAssignedRendererName(medium)}; " +
            $"LowRenderer={GetAssignedRendererName(low)}; " +
            $"SSAOFeature={(feature != null ? feature.name : "missing")}";
    }

    private static UniversalRendererData EnsureSsaoRenderer(UniversalRendererData baseRenderer)
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SsaoRendererPath);
        if (renderer != null)
        {
            return renderer;
        }

        AssetDatabase.CopyAsset(BaseRendererPath, SsaoRendererPath);
        renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SsaoRendererPath);
        return renderer;
    }

    private static ScreenSpaceAmbientOcclusion EnsureSsaoFeature(UniversalRendererData renderer)
    {
        var existing = FindSsaoFeature(renderer);
        if (existing != null)
        {
            return existing;
        }

        var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
        feature.name = "Screen Space Ambient Occlusion";
        AssetDatabase.AddObjectToAsset(feature, renderer);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

        var serialized = new SerializedObject(renderer);
        var features = serialized.FindProperty("m_RendererFeatures");
        var featureMap = serialized.FindProperty("m_RendererFeatureMap");

        var index = features.arraySize;
        features.InsertArrayElementAtIndex(index);
        features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
        featureMap.InsertArrayElementAtIndex(index);
        featureMap.GetArrayElementAtIndex(index).longValue = localId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return feature;
    }

    private static ScreenSpaceAmbientOcclusion FindSsaoFeature(UniversalRendererData renderer)
    {
        if (renderer == null)
        {
            return null;
        }

        foreach (var feature in renderer.rendererFeatures)
        {
            if (feature is ScreenSpaceAmbientOcclusion ssao)
            {
                return ssao;
            }
        }

        return null;
    }

    private static void ConfigureSsaoFeature(ScreenSpaceAmbientOcclusion feature)
    {
        var serialized = new SerializedObject(feature);
        serialized.FindProperty("m_Active").boolValue = true;
        var settings = serialized.FindProperty("m_Settings");
        settings.FindPropertyRelative("Downsample").boolValue = true;
        settings.FindPropertyRelative("AfterOpaque").boolValue = false;
        settings.FindPropertyRelative("Source").enumValueIndex = 1;
        settings.FindPropertyRelative("NormalSamples").enumValueIndex = 0;
        settings.FindPropertyRelative("AOMethod").enumValueIndex = 0;
        settings.FindPropertyRelative("Intensity").floatValue = 0.48f;
        settings.FindPropertyRelative("DirectLightingStrength").floatValue = 0.08f;
        settings.FindPropertyRelative("Radius").floatValue = 0.018f;
        settings.FindPropertyRelative("Samples").enumValueIndex = 2;
        settings.FindPropertyRelative("BlurQuality").enumValueIndex = 2;
        settings.FindPropertyRelative("Falloff").floatValue = 48f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignRenderer(string pipelinePath, UniversalRendererData renderer)
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipeline == null)
        {
            return;
        }

        var serialized = new SerializedObject(pipeline);
        serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
    }

    private static string GetAssignedRendererName(UniversalRenderPipelineAsset pipeline)
    {
        if (pipeline == null)
        {
            return "null";
        }

        var serialized = new SerializedObject(pipeline);
        var renderer = serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
        return renderer != null ? renderer.name : "null";
    }
}
