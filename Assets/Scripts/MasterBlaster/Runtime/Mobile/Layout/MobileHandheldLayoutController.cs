using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Camera;
using HybridGame.MasterBlaster.Scripts.Mobile;
using Unity.Cinemachine;
using Screen = UnityEngine.Device.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>
    /// Applies <see cref="MobileHandheldLayoutPresetEntry"/> rows when the screen size changes (handheld),
    /// coordinating Cinemachine brain output + registered vcams, flow UI, and mobile overlay rects.
    /// When overlay safe-area is driven by presets, sets <see cref="MobileOverlayBootstrap"/> defer so automatic safe-area does not overwrite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileHandheldLayoutController : MonoBehaviour
    {
        private const string LogPrefix = "[MasterBlaster][MobileHandheldLayout]";

        [Header("Preset source")]
        [SerializeField]
        private MobileHandheldLayoutPresetLibrary presetLibrary;

        [Tooltip("Used when presetLibrary is null (quick scene-local tuning).")]
        [SerializeField]
        private MobileHandheldLayoutPresetEntry[] inlinePresets = System.Array.Empty<MobileHandheldLayoutPresetEntry>();

        [Header("Last capture (Inspector)")]
        [Tooltip("Filled by the Inspector Capture button; saved with the scene. Does not auto-apply at runtime unless also listed under Preset Library or Inline Presets.")]
        [SerializeField]
        private MobileHandheldLayoutPresetEntry lastCapturedPreset;

        [SerializeField]
        private MobileHandheldPresetMatchMode matchMode = MobileHandheldPresetMatchMode.NearestAspectRatio;

        [Header("Gameplay cameras (assign scene refs)")]
        [Tooltip("Brain output: Unity Camera on the same GameObject (viewport rect, ortho/FoV).")]
        [SerializeField]
        private CinemachineBrain cinemachineBrain;

        [Tooltip("Optional; captures Priority + Lens for each registered CinemachineCamera.")]
        [SerializeField]
        private CinemachineModeSwitcher cinemachineModeSwitcher;

        [Header("UI / overlay targets")]
        [SerializeField]
        private RectTransform uiCanvasRoot;

        [SerializeField]
        private CanvasScaler uiCanvasScaler;

        [SerializeField]
        private RectTransform mobileOverlayCanvasRoot;

        [SerializeField]
        private CanvasScaler mobileOverlayCanvasScaler;

        [SerializeField]
        private RectTransform mobileOverlayRoot;

        [SerializeField]
        private RectTransform mobileOverlaySafeArea;

        [SerializeField]
        [Tooltip("Optional; when assigned, capture/apply RectTransform layout for this object (e.g. Background Image under MobileOverlayRoot).")]
        private RectTransform mobileOverlayBackground;

        [Tooltip(
            "Assign the scene MobileOverlayBootstrap when presets include overlay SafeArea — otherwise automatic Screen.safeArea layout overwrites your snapshot.")]
        [SerializeField]
        private MobileOverlayBootstrap mobileOverlayBootstrap;

        [Header("Arena grid (optional)")]
        [Tooltip("When assigned, calls RepublishGridOrigin after layout so ArenaGrid3D.CellSize tracks parent scale.")]
        [SerializeField]
        private HybridArenaGrid hybridArenaGrid;

        [Header("Diagnostics")]
        [SerializeField]
        private bool logWhenPresetApplied;

        [SerializeField]
        private bool logWhenSkipped;

        [SerializeField]
        [Tooltip("When enabled, CaptureCurrentLayoutToScratch logs to the Console. Prefix [MasterBlaster][MobileHandheldLayout].")]
        private bool logWhenCaptureStored;

        [SerializeField]
        [Tooltip(
            "When enabled, logs once whenever UnityEngine.Device.Screen width/height changes (Device Simulator / rotation). Prefix [MasterBlaster][MobileHandheldLayout].")]
        private bool logWhenScreenKeyChanges;

        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private bool _deferSafeAreaActive;

        private void OnDisable()
        {
            SetOverlayDeferSafeArea(false);
        }

        private void LateUpdate()
        {
            if (!ShouldRunOnThisPlatform())
                return;

            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0)
                return;

            if (_lastScreenWidth == w && _lastScreenHeight == h)
                return;

            if (logWhenScreenKeyChanges)
            {
                UnityEngine.Debug.Log(
                    LogPrefix + " Screen key "
                    + (_lastScreenWidth < 0
                        ? "set to "
                        : "changed from " + _lastScreenWidth + "x" + _lastScreenHeight + " to ")
                    + w
                    + "x"
                    + h
                    + " (UnityEngine.Device.Screen).");
            }

            _lastScreenWidth = w;
            _lastScreenHeight = h;
            TryApplyPresetForScreen(w, h);
        }

        /// <summary>Editor / runtime: apply immediately using current <see cref="Screen"/> size.</summary>
        public void ApplyNowForCurrentScreen()
        {
            int w = Screen.width;
            int h = Screen.height;
            TryApplyPresetForScreen(w, h);
            _lastScreenWidth = w;
            _lastScreenHeight = h;
        }

        /// <summary>
        /// Snapshots assigned scene refs into a new preset row (used by the custom Inspector and tooling).
        /// </summary>
        public MobileHandheldLayoutPresetEntry BuildCaptureFromSceneRefs(int screenW, int screenH, string label)
        {
            var e = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = screenW,
                screenHeight = screenH,
                label = label ?? string.Empty,
                simulatedDeviceModel = UnityEngine.Device.SystemInfo.deviceModel ?? string.Empty,
                applyGameplayCameras = true,
            };

            if (cinemachineBrain != null && cinemachineBrain.TryGetComponent<UnityEngine.Camera>(out var brainCam))
                e.cinemachineBrainOutputCamera = MobileHandheldUnityCameraSnapshot.Capture(brainCam);
            else
                e.cinemachineBrainOutputCamera = default;

            if (cinemachineModeSwitcher != null && cinemachineModeSwitcher.registeredCameras != null
                                                && cinemachineModeSwitcher.registeredCameras.Count > 0)
            {
                var regs = cinemachineModeSwitcher.registeredCameras;
                var arr = new MobileHandheldCinemachineVcamSnapshotEntry[regs.Count];
                for (int i = 0; i < regs.Count; i++)
                    arr[i] = MobileHandheldCinemachineVcamSnapshotEntry.Capture(regs[i]);
                e.cinemachineVcams = arr;
            }
            else
            {
                e.cinemachineVcams = System.Array.Empty<MobileHandheldCinemachineVcamSnapshotEntry>();
            }

            if (uiCanvasRoot != null)
                e.uiCanvasRootRect = MobileHandheldRectSnapshot.Capture(uiCanvasRoot);
            e.uiCanvasScaler = MobileHandheldCanvasScalerSnapshot.Capture(uiCanvasScaler);

            if (mobileOverlayCanvasRoot != null)
                e.overlayCanvasRect = MobileHandheldRectSnapshot.Capture(mobileOverlayCanvasRoot);
            e.overlayCanvasScaler = MobileHandheldCanvasScalerSnapshot.Capture(mobileOverlayCanvasScaler);
            if (mobileOverlayRoot != null)
                e.overlayRootRect = MobileHandheldRectSnapshot.Capture(mobileOverlayRoot);
            if (mobileOverlaySafeArea != null)
                e.overlaySafeAreaRect = MobileHandheldRectSnapshot.Capture(mobileOverlaySafeArea);
            if (mobileOverlayBackground != null)
                e.overlayBackgroundRect = MobileHandheldRectSnapshot.Capture(mobileOverlayBackground);

            return e;
        }

        /// <summary>
        /// Stores a snapshot on this component (<see cref="lastCapturedPreset"/>), editable in the Inspector and saved with the scene.
        /// </summary>
        public void CaptureCurrentLayoutToScratch(int screenW, int screenH, string label)
        {
            lastCapturedPreset = BuildCaptureFromSceneRefs(screenW, screenH, label);
            if (logWhenCaptureStored)
            {
                UnityEngine.Debug.Log(
                    LogPrefix + " Stored lastCapturedPreset label=\"" + (label ?? string.Empty) + "\" key=" + screenW + "x" + screenH + ".");
            }
        }

        /// <summary>Appends a fresh snapshot to <see cref="inlinePresets"/> (scene-local list).</summary>
        public void AppendCaptureToInlinePresets(int screenW, int screenH, string label)
        {
            var entry = BuildCaptureFromSceneRefs(screenW, screenH, label);
            int n = inlinePresets != null ? inlinePresets.Length : 0;
            var newArr = new MobileHandheldLayoutPresetEntry[n + 1];
            for (int i = 0; i < n; i++)
                newArr[i] = inlinePresets[i];
            newArr[n] = entry;
            inlinePresets = newArr;
            if (logWhenCaptureStored)
            {
                UnityEngine.Debug.Log(
                    LogPrefix + " Appended inline preset index=" + n + " label=\"" + (label ?? string.Empty) + "\" key=" + screenW + "x" + screenH + ".");
            }
        }

        private bool ShouldRunOnThisPlatform()
        {
            if (!isActiveAndEnabled)
                return false;
#if UNITY_EDITOR
            return true;
#else
            return FlowScreenAccessibilityTextScale.IsHandheldMobile();
#endif
        }

        private void TryApplyPresetForScreen(int w, int h)
        {
            var entries = GetEffectiveEntryList();
            if (entries == null || entries.Count == 0)
            {
                SetOverlayDeferSafeArea(false);
                if (logWhenSkipped)
                    UnityEngine.Debug.Log(LogPrefix + " No presets configured; skipping apply.");
                return;
            }

            MobileHandheldLayoutPresetEntry entry = null;
            switch (matchMode)
            {
                case MobileHandheldPresetMatchMode.ExactPixel:
                    if (!MobileHandheldLayoutPresetSelector.TrySelectExact(entries, w, h, out entry, out _))
                    {
                        if (logWhenSkipped)
                            UnityEngine.Debug.LogWarning(LogPrefix + " No exact preset for " + w + "x" + h + ".");
                        return;
                    }

                    break;
                case MobileHandheldPresetMatchMode.NearestAspectRatio:
                    if (!MobileHandheldLayoutPresetSelector.TrySelectNearestAspect(entries, w, h, out entry, out _))
                    {
                        if (logWhenSkipped)
                            UnityEngine.Debug.LogWarning(LogPrefix + " Nearest-aspect selection failed.");
                        return;
                    }

                    break;
                case MobileHandheldPresetMatchMode.InterpolateNearestAspects:
                    if (!MobileHandheldLayoutPresetSelector.TryBuildInterpolatedEntry(entries, w, h, out var built))
                    {
                        if (logWhenSkipped)
                            UnityEngine.Debug.LogWarning(LogPrefix + " Interpolation failed.");
                        return;
                    }

                    entry = built;
                    break;
            }

            if (entry == null)
                return;

            ApplyEntry(entry, w, h);
        }

        private List<MobileHandheldLayoutPresetEntry> GetEffectiveEntryList()
        {
            if (presetLibrary != null && presetLibrary.entries != null && presetLibrary.entries.Count > 0)
                return presetLibrary.entries;
            if (inlinePresets != null && inlinePresets.Length > 0)
            {
                var list = new List<MobileHandheldLayoutPresetEntry>();
                for (int i = 0; i < inlinePresets.Length; i++)
                {
                    if (inlinePresets[i] != null)
                        list.Add(inlinePresets[i]);
                }

                return list;
            }

            return null;
        }

        private void ApplyEntry(MobileHandheldLayoutPresetEntry e, int w, int h)
        {
            bool drivesOverlaySafeArea = e.applyMobileOverlay && mobileOverlaySafeArea != null;

            if (e.applyGameplayCameras)
                ApplyGameplaySnapshots(e);

            if (e.applyUiCanvas)
            {
                if (uiCanvasRoot != null)
                    MobileHandheldRectSnapshot.Apply(uiCanvasRoot, e.uiCanvasRootRect);
                MobileHandheldCanvasScalerSnapshot.Apply(uiCanvasScaler, e.uiCanvasScaler);
            }

            if (e.applyMobileOverlay)
            {
                SetOverlayDeferSafeArea(drivesOverlaySafeArea);
                if (mobileOverlayCanvasRoot != null)
                    MobileHandheldRectSnapshot.Apply(mobileOverlayCanvasRoot, e.overlayCanvasRect);
                MobileHandheldCanvasScalerSnapshot.Apply(mobileOverlayCanvasScaler, e.overlayCanvasScaler);
                if (mobileOverlayRoot != null)
                    MobileHandheldRectSnapshot.Apply(mobileOverlayRoot, e.overlayRootRect);
                if (mobileOverlaySafeArea != null)
                    MobileHandheldRectSnapshot.Apply(mobileOverlaySafeArea, e.overlaySafeAreaRect);
                if (mobileOverlayBackground != null)
                    MobileHandheldRectSnapshot.Apply(mobileOverlayBackground, e.overlayBackgroundRect);
            }
            else
            {
                SetOverlayDeferSafeArea(false);
            }

            if (hybridArenaGrid != null)
                hybridArenaGrid.RepublishGridOrigin();

            if (logWhenPresetApplied)
            {
                UnityEngine.Debug.Log(
                    LogPrefix
                    + " Applied preset label=\""
                    + e.label
                    + "\" mode="
                    + matchMode
                    + " screen="
                    + w
                    + "x"
                    + h
                    + " gameplayCameras="
                    + e.applyGameplayCameras
                    + " ui="
                    + e.applyUiCanvas
                    + " overlay="
                    + e.applyMobileOverlay
                    + ".");
            }
        }

        private void ApplyGameplaySnapshots(MobileHandheldLayoutPresetEntry e)
        {
            if (cinemachineBrain != null && cinemachineBrain.TryGetComponent<UnityEngine.Camera>(out var brainCam))
                MobileHandheldUnityCameraSnapshot.Apply(brainCam, e.cinemachineBrainOutputCamera);

            if (cinemachineModeSwitcher == null || e.cinemachineVcams == null)
                return;

            for (int i = 0; i < e.cinemachineVcams.Length; i++)
            {
                var snap = e.cinemachineVcams[i];
                if (snap == null || string.IsNullOrEmpty(snap.gameObjectName))
                    continue;
                for (int j = 0; j < cinemachineModeSwitcher.registeredCameras.Count; j++)
                {
                    var v = cinemachineModeSwitcher.registeredCameras[j];
                    if (v != null && v.gameObject.name == snap.gameObjectName)
                    {
                        MobileHandheldCinemachineVcamSnapshotEntry.Apply(v, snap);
                        break;
                    }
                }
            }
        }

        private void SetOverlayDeferSafeArea(bool defer)
        {
            if (_deferSafeAreaActive == defer)
                return;
            _deferSafeAreaActive = defer;
            if (mobileOverlayBootstrap != null)
                mobileOverlayBootstrap.SetDeferSafeAreaLayout(defer);
        }
    }
}
