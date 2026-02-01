using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MaskedMurderer.Game
{
    public class JournalController : MonoBehaviour
    {
        [SerializeField] private Text storyText;
        [SerializeField] private Text clueListText;

        private CaseFile caseFile;
        private ClueManager clueManager;
        private string storyIntro;

        public void AssignTextTargets(Text story, Text clues)
        {
            storyText = story;
            clueListText = clues;
            if (storyText != null)
            {
                storyText.text = storyIntro ?? string.Empty;
            }
            Refresh();
        }

        public void Initialize(CaseFile data, ClueManager manager, string story)
        {
            caseFile = data;
            clueManager = manager;
            storyIntro = story ?? string.Empty;
            if (storyText != null)
            {
                storyText.text = storyIntro;
            }

            if (clueManager != null)
            {
                clueManager.OnClueUnlocked += HandleClueChanged;
                clueManager.OnClueUnlockBlocked += HandleClueChanged;
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (clueManager != null)
            {
                clueManager.OnClueUnlocked -= HandleClueChanged;
                clueManager.OnClueUnlockBlocked -= HandleClueChanged;
            }
        }

        private void HandleClueChanged(ClueDefinition clue)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (clueListText == null)
            {
                return;
            }

            if (caseFile == null || caseFile.clues == null)
            {
                clueListText.text = "No case data loaded.";
                return;
            }

            int unlockedCount = 0;
            foreach (var clue in caseFile.clues)
            {
                if (clueManager != null && clueManager.IsUnlocked(clue.id))
                {
                    unlockedCount++;
                }
            }

            StringBuilder sb = new StringBuilder();
            if (storyText == null && !string.IsNullOrEmpty(storyIntro))
            {
                sb.AppendLine(storyIntro.Trim());
                sb.AppendLine();
            }
            sb.AppendLine("Progress: " + unlockedCount + "/" + caseFile.clues.Length);

            Dictionary<string, CaseCluster> clusterMap = new Dictionary<string, CaseCluster>();
            if (caseFile.clusters != null)
            {
                foreach (var cluster in caseFile.clusters)
                {
                    if (cluster != null && !string.IsNullOrEmpty(cluster.id))
                    {
                        clusterMap[cluster.id] = cluster;
                    }
                }
            }

            if (caseFile.clusters != null && caseFile.clusters.Length > 0)
            {
                foreach (var cluster in caseFile.clusters)
                {
                    if (cluster == null || cluster.clueIds == null)
                    {
                        continue;
                    }

                    int clusterUnlocked = 0;
                    foreach (string clueId in cluster.clueIds)
                    {
                        if (clueManager != null && clueManager.IsUnlocked(clueId))
                        {
                            clusterUnlocked++;
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine("[" + cluster.name + "] " + clusterUnlocked + "/" + cluster.clueIds.Length);
                    foreach (string clueId in cluster.clueIds)
                    {
                        ClueDefinition clue = FindClue(clueId);
                        if (clue == null)
                        {
                            continue;
                        }

                        bool unlocked = clueManager != null && clueManager.IsUnlocked(clue.id);
                        if (unlocked)
                        {
                            sb.AppendLine("- " + clue.title + ": " + clue.GetDescription());
                        }
                        else
                        {
                            sb.AppendLine("- ???");
                        }
                    }
                }
            }
            else
            {
                foreach (var clue in caseFile.clues)
                {
                    if (clue == null)
                    {
                        continue;
                    }
                    bool unlocked = clueManager != null && clueManager.IsUnlocked(clue.id);
                    if (unlocked)
                    {
                        sb.AppendLine("- " + clue.title + ": " + clue.GetDescription());
                    }
                    else
                    {
                        sb.AppendLine("- ???");
                    }
                }
            }

            clueListText.text = sb.ToString();
        }

        private ClueDefinition FindClue(string clueId)
        {
            if (caseFile == null || caseFile.clues == null)
            {
                return null;
            }

            foreach (var clue in caseFile.clues)
            {
                if (clue != null && clue.id == clueId)
                {
                    return clue;
                }
            }

            return null;
        }
    }
}
