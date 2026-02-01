using System.Collections.Generic;

namespace TheTear.Story
{
    public class ClueGraph
    {
        public class Node
        {
            public ClueData data;
            public List<Node> prereqs = new List<Node>();
        }

        private readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>();

        public IEnumerable<Node> Nodes => nodes.Values;

        public Node GetNode(string id)
        {
            nodes.TryGetValue(id, out Node node);
            return node;
        }

        public static ClueGraph Build(StoryModel story)
        {
            var graph = new ClueGraph();
            if (story == null || story.clues == null)
            {
                return graph;
            }

            foreach (ClueData clue in story.clues)
            {
                graph.nodes[clue.id] = new Node { data = clue };
            }

            foreach (ClueData clue in story.clues)
            {
                if (clue.prerequisites == null)
                {
                    continue;
                }

                Node node = graph.nodes[clue.id];
                foreach (string prereqId in clue.prerequisites)
                {
                    if (graph.nodes.TryGetValue(prereqId, out Node prereq))
                    {
                        node.prereqs.Add(prereq);
                    }
                }
            }

            return graph;
        }
    }
}
