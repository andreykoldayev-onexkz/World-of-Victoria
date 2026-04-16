using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WorldOfVictoria.Core;
using WorldOfVictoria.UI;

public static class Phase7ProjectSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var uiRoot = GameObject.Find("UI");
        if (gameManager == null || uiRoot == null)
        {
            Debug.LogError("GameManager or UI root missing in Game scene.");
            return;
        }

        var loadingUi = EnsureLoadingScreen(uiRoot.transform);
        ConfigureGameManager(gameManager, loadingUi);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    private static LoadingScreenUI EnsureLoadingScreen(Transform uiRoot)
    {
        var overlay = uiRoot.Find("LoadingOverlay");
        if (overlay == null)
        {
            var overlayObject = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(uiRoot, false);
            overlay = overlayObject.transform;
        }

        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0.05f, 0.055f, 0.07f, 0.92f);

        var panel = EnsureChild(overlay, "Panel", typeof(RectTransform), typeof(Image));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460f, 180f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 0.96f);

        var status = EnsureText(panel.transform, "StatusText", new Vector2(0f, 28f), new Vector2(380f, 36f), 28, TextAnchor.MiddleCenter);
        var detail = EnsureText(panel.transform, "DetailText", new Vector2(0f, -6f), new Vector2(380f, 28f), 16, TextAnchor.MiddleCenter);
        detail.color = new Color(0.84f, 0.89f, 0.93f, 0.95f);

        var barFrame = EnsureChild(panel.transform, "ProgressFrame", typeof(RectTransform), typeof(Image));
        var frameRect = barFrame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(380f, 22f);
        frameRect.anchoredPosition = new Vector2(0f, -52f);
        barFrame.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.24f, 1f);

        var fill = EnsureChild(barFrame.transform, "Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        fill.GetComponent<Image>().color = new Color(0.50f, 0.78f, 0.36f, 1f);
        fill.GetComponent<Image>().type = Image.Type.Filled;
        fill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        fill.GetComponent<Image>().fillAmount = 0.25f;

        var loadingUi = overlay.GetComponent<LoadingScreenUI>();
        if (loadingUi == null)
        {
            loadingUi = overlay.gameObject.AddComponent<LoadingScreenUI>();
        }

        var serializedUi = new SerializedObject(loadingUi);
        serializedUi.FindProperty("root").objectReferenceValue = overlay.gameObject;
        serializedUi.FindProperty("progressFill").objectReferenceValue = fill.GetComponent<Image>();
        serializedUi.FindProperty("statusText").objectReferenceValue = status;
        serializedUi.FindProperty("detailText").objectReferenceValue = detail;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();

        overlay.gameObject.SetActive(false);
        return loadingUi;
    }

    private static void ConfigureGameManager(GameManager gameManager, LoadingScreenUI loadingUi)
    {
        var serializedManager = new SerializedObject(gameManager);
        serializedManager.FindProperty("loadingScreen").objectReferenceValue = loadingUi;
        serializedManager.FindProperty("loadLastSaveOnPlay").boolValue = true;
        serializedManager.FindProperty("saveOnApplicationQuit").boolValue = true;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsureChild(Transform parent, string name, params System.Type[] components)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        var gameObject = new GameObject(name, components);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text EnsureText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor)
    {
        var textObject = EnsureChild(parent, name, typeof(RectTransform), typeof(Text));
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = new Color(0.97f, 0.98f, 1f, 1f);
        text.text = name;
        return text;
    }
}
