using System.Collections.Generic;
using UnityEngine;

namespace MaskedMurderer.Game
{
    public class ClueSpawnController : MonoBehaviour
    {
        [SerializeField] private ARPlaceClueLocation placement;
        [SerializeField] private Transform sceneRoot;
        [SerializeField] private PrefabLibrary prefabLibrary;
        [SerializeField] private GameObject fallbackPrefab;
        [SerializeField] private float extraClueRadius = 0.18f;

        private ClueManager clueManager;
        private bool spawned;
        private Transform clueRoot;

        public void Initialize(ARPlaceClueLocation placementSource, Transform root, PrefabLibrary library, ClueManager manager)
        {
            placement = placementSource;
            sceneRoot = root;
            prefabLibrary = library;
            clueManager = manager;
        }

        public void SpawnClues(CaseFile caseFile)
        {
            if (spawned || caseFile == null || caseFile.clues == null || caseFile.clues.Length == 0)
            {
                return;
            }

            if (placement == null || placement.ClueLocations == null || placement.ClueLocations.Count == 0)
            {
                Debug.LogWarning("ClueSpawnController: No boundary locations available.");
                return;
            }

            if (prefabLibrary == null)
            {
                prefabLibrary = Resources.Load<PrefabLibrary>("CluePrefabLibrary");
            }

            if (clueManager == null)
            {
                clueManager = FindObjectOfType<ClueManager>();
            }

            if (sceneRoot == null)
            {
                sceneRoot = placement.transform;
            }

            if (clueRoot == null)
            {
                GameObject rootGo = new GameObject("ClueRoot");
                rootGo.transform.SetParent(sceneRoot, false);
                clueRoot = rootGo.transform;
            }

            List<Vector3> locations = placement.ClueLocations;
            List<Quaternion> rotations = placement.ClueRotations;
            int boundaryCount = locations.Count;

            for (int i = 0; i < caseFile.clues.Length; i++)
            {
                ClueDefinition clue = caseFile.clues[i];
                if (clue == null || string.IsNullOrEmpty(clue.id))
                {
                    continue;
                }

                int boundaryIndex = i % boundaryCount;
                int ringIndex = i / boundaryCount;

                Vector3 position = locations[boundaryIndex];
                Quaternion rotation = rotations != null && rotations.Count > boundaryIndex ? rotations[boundaryIndex] : Quaternion.identity;
                if (ringIndex > 0)
                {
                    position += ComputeOffset(boundaryIndex, ringIndex, i);
                }

                GameObject prefab = ResolvePrefab(clue.id);
                GameObject instance = Instantiate(prefab, position, rotation);
                instance.name = clue.id + "_Clue";
                instance.transform.SetParent(clueRoot, true);

                EnsureCollider(instance);
                InteractableClue interactable = instance.GetComponent<InteractableClue>();
                if (interactable == null)
                {
                    interactable = instance.AddComponent<InteractableClue>();
                }
                if (clueManager != null)
                {
                    interactable.Initialize(clueManager, clue.id);
                }
                else
                {
                    Debug.LogWarning("ClueSpawnController: Missing ClueManager for " + clue.id);
                }

                CluePulse pulse = instance.GetComponent<CluePulse>();
                if (pulse == null)
                {
                    pulse = instance.AddComponent<CluePulse>();
                }
            }

            spawned = true;
        }

        private GameObject ResolvePrefab(string clueId)
        {
            if (prefabLibrary != null && prefabLibrary.TryGetPrefab(clueId, out GameObject prefab))
            {
                return prefab;
            }

            if (fallbackPrefab != null)
            {
                return fallbackPrefab;
            }

            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        private Vector3 ComputeOffset(int boundaryIndex, int ringIndex, int seed)
        {
            float angle = (seed * 137.5f) * Mathf.Deg2Rad;
            float radius = extraClueRadius * ringIndex;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void EnsureCollider(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.2f;
            collider.center = Vector3.zero;
        }
    }
}
