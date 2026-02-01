using System;

namespace TheTear.Story
{
    [Serializable]
    public class ClusterData
    {
        public string id;
        public string name;
        public string title;
        public string description;
        public string completionText;
        public string[] clueIds;
    }
}
