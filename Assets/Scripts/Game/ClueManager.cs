using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaskedMurderer.Game
{
    public class ClueManager : MonoBehaviour
    {
        public event Action<ClueDefinition> OnClueUnlocked;
        public event Action<ClueDefinition> OnClueUnlockBlocked;

        public CaseFile CaseFile => caseFile;
        public int RequiredClueCount => requiredClueCount;
        public int TotalClueCount => totalClueCount;
        public int UnlockedCount => unlocked.Count;

        private CaseFile caseFile;
        private readonly Dictionary<string, ClueDefinition> clueMap = new Dictionary<string, ClueDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int requiredClueCount;
        private int totalClueCount;

        public void Initialize(CaseFile caseData)
        {
            caseFile = caseData;
            clueMap.Clear();
            unlocked.Clear();
            requiredClueCount = 0;
            totalClueCount = 0;

            if (caseFile == null || caseFile.clues == null)
            {
                return;
            }

            foreach (ClueDefinition clue in caseFile.clues)
            {
                if (clue == null || string.IsNullOrEmpty(clue.id))
                {
                    continue;
                }
                clueMap[clue.id] = clue;
                totalClueCount++;
                if (clue.required)
                {
                    requiredClueCount++;
                }
            }

            if (requiredClueCount == 0)
            {
                requiredClueCount = totalClueCount;
            }
        }

        public IEnumerable<ClueDefinition> GetAllClues()
        {
            return caseFile != null && caseFile.clues != null ? caseFile.clues : Array.Empty<ClueDefinition>();
        }

        public bool IsUnlocked(string clueId)
        {
            return unlocked.Contains(clueId);
        }

        public bool IsEligible(string clueId)
        {
            if (!clueMap.TryGetValue(clueId, out ClueDefinition clue))
            {
                return false;
            }
            if (unlocked.Contains(clueId))
            {
                return false;
            }
            if (clue.prerequisites == null || clue.prerequisites.Length == 0)
            {
                return true;
            }
            foreach (string prereq in clue.prerequisites)
            {
                if (!unlocked.Contains(prereq))
                {
                    return false;
                }
            }
            return true;
        }

        public bool TryUnlockClue(string clueId, string source)
        {
            if (!clueMap.TryGetValue(clueId, out ClueDefinition clue))
            {
                return false;
            }

            if (!IsEligible(clueId))
            {
                if (string.Equals(source, "tap", StringComparison.OrdinalIgnoreCase))
                {
                    OnClueUnlockBlocked?.Invoke(clue);
                }
                return false;
            }

            unlocked.Add(clueId);
            OnClueUnlocked?.Invoke(clue);
            return true;
        }
    }
}
