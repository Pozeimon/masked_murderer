using System;
using System.IO;
using UnityEngine;

namespace TheTear.Telemetry
{
    public class TelemetryRecorder : MonoBehaviour
    {
        public string fileName = "the_tear_telemetry.jsonl";
        private string filePath;

        private void Awake()
        {
            filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public void RecordEvent(string eventType, string detail)
        {
            string ts = DateTime.UtcNow.ToString("o");
            string line = "{\"ts\":\"" + Escape(ts) + "\",\"event\":\"" + Escape(eventType) + "\",\"detail\":\"" + Escape(detail) + "\"}";
            try
            {
                File.AppendAllText(filePath, line + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Telemetry write failed: " + ex.Message);
            }
        }

        private string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
