using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TheTear.UI
{
    public class ErrorPanelController : MonoBehaviour
    {
        public GameObject panelRoot;
        public Component errorText;
        public Button closeButton;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void ShowErrors(List<string> issues, bool allowClose)
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(true);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ERRORS:");
            foreach (var issue in issues)
            {
                sb.AppendLine("- " + issue);
            }
            UITextHelper.SetText(errorText, sb.ToString().TrimEnd());

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(allowClose);
                closeButton.interactable = allowClose;
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
