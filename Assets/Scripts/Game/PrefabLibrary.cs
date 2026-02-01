using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaskedMurderer.Game
{
    [CreateAssetMenu(menuName = "MaskedMurderer/Prefab Library", fileName = "CluePrefabLibrary")]
    public class PrefabLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public GameObject prefab;
        }

        public Entry[] entries;

        private Dictionary<string, GameObject> lookup;

        public bool TryGetPrefab(string key, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            BuildLookup();
            return lookup.TryGetValue(key, out prefab) && prefab != null;
        }

        private void BuildLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            if (entries == null)
            {
                return;
            }

            foreach (Entry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.key) || entry.prefab == null)
                {
                    continue;
                }
                lookup[entry.key] = entry.prefab;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lookup = null;
        }
#endif
    }
}
