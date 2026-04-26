using UnityEngine;
using UnityEngine.UI;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.UI
{
    [DisallowMultipleComponent]
    public sealed class InteractionHudUI : MonoBehaviour
    {
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private Text selectionLabel;

        private Font cachedFont;

        private void Awake()
        {
            EnsureVisualTree();
        }

        public void SetSelectedBlock(byte blockType)
        {
            EnsureVisualTree();

            if (selectionLabel == null)
            {
                return;
            }

            selectionLabel.text = blockType switch
            {
                VoxelBlockIds.Grass => "Block: Grass [2]",
                _ => "Block: Stone [1]"
            };
        }

        private void EnsureVisualTree()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            if (GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (crosshairRoot == null)
            {
                crosshairRoot = CreateCrosshairRoot(transform);
            }

            if (selectionLabel == null)
            {
                selectionLabel = CreateSelectionLabel(transform);
            }
        }

        private RectTransform CreateCrosshairRoot(Transform parent)
        {
            var go = new GameObject("Crosshair", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var root = go.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(16f, 16f);
            root.anchoredPosition = Vector2.zero;

            CreateCrosshairBar(root, "Vertical", new Vector2(2f, 16f));
            CreateCrosshairBar(root, "Horizontal", new Vector2(16f, 2f));
            return root;
        }

        private void CreateCrosshairBar(RectTransform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
        }

        private Text CreateSelectionLabel(Transform parent)
        {
            var go = new GameObject("SelectedBlockLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(320f, 32f);
            rect.anchoredPosition = new Vector2(0f, 22f);

            var text = go.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.92f);
            text.text = "Block: Stone [1]";
            return text;
        }

        private Font GetDefaultFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }
    }
}
