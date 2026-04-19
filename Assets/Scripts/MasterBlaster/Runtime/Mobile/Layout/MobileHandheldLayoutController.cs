using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Mobile;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>
    /// Applies <see cref="MobileHandheldLayoutPresetEntry"/> rows when the screen size changes (handheld),
    /// coordinating letterbox, flow UI, and mobile overlay rects. When overlay safe-area is driven by presets,
    /// sets <see cref="MobileOverlayBootstrap"/> defer so automatic safe-area does not overwrite.
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

        [SerializeField]
        private MobileHandheldPresetMatchMode matchMode = MobileHandheldPresetMatchMode.NearestAspectRatio;

        [Header("Targets (assign scene refs)")]
        [SerializeField]
        private AmigaLetterboxCamera letterboxCamera;

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
            };

            if (letterboxCamera != null)
            {
                e.applyLetterbox = true;
                e.letterboxDesignWidth = letterboxCamera.GetDesignWidth();
                e.letterboxDesignHeight = letterboxCamera.GetDesignHeight();
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

            return e;
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

            if (e.applyLetterbox && letterboxCamera != null && e.letterboxDesignWidth > 0 && e.letterboxDesignHeight > 0)
                letterboxCamera.SetDesignResolution(e.letterboxDesignWidth, e.letterboxDesignHeight);

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
                    LogPrefix + " Applied preset label=\"" + e.label + "\" mode=" + matchMode + " screen=" + w + "x" + h + ".");
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
