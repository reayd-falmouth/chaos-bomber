using System;
using System.Collections;
using HybridGame.MasterBlaster.Scripts.Camera;
using HybridGame.MasterBlaster.Scripts.Debug;
using HybridGame.MasterBlaster.Scripts.Player;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts
{
    /// <summary>
    /// Manages switching between Bomberman (top-down), FPS, and angled arena view in a single scene.
    /// Single-player implementation — Phase 6 will add NetworkVariable for online sync.
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        /// <summary>Append-only enum values for serialized data compatibility.</summary>
        public enum GameMode
        {
            Bomberman,
            FPS,
            ArenaPerspective
        }

        /// <summary>Top-down or angled arena: grid movement and bombs; not first-person.</summary>
        public static bool IsGridPresentationMode(GameMode mode) =>
            mode == GameMode.Bomberman || mode == GameMode.ArenaPerspective;

        /// <summary>
        /// <see cref="BombController3D"/> runs in grid presentation modes and in FPS (bombs use Input System PlaceBomb).
        /// </summary>
        public static bool ShouldEnableBombController(GameMode mode) =>
            IsGridPresentationMode(mode) || mode == GameMode.FPS;

        public static GameModeManager Instance { get; private set; }
        public GameMode CurrentMode { get; private set; } = GameMode.Bomberman;

        public static event Action<GameMode> OnModeChanged;

        [Tooltip("Press this key to toggle mode (dev/testing only)")]
        public KeyCode devToggleKey = KeyCode.F2;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ApplyMode(CurrentMode);
            // Second apply next frame: dual-mode players, runtime HumanPlayerInput, and bomb/camera state may not exist yet on first ApplyMode (script order / EnablePlayer).
            StartCoroutine(ReapplyCurrentModeEndOfFrame());
        }

        private IEnumerator ReapplyCurrentModeEndOfFrame()
        {
            yield return null;
            ApplyMode(CurrentMode);
        }

        private void Update()
        {
            if (Input.GetKeyDown(devToggleKey))
                SwitchMode(GameModeCycle.GetNext(CurrentMode));
        }

        public void SwitchMode(GameMode newMode)
        {
            if (newMode == CurrentMode) return;
            CurrentMode = newMode;
            ApplyMode(newMode);
        }

        private void ApplyMode(GameMode mode)
        {
            // 1. Notify all dual-mode controllers (include inactive — player root may start disabled)
            foreach (var ctrl in FindObjectsByType<PlayerDualModeController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                ctrl.OnModeChanged(mode);

            // 2. Camera
            var camMgr = FindFirstObjectByType<HybridCameraManager>();
            if (camMgr != null) camMgr.SetMode(mode);

            // 3. Cursor
            if (mode == GameMode.FPS)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 4. Broadcast for any other listeners (HUD, audio, BillboardSprite, etc.). After HybridCameraManager.SetMode so Camera.main matches the active mode.
            OnModeChanged?.Invoke(mode);

            // #region agent log
            AgentDebugNdjson.Log("C", "GameModeManager.ApplyMode", "applied",
                "{\"mode\":\"" + mode + "\"}");
            // #endregion
        }
    }
}
