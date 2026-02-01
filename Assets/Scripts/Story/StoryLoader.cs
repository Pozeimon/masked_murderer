using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TheTear.Story
{
    public static class StoryLoader
    {
        public static IEnumerator LoadStory(Action<StoryModel, string> onComplete)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "story", "case.json");
            string json = null;
            string error = null;

            if (path.Contains("://") || path.Contains(":///"))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(path))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        error = request.error;
                    }
                    else
                    {
                        json = request.downloadHandler.text;
                    }
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    error = "Missing case.json at " + path;
                }
                else
                {
                    json = File.ReadAllText(path);
                }
            }

            StoryModel model = null;
            if (!string.IsNullOrEmpty(json))
            {
                model = JsonUtility.FromJson<StoryModel>(json);
                if (model == null)
                {
                    error = "Failed to parse case.json";
                }
            }

            onComplete?.Invoke(model, error);
        }

#if UNITY_EDITOR
        public static StoryModel LoadStoryBlocking(out string error)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "story", "case.json");
            error = null;
            if (!File.Exists(path))
            {
                error = "Missing case.json at " + path;
                return null;
            }

            string json = File.ReadAllText(path);
            StoryModel model = JsonUtility.FromJson<StoryModel>(json);
            if (model == null)
            {
                error = "Failed to parse case.json";
            }
            return model;
        }
#endif
    }
}
