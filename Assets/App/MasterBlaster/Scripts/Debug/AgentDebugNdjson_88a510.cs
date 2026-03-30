using System;
using System.IO;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Debug
{
    /// <summary>
    /// NDJSON logger for the current debug session.
    /// Writes to debug-88a510.log at the project root (same strategy as AgentDebugNdjson).
    /// </summary>
    public static class AgentDebugNdjson_88a510
    {
        const string LogFileName = "debug-88a510.log";
        const string SessionId = "88a510";

        public static void Log(string hypothesisId, string location, string message, string dataJsonObject, string runId)
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "..", LogFileName);
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var line =
                    "{\"sessionId\":\"" + SessionId +
                    "\",\"runId\":\"" + Escape(runId) +
                    "\",\"hypothesisId\":\"" + Escape(hypothesisId) +
                    "\",\"location\":\"" + Escape(location) +
                    "\",\"message\":\"" + Escape(message) +
                    "\",\"data\":" + (string.IsNullOrEmpty(dataJsonObject) ? "{}" : dataJsonObject) +
                    ",\"timestamp\":" + ts +
                    "}\n";

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

