using System;
using System.Collections.Generic;
using UnityEngine;
using TheTear.AR;
using TheTear.Factory;
using TheTear.Interaction;

namespace TheTear.Story
{
    public class ClueManager : MonoBehaviour
    {
        public event Action<ClueData> OnClueUnlocked;
        public event Action<ClueData> OnClueUnlockBlocked;
        public event Action<ClusterData> OnClusterCompleted;
        public event Action<bool> OnDeductionAvailabilityChanged;

        public StoryModel Story => story;
        public bool IsDeductionAvailable => deductionAvailable;
        public bool HasSpawned => spawned;
        public int EligibleCount => eligible.Count;

        public PrefabLibrary prefabLibrary;

        private StoryModel story;
        private SceneRootController sceneRoot;
        private readonly Dictionary<string, ClueData> clueMap = new Dictionary<string, ClueData>();
        private readonly Dictionary<string, StoryObjectData> objectDataMap = new Dictionary<string, StoryObjectData>();
        private readonly Dictionary<string, GameObject> objectInstances = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, string> objectToClue = new Dictionary<string, string>();
        private readonly HashSet<string> unlocked = new HashSet<string>();
        private readonly HashSet<string> revealed = new HashSet<string>();
        private readonly HashSet<string> eligible = new HashSet<string>();
        private readonly HashSet<string> completedClusters = new HashSet<string>();
        private readonly Dictionary<GameObject, Coroutine> pulseRoutines = new Dictionary<GameObject, Coroutine>();
        private bool spawned;
        private bool deductionAvailable;

        public void Initialize(StoryModel model, SceneRootController root)
        {
            story = model;
            sceneRoot = root;
            spawned = false;
            unlocked.Clear();
            revealed.Clear();
            eligible.Clear();
            completedClusters.Clear();
            clueMap.Clear();
            objectDataMap.Clear();
            objectToClue.Clear();
            objectInstances.Clear();
            pulseRoutines.Clear();

            if (story != null && story.clues != null)
            {
                foreach (var clue in story.clues)
                {
                    clueMap[clue.id] = clue;
                    if (!string.IsNullOrEmpty(clue.objectId))
                    {
                        objectToClue[clue.objectId] = clue.id;
                    }
                }
            }

            if (story != null && story.objects != null)
            {
                foreach (var obj in story.objects)
                {
                    objectDataMap[obj.id] = obj;
                    if (obj.startsVisible)
                    {
                        revealed.Add(obj.id);
                    }
                }
            }

            if (sceneRoot != null)
            {
                sceneRoot.SetActive(false);
            }

            RecomputeState();
        }

        public void SpawnClues()
        {
            if (spawned || story == null || sceneRoot == null)
            {
                return;
            }

            sceneRoot.SetActive(true);
            spawned = true;

            var placedPositions = new List<Vector3>();
            foreach (var obj in story.objects)
            {
                obj.localPosition = ApplySpacing(obj.localPosition, placedPositions);
                placedPositions.Add(obj.localPosition);

                GameObject go = ClueFactory.Create(obj, sceneRoot.transform, prefabLibrary);
                objectInstances[obj.id] = go;
                bool visible = revealed.Contains(obj.id);
                go.SetActive(visible);

                if (objectToClue.TryGetValue(obj.id, out string clueId))
                {
                    var interactable = go.AddComponent<InteractableClue>();
                    interactable.Initialize(this, clueId);
                }
            }
        }

        public bool IsUnlocked(string clueId)
        {
            return unlocked.Contains(clueId);
        }

        public bool IsEligible(string clueId)
        {
            if (!clueMap.TryGetValue(clueId, out ClueData clue))
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
            foreach (var prereq in clue.prerequisites)
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
            if (!clueMap.TryGetValue(clueId, out ClueData clue))
            {
                return false;
            }

            if (!IsEligible(clueId))
            {
                if (source == "tap")
                {
                    OnClueUnlockBlocked?.Invoke(clue);
                }
                return false;
            }

            unlocked.Add(clueId);

            if (clue.revealsObjectIds != null)
            {
                foreach (var revealId in clue.revealsObjectIds)
                {
                    revealed.Add(revealId);
                    if (objectInstances.TryGetValue(revealId, out GameObject revealObj))
                    {
                        revealObj.SetActive(true);
                        PulseObject(revealObj);
                    }
                }
            }

            RecomputeState();
            OnClueUnlocked?.Invoke(clue);
            CheckClusterCompletion();
            PulseClueObject(clue.objectId);
            return true;
        }

