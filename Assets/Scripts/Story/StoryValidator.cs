using System.Collections.Generic;
using System.Linq;

namespace TheTear.Story
{
    public static class StoryValidator
    {
        public static List<string> Validate(StoryModel story)
        {
            List<string> issues = new List<string>();
            if (story == null)
            {
                issues.Add("Story is null.");
                return issues;
            }

            if (story.clues == null || story.clues.Length == 0)
            {
                issues.Add("Story has no clues.");
            }

            if (story.objects == null || story.objects.Length == 0)
            {
                issues.Add("Story has no objects.");
            }

            var clueIds = new HashSet<string>();
            if (story.clues != null)
            {
                foreach (var clue in story.clues)
                {
                    if (string.IsNullOrEmpty(clue.id))
                    {
                        issues.Add("A clue is missing an id.");
                        continue;
                    }
                    if (!clueIds.Add(clue.id))
                    {
                        issues.Add("Duplicate clue id: " + clue.id);
                    }
                }
            }

            var objectIds = new HashSet<string>();
            if (story.objects != null)
            {
                foreach (var obj in story.objects)
                {
                    if (string.IsNullOrEmpty(obj.id))
                    {
                        issues.Add("An object is missing an id.");
                        continue;
                    }
                    if (!objectIds.Add(obj.id))
                    {
                        issues.Add("Duplicate object id: " + obj.id);
                    }
                }
            }

            var clusterIds = new HashSet<string>();
            var clusterMap = new Dictionary<string, ClusterData>();
            if (story.clusters != null)
            {
                foreach (var cluster in story.clusters)
                {
                    if (string.IsNullOrEmpty(cluster.id))
                    {
                        issues.Add("A cluster is missing an id.");
                        continue;
                    }
                    if (!clusterIds.Add(cluster.id))
                    {
                        issues.Add("Duplicate cluster id: " + cluster.id);
                    }
                    else
                    {
                        clusterMap[cluster.id] = cluster;
                    }

                    if (cluster.clueIds == null || cluster.clueIds.Length == 0)
                    {
                        issues.Add("Cluster " + cluster.id + " has no clues.");
                    }
                }
            }

            var clueMap = story.clues != null
                ? story.clues.Where(c => !string.IsNullOrEmpty(c.id)).ToDictionary(c => c.id, c => c)
                : new Dictionary<string, ClueData>();

            if (story.clues != null)
            {
                foreach (var clue in story.clues)
                {
                    if (!string.IsNullOrEmpty(clue.objectId) && !objectIds.Contains(clue.objectId))
                    {
                        issues.Add("Clue " + clue.id + " references missing objectId: " + clue.objectId);
                    }

                    if (clue.prerequisites != null)
                    {
                        foreach (var prereq in clue.prerequisites)
                        {
                            if (!clueIds.Contains(prereq))
                            {
                                issues.Add("Clue " + clue.id + " has missing prerequisite: " + prereq);
                            }
                        }
                    }

                    if (clue.revealsObjectIds != null)
                    {
                        foreach (var reveal in clue.revealsObjectIds)
                        {
                            if (!objectIds.Contains(reveal))
                            {
                                issues.Add("Clue " + clue.id + " reveals missing objectId: " + reveal);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(clue.clusterId) && !clusterIds.Contains(clue.clusterId))
                    {
                        issues.Add("Clue " + clue.id + " references missing clusterId: " + clue.clusterId);
                    }
                }
            }

            if (story.clusters != null)
            {
                foreach (var cluster in story.clusters)
                {
                    if (cluster.clueIds == null)
                    {
                        continue;
                    }

                    foreach (var clueId in cluster.clueIds)
                    {
                        if (!clueIds.Contains(clueId))
                        {
                            issues.Add("Cluster " + cluster.id + " references missing clueId: " + clueId);
                        }
                    }

                    if (!ClusterHasCompletionPath(cluster, clueMap))
                    {
                        issues.Add("Cluster " + cluster.id + " has no valid completion path.");
                    }
                }
            }

            if (HasCycle(clueMap))
            {
                issues.Add("Clue graph contains a cycle.");
            }

            if (story.solution == null)
            {
                issues.Add("Story solution is missing.");
            }
            else
            {
                if (story.culprits == null || !story.culprits.Contains(story.solution.culprit))
                {
                    issues.Add("Solution culprit is not in culprits list.");
                }
                if (story.methods == null || !story.methods.Contains(story.solution.method))
                {
                    issues.Add("Solution method is not in methods list.");
                }
                if (story.motives == null || !story.motives.Contains(story.solution.motive))
                {
                    issues.Add("Solution motive is not in motives list.");
                }
            }

            return issues;
        }

        private static bool HasCycle(Dictionary<string, ClueData> clueMap)
        {
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();

            foreach (var clueId in clueMap.Keys)
            {
                if (DetectCycle(clueId, clueMap, visiting, visited))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DetectCycle(string clueId, Dictionary<string, ClueData> clueMap, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(clueId))
            {
                return false;
            }
            if (visiting.Contains(clueId))
            {
                return true;
            }

            visiting.Add(clueId);
            if (clueMap.TryGetValue(clueId, out ClueData clue) && clue.prerequisites != null)
            {
                foreach (var prereq in clue.prerequisites)
                {
                    if (clueMap.ContainsKey(prereq) && DetectCycle(prereq, clueMap, visiting, visited))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(clueId);
            visited.Add(clueId);
            return false;
        }

        private static bool ClusterHasCompletionPath(ClusterData cluster, Dictionary<string, ClueData> clueMap)
        {
            if (cluster == null || cluster.clueIds == null || cluster.clueIds.Length == 0)
            {
                return false;
            }

            var clusterSet = new HashSet<string>(cluster.clueIds);
            var unlocked = new HashSet<string>();
            bool progress = true;

            while (progress)
            {
                progress = false;
                foreach (var clueId in cluster.clueIds)
                {
                    if (unlocked.Contains(clueId))
                    {
                        continue;
                    }

                    if (!clueMap.TryGetValue(clueId, out ClueData clue))
                    {
                        continue;
                    }

                    bool prereqsMet = true;
                    if (clue.prerequisites != null)
                    {
                        foreach (var prereq in clue.prerequisites)
                        {
                            if (clusterSet.Contains(prereq) && !unlocked.Contains(prereq))
                            {
                                prereqsMet = false;
                                break;
                            }
                        }
                    }

                    if (prereqsMet)
                    {
                        unlocked.Add(clueId);
                        progress = true;
                    }
                }
            }

            return unlocked.Count == clusterSet.Count;
        }
    }
}
