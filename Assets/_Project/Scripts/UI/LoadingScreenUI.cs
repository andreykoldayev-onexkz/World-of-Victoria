using UnityEngine;
using UnityEngine.UI;

namespace WorldOfVictoria.UI
{
    public sealed class LoadingScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text statusText;
        [SerializeField] private Text detailText;

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        public void SetProgress(float progress, string status, string detail = "")
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(progress);
            }

            if (statusText != null)
            {
                statusText.text = status;
            }

            if (detailText != null)
            {
                detailText.text = detail;
            }
        }
    }
}