        public bool UnlockFirstEligibleFromJournal()
        {
            if (story == null || story.clues == null)
            {
                return false;
            }

            foreach (var clue in story.clues)
            {
                if (IsEligible(clue.id) && TryUnlockClue(clue.id, "journal"))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<ClueData> GetAllClues()
        {
            return story != null ? story.clues : Array.Empty<ClueData>();
        }

        public IEnumerable<ClusterData> GetClusters()
        {
            return story != null ? story.clusters : Array.Empty<ClusterData>();
        }

        public int GetClusterUnlockedCount(string clusterId)
        {
            int count = 0;
            if (story == null || story.clusters == null)
            {
                return count;
            }
            foreach (var cluster in story.clusters)
            {
                if (cluster.id != clusterId || cluster.clueIds == null)
                {
                    continue;
                }
                foreach (var clueId in cluster.clueIds)
                {
                    if (unlocked.Contains(clueId))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public int GetClusterTotalCount(string clusterId)
        {
            if (story == null || story.clusters == null)
            {
                return 0;
            }
            foreach (var cluster in story.clusters)
            {
                if (cluster.id == clusterId)
                {
                    return cluster.clueIds != null ? cluster.clueIds.Length : 0;
                }
            }
            return 0;
        }

        private void RecomputeState()
        {
            eligible.Clear();
            if (story != null && story.clues != null)
            {
                foreach (var clue in story.clues)
                {
                    if (IsEligible(clue.id))
                    {
                        eligible.Add(clue.id);
                    }
                }
            }

            bool wasAvailable = deductionAvailable;
            deductionAvailable = ComputeDeductionAvailable();
            if (deductionAvailable != wasAvailable)
            {
                OnDeductionAvailabilityChanged?.Invoke(deductionAvailable);
            }
        }

        private void CheckClusterCompletion()
        {
            if (story == null || story.clusters == null)
            {
                return;
            }

            foreach (var cluster in story.clusters)
            {
                if (cluster == null || string.IsNullOrEmpty(cluster.id) || completedClusters.Contains(cluster.id))
                {
                    continue;
                }

                if (cluster.clueIds == null || cluster.clueIds.Length == 0)
                {
                    continue;
                }

                bool complete = true;
                foreach (var clueId in cluster.clueIds)
                {
                    if (!unlocked.Contains(clueId))
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                {
                    completedClusters.Add(cluster.id);
                    OnClusterCompleted?.Invoke(cluster);
                }
            }
        }

        public void PulseClueObject(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                return;
            }

            if (objectInstances.TryGetValue(objectId, out GameObject target))
            {
                PulseObject(target);
            }
        }

        private void PulseObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (pulseRoutines.TryGetValue(target, out Coroutine routine) && routine != null)
            {
                StopCoroutine(routine);
            }

            pulseRoutines[target] = StartCoroutine(PulseRoutine(target.transform));
        }

        private System.Collections.IEnumerator PulseRoutine(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 startScale = target.localScale;
            Vector3 peakScale = startScale * 1.18f;
            const float upDuration = 0.15f;
            const float downDuration = 0.2f;

            float t = 0f;
            while (t < upDuration)
            {
                t += Time.unscaledDeltaTime;
                float lerp = Mathf.Clamp01(t / upDuration);
                target.localScale = Vector3.Lerp(startScale, peakScale, lerp);
                yield return null;
            }

            t = 0f;
            while (t < downDuration)
            {
                t += Time.unscaledDeltaTime;
                float lerp = Mathf.Clamp01(t / downDuration);
                target.localScale = Vector3.Lerp(peakScale, startScale, lerp);
                yield return null;
            }

            target.localScale = startScale;
        }

        private Vector3 ApplySpacing(Vector3 localPosition, List<Vector3> existingPositions)
        {
            const float minSpacing = 0.15f;
            const float maxRadius = 2f;

            Vector3 adjusted = ClampToRadius(localPosition, maxRadius);

            if (existingPositions == null || existingPositions.Count == 0)
            {
                return adjusted;
            }

            for (int i = 0; i < existingPositions.Count; i++)
            {
                float dist = Vector3.Distance(adjusted, existingPositions[i]);
                if (dist >= minSpacing)
                {
                    continue;
                }

                Vector3 radial = new Vector3(adjusted.x, 0f, adjusted.z);
                if (radial.sqrMagnitude < 0.0001f)
                {
                    radial = new Vector3(existingPositions[i].x, 0f, existingPositions[i].z);
                }
                if (radial.sqrMagnitude < 0.0001f)
                {
                    radial = Vector3.forward;
                }

                Vector3 push = radial.normalized * (minSpacing - dist);
                adjusted += new Vector3(push.x, 0f, push.z);
            }

            return ClampToRadius(adjusted, maxRadius);
        }

        private Vector3 ClampToRadius(Vector3 localPosition, float maxRadius)
        {
            Vector3 radial = new Vector3(localPosition.x, 0f, localPosition.z);
            float radius = radial.magnitude;
            if (radius <= maxRadius || radius <= 0.0001f)
            {
                return localPosition;
            }

            Vector3 clamped = radial.normalized * maxRadius;
            return new Vector3(clamped.x, localPosition.y, clamped.z);
        }

        private bool ComputeDeductionAvailable()
        {
            if (story == null || story.clues == null)
            {
                return false;
            }

            bool allRequired = true;
            foreach (var clue in story.clues)
            {
                if (clue.required && !unlocked.Contains(clue.id))
                {
                    allRequired = false;
                    break;
                }
            }

            bool essentialsMet = true;
            if (story.essentials != null && story.essentials.Length > 0)
            {
                foreach (var essential in story.essentials)
                {
                    if (!unlocked.Contains(essential))
                    {
                        essentialsMet = false;
                        break;
                    }
                }
            }

            bool anyClusterComplete = false;
            if (story.clusters != null)
            {
                foreach (var cluster in story.clusters)
                {
                    if (cluster.clueIds == null || cluster.clueIds.Length == 0)
                    {
                        continue;
                    }
                    bool clusterComplete = true;
                    foreach (var clueId in cluster.clueIds)
                    {
                        if (!unlocked.Contains(clueId))
                        {
                            clusterComplete = false;
                            break;
                        }
                    }
                    if (clusterComplete)
                    {
                        anyClusterComplete = true;
                        break;
                    }
                }
            }

            return allRequired || (anyClusterComplete && essentialsMet);
        }
    }
}
