using System.IO;
using HybridGame.MasterBlaster.Scripts.Mobile.Layout;
using UnityEditor;
using UnityEngine;

namespace HybridGame.MasterBlaster.Editor
{
    /// <summary>
    /// Resolves a human-readable device label for handheld layout capture using Device Simulator definitions.
    /// </summary>
    public static class MobileHandheldSimulatorDeviceLabel
    {
        /// <summary>
        /// Uses <see cref="UnityEngine.Device.SystemInfo.deviceModel"/> (simulated in Device Simulator) and installed
        /// <c>com.unity.device-simulator.devices</c> <c>.device</c> files to find <c>friendlyName</c>.
        /// Falls back to <see cref="UnityEngine.Device.SystemInfo.deviceModel"/> when no file matches.
        /// </summary>
        public static string ResolveFriendlyNameOrDeviceModel()
        {
            string model = UnityEngine.Device.SystemInfo.deviceModel;
            if (string.IsNullOrEmpty(model))
                return string.Empty;

            if (TryResolveFriendlyNameFromInstalledDeviceDefinitions(model, out var friendly) && !string.IsNullOrEmpty(friendly))
                return friendly;

            return model;
        }

        /// <summary>
        /// Scans the device-simulator.devices package for a <c>.device</c> file whose <c>systemInfo.deviceModel</c> matches.
        /// </summary>
        public static bool TryResolveFriendlyNameFromInstalledDeviceDefinitions(string deviceModel, out string friendlyName)
        {
            friendlyName = null;
            if (string.IsNullOrEmpty(deviceModel))
                return false;

            var devicesDir = TryGetDeviceDefinitionsDirectory();
            if (string.IsNullOrEmpty(devicesDir) || !Directory.Exists(devicesDir))
                return false;

            foreach (var path in Directory.GetFiles(devicesDir, "*.device", SearchOption.TopDirectoryOnly))
            {
                string json;
                try
                {
                    json = File.ReadAllText(path);
                }
                catch (IOException)
                {
                    continue;
                }

                if (MobileHandheldDeviceDefinitionParser.TryGetFriendlyNameFromDeviceJson(json, deviceModel, out var fn))
                {
                    friendlyName = fn;
                    return true;
                }
            }

            return false;
        }

        private static string TryGetDeviceDefinitionsDirectory()
        {
#if UNITY_2021_2_OR_NEWER
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var p in packages)
            {
                if (p.name == "com.unity.device-simulator.devices")
                {
                    var path = Path.Combine(p.resolvedPath, "Editor", "Devices");
                    if (Directory.Exists(path))
                        return path;
                }
            }
#endif
            return TryGetDeviceDefinitionsDirectoryFromPackageCache();
        }

        private static string TryGetDeviceDefinitionsDirectoryFromPackageCache()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            var cache = Path.Combine(projectRoot, "Library", "PackageCache");
            if (!Directory.Exists(cache))
                return null;

            try
            {
                foreach (var dir in Directory.GetDirectories(cache, "com.unity.device-simulator.devices@*"))
                {
                    var devices = Path.Combine(dir, "Editor", "Devices");
                    if (Directory.Exists(devices))
                        return devices;
                }
            }
            catch (IOException)
            {
                // ignore
            }

            return null;
        }
    }
}
