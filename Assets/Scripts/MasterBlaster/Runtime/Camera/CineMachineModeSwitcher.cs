using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using HybridGame.MasterBlaster.Scripts.Core;
using Unity.FPS.Gameplay; // To check for Player components

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    public class CinemachineModeSwitcher : MonoBehaviour
    {
        public static CinemachineModeSwitcher Instance;

        public int activePriority = 20;
        public int inactivePriority = 10;

        public enum InitialBombermanCameraPolicy
        {
            FirstNonPlayerCamera,
            FirstRegisteredCamera,
            SpecificCamera
        }

        [Header("Startup")]
        [Tooltip("On startup, force a deterministic initial camera selection (prevents defaults/registration order selecting the wrong camera).")]
        [SerializeField] private bool forceInitialSelectionOnStart = true;

        [Tooltip("Which camera should be active at startup when in Bomberman mode.")]
        [SerializeField] private InitialBombermanCameraPolicy initialBombermanCameraPolicy = InitialBombermanCameraPolicy.FirstNonPlayerCamera;

        [Tooltip("Used only when InitialBombermanCameraPolicy is SpecificCamera.")]
        [SerializeField] private CinemachineCamera specificInitialCamera;

        [Tooltip("If enabled, on startup (in Bomberman mode) activates the next camera in the registered list instead of the first.")]
        [SerializeField] private bool startOnNextRegisteredCamera;

        public List<CinemachineCamera> registeredCameras = new List<CinemachineCamera>();

        private int m_SelectedIndex = -1;
        private GameModeManager.GameMode m_LastMode = GameModeManager.GameMode.Bomberman;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            GameModeManager.OnModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            GameModeManager.OnModeChanged -= HandleModeChanged;
        }

        private void Start()
        {
            StartCoroutine(StartupResyncNextFrame());
        }

        public void Register(CinemachineCamera cam)
        {
            if (!registeredCameras.Contains(cam))
            {
                registeredCameras.Add(cam);
                // Initial sync
                var mode = GameModeManager.Instance != null ? GameModeManager.Instance.CurrentMode : GameModeManager.GameMode.Bomberman;
                m_LastMode = mode;
                SyncCameraPriority(cam, mode);
            }
        }

        public void Unregister(CinemachineCamera cam) => registeredCameras.Remove(cam);

        public void UpdateAllCameras(GameModeManager.GameMode mode)
        {
            foreach (var cam in registeredCameras)
            {
                SyncCameraPriority(cam, mode);
            }
        }

        public void SelectNextCamera()
        {
            if (registeredCameras.Count == 0) return;

            if (m_SelectedIndex < 0)
                m_SelectedIndex = 0;

            m_SelectedIndex = (m_SelectedIndex + 1) % registeredCameras.Count;
            ApplySpecificCameraSelection(registeredCameras[m_SelectedIndex]);
        }

        private IEnumerator StartupResyncNextFrame()
        {
            DiscoverSceneCameras();
            yield return null;

            DiscoverSceneCameras();
            var mode = GameModeManager.Instance != null ? GameModeManager.Instance.CurrentMode : GameModeManager.GameMode.Bomberman;
            m_LastMode = mode;

            if (forceInitialSelectionOnStart && mode == GameModeManager.GameMode.Bomberman)
            {
                var initial = ResolveInitialBombermanCamera();
                if (initial != null)
                {
                    // #region agent log
                    AgentCamDbg("A", "StartupResyncNextFrame", "before_ApplySpecific",
                        "{\"mode\":\"" + mode + "\",\"initial\":\"" + SafeName(initial) + "\",\"policy\":\"" + initialBombermanCameraPolicy + "\"}");
                    // #endregion
                    ApplySpecificCameraSelection(initial);
                    if (startOnNextRegisteredCamera)
                        SelectNextCamera();
                    // #region agent log
                    AgentCamDbg("A", "StartupResyncNextFrame", "after_ApplySpecific", "{\"priorities\":" + BuildPriorityJson() + "}");
                    // #endregion
                    yield break;
                }
            }

            UpdateAllCameras(mode);
            // #region agent log
            AgentCamDbg("B", "StartupResyncNextFrame", "fallback_UpdateAll", "{\"mode\":\"" + mode + "\",\"priorities\":" + BuildPriorityJson() + "}");
            // #endregion
        }

        private void HandleModeChanged(GameModeManager.GameMode mode)
        {
            m_LastMode = mode;
            DiscoverSceneCameras();
            UpdateAllCameras(mode);
            // #region agent log
            AgentCamDbg("C", "HandleModeChanged", "after_UpdateAll",
                "{\"mode\":\"" + mode + "\",\"priorities\":" + BuildPriorityJson() + "}");
            // #endregion
        }

        private void DiscoverSceneCameras()
        {
            var cams = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam == null) continue;
                if (!registeredCameras.Contains(cam))
                    registeredCameras.Add(cam);
            }
        }

        private CinemachineCamera ResolveInitialBombermanCamera()
        {
            if (registeredCameras.Count == 0) return null;

            switch (initialBombermanCameraPolicy)
            {
                case InitialBombermanCameraPolicy.FirstRegisteredCamera:
                    return registeredCameras[0];
                case InitialBombermanCameraPolicy.SpecificCamera:
                    return specificInitialCamera != null && registeredCameras.Contains(specificInitialCamera)
                        ? specificInitialCamera
                        : null;
                case InitialBombermanCameraPolicy.FirstNonPlayerCamera:
                default:
                {
                    for (int i = 0; i < registeredCameras.Count; i++)
                    {
                        var cam = registeredCameras[i];
                        if (cam == null) continue;
                        if (!IsPlayerCamera(cam))
                            return cam;
                    }
                    return null;
                }
            }
        }

        private void ApplySpecificCameraSelection(CinemachineCamera selected)
        {
            if (selected == null) return;

            for (int i = 0; i < registeredCameras.Count; i++)
            {
                var cam = registeredCameras[i];
                if (cam == null) continue;
                cam.Priority = cam == selected ? activePriority : inactivePriority;
            }
        }

        private static bool IsPlayerCamera(CinemachineCamera cam)
        {
            return cam != null && cam.GetComponentInParent<PlayerCharacterController>() != null;
        }

        private void SyncCameraPriority(CinemachineCamera cam, GameModeManager.GameMode mode)
        {
            bool isPlayerCamera = IsPlayerCamera(cam);
            bool hasArenaMarker = HasAnyArenaPerspectiveCamera();
            bool thisMarked = cam.GetComponent<ArenaPerspectiveCinemachineCamera>() != null;
            cam.Priority = CinemachineModePriorityLogic.Compute(
                mode,
                isPlayerCamera,
                hasArenaMarker,
                thisMarked,
                activePriority,
                inactivePriority);
        }

        private bool HasAnyArenaPerspectiveCamera()
        {
            for (int i = 0; i < registeredCameras.Count; i++)
            {
                var c = registeredCameras[i];
                if (c != null && c.GetComponent<ArenaPerspectiveCinemachineCamera>() != null)
                    return true;
            }

            return false;
        }

        // #region agent log
        private static string SafeName(UnityEngine.Object o) => o != null ? o.name.Replace("\"", "'") : "null";

        private string BuildPriorityJson()
        {
            var parts = new List<string>(registeredCameras.Count);
            for (int i = 0; i < registeredCameras.Count; i++)
            {
                var c = registeredCameras[i];
                if (c == null) continue;
                int p = c.Priority;
                bool marked = c.GetComponent<ArenaPerspectiveCinemachineCamera>() != null;
                parts.Add("{\"n\":\"" + SafeName(c.gameObject) + "\",\"p\":" + p + ",\"player\":" + (IsPlayerCamera(c) ? "true" : "false") + ",\"apMark\":" + (marked ? "true" : "false") + "}");
            }

            return "[" + string.Join(",", parts) + "]";
        }

        private static void AgentCamDbg(string hypothesisId, string location, string message, string dataJson)
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "..", "debug-6efe26.log");
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var line = "{\"sessionId\":\"6efe26\",\"hypothesisId\":\"" + hypothesisId + "\",\"location\":\"" + location + "\",\"message\":\"" + message + "\",\"data\":" + dataJson + ",\"timestamp\":" + ts + "}\n";
                File.AppendAllText(path, line);
            }
            catch
            {
                // ignored
            }
        }
        // #endregion
    }
}