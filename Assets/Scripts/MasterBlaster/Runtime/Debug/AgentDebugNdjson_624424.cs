using System;
using System.IO;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Debug
{
    /// <summary>NDJSON logger for Cursor debug session 624424 → debug-624424.log at project root.</summary>
    public static class AgentDebugNdjson_624424
    {
        const string LogFileName = "debug-624424.log";
        const string SessionId = "624424";

        public static void Log(string hypothesisId, string location, string message, string dataJsonObject, string runId = "pre")
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
                // ignore
            }
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
