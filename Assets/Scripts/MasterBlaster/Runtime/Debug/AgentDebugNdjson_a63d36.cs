using System.IO;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Debug
{
    /// <summary>Session debug NDJSON (Cursor debug mode a63d36). Do not log secrets/PII.</summary>
    public static class AgentDebugNdjson_a63d36
    {
        const string LogFileName = "debug-a63d36.log";
        const string SessionId = "a63d36";

        public static void Log(string hypothesisId, string location, string message, string dataJsonObject)
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", LogFileName));
                long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var line = "{\"sessionId\":\"" + SessionId + "\",\"hypothesisId\":\"" + hypothesisId +
                           "\",\"location\":\"" + Escape(location) + "\",\"message\":\"" + Escape(message) +
                           "\",\"data\":" + dataJsonObject + ",\"timestamp\":" + ts + "}\n";
                File.AppendAllText(path, line);
            }
            catch
            {
                // ignore logging failures
            }
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
