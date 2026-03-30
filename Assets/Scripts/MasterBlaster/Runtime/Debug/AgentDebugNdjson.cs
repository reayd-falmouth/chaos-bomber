using System.IO;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Debug
{
    /// <summary>Session debug NDJSON logger (Cursor debug mode). Do not log secrets/PII.</summary>
    public static class AgentDebugNdjson
    {
        const string LogFileName = "debug-8869c5.log";
        const string SessionId = "8869c5";

        public static void Log(string hypothesisId, string location, string message, string dataJsonObject)
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "..", LogFileName);
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
