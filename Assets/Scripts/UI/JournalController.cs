using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TheTear.Story;

namespace TheTear.UI
{
    public class JournalController : MonoBehaviour
    {
        public GameObject panelRoot;
        public Component titleText;
        public Component clusterText;
        public Component clueListText;
        public Button closeButton;
        public Button unlockButton;
        public Button deductionButton;
        public ToastController toast;

        private ClueManager clueManager;
        private DeductionController deduction;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (unlockButton != null) unlockButton.onClick.AddListener(UnlockEligible);
            if (deductionButton != null) deductionButton.onClick.AddListener(OpenDeduction);
        }

        public void Initialize(ClueManager manager, DeductionController deductionController, ToastController toastController = null)
        {
            clueManager = manager;
            deduction = deductionController;
            toast = toastController;
            Refresh();
            Hide();
        }

        public void Toggle()
        {
            if (panelRoot == null)
            {
                return;
            }
            bool show = !panelRoot.activeSelf;
            panelRoot.SetActive(show);
            if (show)
            {
                Refresh();
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Refresh()
        {
            if (clueManager == null)
            {
                return;
            }

            StoryModel story = clueManager.Story;
            if (story != null && titleText != null)
            {
                string caseTitle = !string.IsNullOrEmpty(story.caseTitle) ? story.caseTitle : story.title;
                if (!string.IsNullOrEmpty(caseTitle))
                {
                    UITextHelper.SetText(titleText, caseTitle);
                }
            }

            StringBuilder clusters = new StringBuilder();
            foreach (var cluster in clueManager.GetClusters())
            {
                int unlockedCount = clueManager.GetClusterUnlockedCount(cluster.id);
                int total = clueManager.GetClusterTotalCount(cluster.id);
                string clusterTitle = !string.IsNullOrEmpty(cluster.title) ? cluster.title : cluster.name;
                clusters.Append(clusterTitle).Append(" [").Append(cluster.id).Append("] ")
                    .Append(unlockedCount).Append("/").Append(total).Append("\n");

                if (!string.IsNullOrEmpty(cluster.description))
                {
                    clusters.Append(cluster.description).Append("\n");
                }

                if (total > 0 && unlockedCount == total && !string.IsNullOrEmpty(cluster.completionText))
                {
                    clusters.Append(cluster.completionText).Append("\n");
                }

                clusters.Append("\n");
            }
            UITextHelper.SetText(clusterText, clusters.ToString().TrimEnd());

            StringBuilder list = new StringBuilder();
            foreach (var clue in clueManager.GetAllClues())
            {
                string status = clueManager.IsUnlocked(clue.id) ? "[x]" : (clueManager.IsEligible(clue.id) ? "[>]" : "[ ]");
                list.Append(status).Append(" ").Append(clue.id).Append(" ").Append(clue.title);
                string description = !string.IsNullOrEmpty(clue.description) ? clue.description : clue.summary;
                if (!string.IsNullOrEmpty(description))
                {
                    list.Append(" - ").Append(description);
                }
                list.Append("\n");
            }
            UITextHelper.SetText(clueListText, list.ToString().TrimEnd());

            if (deductionButton != null)
            {
                deductionButton.interactable = clueManager.IsDeductionAvailable;
            }

            if (unlockButton != null)
            {
                unlockButton.interactable = clueManager.EligibleCount > 0;
            }
        }

        private void UnlockEligible()
        {
            if (clueManager != null)
            {
                bool unlockedAny = clueManager.UnlockFirstEligibleFromJournal();
                if (!unlockedAny && toast != null)
                {
                    toast.Show("No eligible clues.");
                }
                Refresh();
            }
        }

        private void OpenDeduction()
        {
            if (deduction != null && clueManager != null && clueManager.IsDeductionAvailable)
            {
                deduction.Show();
            }
        }
    }
}
