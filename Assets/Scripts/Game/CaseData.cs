using System;
using UnityEngine;

namespace MaskedMurderer.Game
{
    [Serializable]
    public class CaseFile
    {
        public string caseId;
        public string title;
        public string victim;
        public CaseSolution solution;
        public string[] culprits;
        public string[] methods;
        public string[] motives;
        public string[] essentials;
        public CaseCluster[] clusters;
        public CaseObject[] objects;
        public ClueDefinition[] clues;
    }

    [Serializable]
    public class CaseSolution
    {
        public string culprit;
        public string method;
        public string motive;
    }

    [Serializable]
    public class CaseCluster
    {
        public string id;
        public string name;
        public string[] clueIds;
    }

    [Serializable]
    public class CaseObject
    {
        public string id;
        public string label;
        public string recipe;
        public string mode;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public Vector3 localScale;
        public bool startsVisible;
    }

    [Serializable]
    public class ClueDefinition
    {
        public string id;
        public string title;
        public string description;
        public string summary;
        public string objectId;
        public string[] prerequisites;
        public string[] revealsObjectIds;
        public string clusterId;
        public bool required;
        public string unlockText;

        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            if (!string.IsNullOrEmpty(summary))
            {
                return summary;
            }
            return string.Empty;
        }
    }
}
