using System;
using System.Text.RegularExpressions;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>
    /// Minimal parsing of Unity Device Simulator <c>.device</c> JSON to map <c>systemInfo.deviceModel</c> to <c>friendlyName</c>.
    /// </summary>
    public static class MobileHandheldDeviceDefinitionParser
    {
        private static readonly Regex s_friendlyNameRegex = new Regex(
            "\"friendlyName\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns true when <paramref name="json"/> is a device definition whose <c>systemInfo.deviceModel</c>
        /// equals <paramref name="targetDeviceModel"/>; outputs the top-level <c>friendlyName</c>.
        /// </summary>
        public static bool TryGetFriendlyNameFromDeviceJson(string json, string targetDeviceModel, out string friendlyName)
        {
            friendlyName = null;
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(targetDeviceModel))
                return false;

            var modelInFile = TryExtractSystemInfoDeviceModel(json);
            if (!string.Equals(modelInFile, targetDeviceModel, StringComparison.Ordinal))
                return false;

            var m = s_friendlyNameRegex.Match(json);
            if (!m.Success)
                return false;

            friendlyName = m.Groups[1].Value;
            return !string.IsNullOrEmpty(friendlyName);
        }

        private static string TryExtractSystemInfoDeviceModel(string json)
        {
            int i = json.IndexOf("\"systemInfo\"", StringComparison.Ordinal);
            if (i < 0)
                return null;

            var tail = json.Substring(i);
            var mm = Regex.Match(tail, "\"deviceModel\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.CultureInvariant);
            return mm.Success ? mm.Groups[1].Value : null;
        }
    }
}
