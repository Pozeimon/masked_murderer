using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MaskedMurderer.Game
{
    public static class CaseLoader
    {
        public static CaseFile LoadCase(out string error)
        {
            error = null;
            string json = LoadTextByFallbacks(new[]
            {
                Path.Combine(Application.dataPath, "..", "case.json"),
                Path.Combine(Application.streamingAssetsPath, "case.json"),
                Path.Combine(Application.streamingAssetsPath, "story", "case.json")
            }, out error);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            CaseFile caseFile = JsonUtility.FromJson<CaseFile>(json);
            if (caseFile == null)
            {
                error = "Failed to parse case.json";
            }
            return caseFile;
        }

        public static IEnumerator LoadCaseAsync(Action<CaseFile, string> onComplete)
        {
            string error = null;
            string json = null;

            yield return LoadTextByFallbacksAsync(new[]
            {
                Path.Combine(Application.streamingAssetsPath, "case.json"),
                Path.Combine(Application.streamingAssetsPath, "story", "case.json"),
                Path.Combine(Application.dataPath, "..", "case.json")
            }, (text, err) =>
            {
                json = text;
                error = err;
            });

            CaseFile caseFile = null;
            if (!string.IsNullOrEmpty(json))
            {
                caseFile = JsonUtility.FromJson<CaseFile>(json);
                if (caseFile == null)
                {
                    error = "Failed to parse case.json";
                }
            }

            onComplete?.Invoke(caseFile, error);
        }

        public static string LoadStoryText(out string error)
        {
            error = null;
            string story = LoadTextByFallbacks(new[]
            {
                Path.Combine(Application.dataPath, "..", "story.txt"),
                Path.Combine(Application.streamingAssetsPath, "story.txt")
            }, out error);
            return story;
        }

        public static IEnumerator LoadStoryAsync(Action<string, string> onComplete)
        {
            string story = null;
            string error = null;

            yield return LoadTextByFallbacksAsync(new[]
            {
                Path.Combine(Application.streamingAssetsPath, "story.txt"),
                Path.Combine(Application.dataPath, "..", "story.txt")
            }, (text, err) =>
            {
                story = text;
                error = err;
            });

            onComplete?.Invoke(story, error);
        }

        private static string LoadTextByFallbacks(string[] paths, out string error)
        {
            error = null;
            if (paths == null || paths.Length == 0)
            {
                error = "No paths provided";
                return null;
            }

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    try
                    {
                        return File.ReadAllText(path);
                    }
                    catch (IOException ioEx)
                    {
                        error = "Failed to read " + path + ": " + ioEx.Message;
                        return null;
                    }
                }
            }

            error = "Missing file at " + string.Join(" or ", paths);
            return null;
        }

        private static IEnumerator LoadTextByFallbacksAsync(string[] paths, Action<string, string> onComplete)
        {
            if (paths == null || paths.Length == 0)
            {
                onComplete?.Invoke(null, "No paths provided");
                yield break;
            }

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (path.Contains("://") || path.Contains(":///"))
                {
                    using (UnityWebRequest request = UnityWebRequest.Get(path))
                    {
                        yield return request.SendWebRequest();
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            onComplete?.Invoke(request.downloadHandler.text, null);
                            yield break;
                        }
                    }
                }
                else if (File.Exists(path))
                {
                    string text = null;
                    try
                    {
                        text = File.ReadAllText(path);
                    }
                    catch (IOException ioEx)
                    {
                        onComplete?.Invoke(null, "Failed to read " + path + ": " + ioEx.Message);
                        yield break;
                    }

                    onComplete?.Invoke(text, null);
                    yield break;
                }
            }

            onComplete?.Invoke(null, "Missing file at " + string.Join(" or ", paths));
        }
    }
}
