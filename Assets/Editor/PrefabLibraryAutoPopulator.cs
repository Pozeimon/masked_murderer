using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaskedMurderer.Game
{
    public static class PrefabLibraryAutoPopulator
    {
        private const string LibraryPath = "Assets/Resources/CluePrefabLibrary.asset";
        private const string PrefabFolder = "Assets/Prefabs/Clues";

        [MenuItem("MaskedMurderer/Refresh Prefab Library")]
        public static void RefreshLibrary()
        {
            PrefabLibrary library = GetOrCreateLibrary();
            if (library == null)
            {
                return;
            }

            PopulateLibrary(library);
        }

        [InitializeOnLoadMethod]
        private static void AutoRefresh()
        {
            PrefabLibrary library = AssetDatabase.LoadAssetAtPath<PrefabLibrary>(LibraryPath);
            if (library != null)
            {
                PopulateLibrary(library);
            }
        }

        private static PrefabLibrary GetOrCreateLibrary()
        {
            PrefabLibrary library = AssetDatabase.LoadAssetAtPath<PrefabLibrary>(LibraryPath);
            if (library != null)
            {
                return library;
            }

            library = ScriptableObject.CreateInstance<PrefabLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static void PopulateLibrary(PrefabLibrary library)
        {
            if (library == null)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                Debug.LogWarning("PrefabLibraryAutoPopulator: Missing folder " + PrefabFolder);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            List<PrefabLibrary.Entry> entries = new List<PrefabLibrary.Entry>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                string clueId = name.Split('_').FirstOrDefault();
                if (string.IsNullOrEmpty(clueId) || !clueId.StartsWith("C"))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                entries.Add(new PrefabLibrary.Entry
                {
                    key = clueId,
                    prefab = prefab
                });
            }

            library.entries = entries.OrderBy(entry => entry.key).ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }
    }
}
