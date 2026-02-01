using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TheTear.Characters;
using TheTear.Story;

namespace TheTear.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        public GameObject panelRoot;
        public Button resumeButton;
        public Button quitButton;
        public Component characterText;
        public Component clueListText;

        public event Action<bool> OnPauseChanged;

        private ClueManager clueManager;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Hide);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        public void Initialize(ClueManager manager)
        {
            clueManager = manager;
            Hide();
        }

        public void Show()
        {
            if (panelRoot == null)
            {
                return;
            }
            panelRoot.SetActive(true);
            Time.timeScale = 0f;
            Refresh();
            OnPauseChanged?.Invoke(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }

        public void Refresh()
        {
            if (clueManager == null)
            {
                return;
            }

            StringBuilder charDesc = new StringBuilder();
            charDesc.AppendLine(CharacterDescriptions.GetDescription(CharacterMode.Matter));
            charDesc.AppendLine(CharacterDescriptions.GetDescription(CharacterMode.Void));
            charDesc.AppendLine(CharacterDescriptions.GetDescription(CharacterMode.Flow));
            UITextHelper.SetText(characterText, charDesc.ToString().TrimEnd());

            StringBuilder clues = new StringBuilder();
            foreach (var clue in clueManager.GetAllClues())
            {
                if (clueManager.IsUnlocked(clue.id))
                {
                    clues.Append("- ").Append(clue.title).Append("\n");
                }
            }
            if (clues.Length == 0)
            {
                clues.Append("(No clues unlocked yet)");
            }
            UITextHelper.SetText(clueListText, clues.ToString().TrimEnd());
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
