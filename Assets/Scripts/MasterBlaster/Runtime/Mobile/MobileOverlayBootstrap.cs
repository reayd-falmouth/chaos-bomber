using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Builds and controls the mobile gameplay overlay at runtime.
    /// Optionally uses a scene-authored hierarchy under this GameObject so D-pad / bomb layout can be edited in the Inspector.
    /// </summary>
    public class MobileOverlayBootstrap : MonoBehaviour
    {
        private static MobileOverlayBootstrap _instance;

        private static bool s_previewSimulateHandheld;
        private static bool s_previewIgnoreFlowState;

        [Header("Scene hierarchy (optional)")]
        [Tooltip("Assign after using the Inspector Generate button or authoring by hand. If any field is null, runtime falls back to code-built UI.")]
        [SerializeField]
        private Canvas _authoringCanvas;

        [SerializeField]
        private RectTransform _authoringOverlayRoot;

        [SerializeField]
        private RectTransform _authoringSafeArea;

        [Header("Diagnostics")]
        [SerializeField]
        [Tooltip("When enabled, logs whenever normalized safe-area anchors change (rotation, inset updates). Prefix [MasterBlaster][MobileOverlay][SafeArea].")]
        private bool logSafeAreaDiagnostics;

        private GameObject _root;
        private RectTransform _safeArea;

        /// <summary>Last anchors applied from <see cref="Screen.safeArea"/>; used to skip redundant layout writes.</summary>
        private Vector2 _appliedSafeAnchorMin = new Vector2(float.NaN, float.NaN);

        private Vector2 _appliedSafeAnchorMax = new Vector2(float.NaN, float.NaN);

        public static void EnsurePresent()
        {
            if (_instance != null)
                return;

            var found = FindObjectsByType<MobileOverlayBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null && found.Length > 0)
            {
                _instance = found[0];
                DontDestroyOnLoad(_instance.gameObject);
                return;
            }

            var go = new GameObject("MobileOverlayBootstrap");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MobileOverlayBootstrap>();
        }

        /// <summary>
        /// Overlay-only preview flags (Editor layout); does not change <see cref="FlowScreenAccessibilityTextScale.IsHandheldMobile"/>.
        /// </summary>
        public static void SetPreviewOverlayState(bool simulateHandheld, bool ignoreFlowStateForVisibility)
        {
            s_previewSimulateHandheld = simulateHandheld;
            s_previewIgnoreFlowState = ignoreFlowStateForVisibility;
        }

        /// <summary>
        /// When true, flow menus merge <see cref="MobileOverlayState"/> (Android/iOS or editor preview overlay).
        /// </summary>
        public static bool ShouldMergeOverlayIntoUiInput() =>
            FlowScreenAccessibilityTextScale.IsHandheldMobile() || s_previewSimulateHandheld;

        /// <summary>
        /// Ensures overlay exists on handheld or editor preview before the arena (e.g. Title) so touch controls work without visiting Game first.
        /// </summary>
        public static void EnsurePresentIfHandheld()
        {
            if (!FlowScreenAccessibilityTextScale.IsHandheldMobile() && !s_previewSimulateHandheld)
                return;
            EnsurePresent();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                UnityEngine.Debug.LogWarning(
                    "[MasterBlaster][MobileOverlay] Duplicate MobileOverlayBootstrap destroyed on " + gameObject.name + ".");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && UseAuthoringHierarchy())
                WarnIfMissingControlRoots();
        }

        /// <summary>
        /// Editor / tooling: builds default canvas, safe area, D-pad, and bomb under this transform and assigns authoring fields.
        /// </summary>
        public void PopulateDefaultAuthoringHierarchy()
        {
            if (Application.isPlaying)
                return;

            while (transform.childCount > 0)
                Object.DestroyImmediate(transform.GetChild(0).gameObject);

            var canvasGo = new GameObject("MobileOverlayCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _authoringCanvas = canvas;

            var rootGo = new GameObject("MobileOverlayRoot");
            rootGo.transform.SetParent(canvasGo.transform, false);
            var rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            _authoringOverlayRoot = rootRect;

            var safeGo = new GameObject("SafeArea");
            safeGo.transform.SetParent(rootRect, false);
            _authoringSafeArea = safeGo.AddComponent<RectTransform>();
            ApplySafeAreaToRect(_authoringSafeArea);

            BuildControls(_authoringSafeArea);

            WarnIfMissingControlRoots();
            UnityEngine.Debug.Log(
                "[MasterBlaster][MobileOverlay] Populated default authoring hierarchy under " + gameObject.name + ".");
        }

        private void WarnIfMissingControlRoots()
        {
            if (_authoringSafeArea == null)
                return;
            if (_authoringSafeArea.Find("DPadRoot") == null || _authoringSafeArea.Find("BombRoot") == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[MasterBlaster][MobileOverlay] SafeArea should contain DPadRoot and BombRoot for touch controls.");
            }
        }
#endif

        private void Update()
        {
            if (!ShouldUseMobileOverlay())
            {
                SetOverlayActive(false);
                MobileOverlayState.ResetAll();
                return;
            }

            EnsureOverlayBuilt();
            UpdateSafeArea();
            bool showControls = ShouldShowMobileControlsForCurrentFlow();
            SetOverlayActive(showControls);
            if (!showControls)
                MobileOverlayState.ResetAll();
        }

        private bool ShouldUseMobileOverlay() =>
            FlowScreenAccessibilityTextScale.IsHandheldMobile() || s_previewSimulateHandheld;

        /// <summary>
        /// Show D-pad on handheld for every flow state except Quote and Prologue (unless preview ignores flow).
        /// </summary>
        private static bool ShouldShowMobileControlsForCurrentFlow()
        {
            var flow = SceneFlowManager.I;
            FlowState? state = flow != null ? flow.CurrentState : (FlowState?)null;
            return MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(
                s_previewSimulateHandheld,
                s_previewIgnoreFlowState,
                state);
        }

        private void SetOverlayActive(bool active)
        {
            if (_root != null && _root.activeSelf != active)
                _root.SetActive(active);
        }

        private bool UseAuthoringHierarchy() =>
            _authoringCanvas != null
            && _authoringOverlayRoot != null
            && _authoringSafeArea != null;

        private void EnsureOverlayBuilt()
        {
            if (_root != null)
                return;

            if (UseAuthoringHierarchy())
            {
                _root = _authoringOverlayRoot.gameObject;
                _safeArea = _authoringSafeArea;
                ApplySafeAreaToRect(_safeArea);
                EnsureEventSystemExists();
                return;
            }

            var canvasGo = new GameObject("MobileOverlayCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;

            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            EnsureEventSystemExists();

            _root = new GameObject("MobileOverlayRoot");
            _root.transform.SetParent(canvasGo.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            _safeArea = new GameObject("SafeArea").AddComponent<RectTransform>();
            _safeArea.SetParent(rootRect, false);
            ApplySafeAreaToRect(_safeArea);

            BuildControls(_safeArea);
        }

        private static void EnsureEventSystemExists()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
            UnityEngine.Debug.Log(
                "[MasterBlaster][MobileOverlay] Created fallback EventSystem with InputSystemUIInputModule for on-screen controls.");
        }

        private static void BuildControls(RectTransform parent)
        {
            var dpadRoot = new GameObject("DPadRoot").AddComponent<RectTransform>();
            dpadRoot.SetParent(parent, false);
            dpadRoot.anchorMin = new Vector2(0.06f, 0.06f);
            dpadRoot.anchorMax = new Vector2(0.29f, 0.29f);
            dpadRoot.offsetMin = Vector2.zero;
            dpadRoot.offsetMax = Vector2.zero;

            CreateControlButton(dpadRoot, "Up", MobileHoldButton.MobileControl.Up, new Vector2(0.33f, 0.66f), new Vector2(0.66f, 1f));
            CreateControlButton(dpadRoot, "Down", MobileHoldButton.MobileControl.Down, new Vector2(0.33f, 0f), new Vector2(0.66f, 0.33f));
            CreateControlButton(dpadRoot, "Left", MobileHoldButton.MobileControl.Left, new Vector2(0f, 0.33f), new Vector2(0.33f, 0.66f));
            CreateControlButton(dpadRoot, "Right", MobileHoldButton.MobileControl.Right, new Vector2(0.66f, 0.33f), new Vector2(1f, 0.66f));

            var bombRoot = new GameObject("BombRoot").AddComponent<RectTransform>();
            bombRoot.SetParent(parent, false);
            bombRoot.anchorMin = new Vector2(0.74f, 0.08f);
            bombRoot.anchorMax = new Vector2(0.94f, 0.28f);
            bombRoot.offsetMin = Vector2.zero;
            bombRoot.offsetMax = Vector2.zero;
            CreateControlButton(bombRoot, "Bomb", MobileHoldButton.MobileControl.Bomb, Vector2.zero, Vector2.one);
        }

        private static void CreateControlButton(
            RectTransform parent,
            string label,
            MobileHoldButton.MobileControl control,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(label, typeof(Image), typeof(MobileHoldButton));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.18f);

            var button = go.GetComponent<MobileHoldButton>();
            button.Control = control;
        }

        /// <summary>
        /// Keeps the overlay control roots inside the device safe area (notch, home indicator).
        /// Runs every frame while the overlay is active so rotation and insets stay correct.
        /// </summary>
        private void UpdateSafeArea()
        {
            if (_safeArea == null)
                return;
            ApplySafeAreaToRect(_safeArea);
        }

        /// <summary>
        /// Maps <see cref="Screen.safeArea"/> to normalized anchor min/max on <paramref name="target"/>.
        /// Skips writes when values are unchanged to avoid redundant layout and Inspector churn.
        /// </summary>
        private void ApplySafeAreaToRect(RectTransform target)
        {
            if (target == null)
                return;

            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0)
                return;

            var safeArea = Screen.safeArea;
            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;

            min.x /= w;
            min.y /= h;
            max.x /= w;
            max.y /= h;

            if (VectorsApproximately(min, _appliedSafeAnchorMin) && VectorsApproximately(max, _appliedSafeAnchorMax))
                return;

            if (logSafeAreaDiagnostics)
            {
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileOverlay][SafeArea] Applying inset from Screen.safeArea. " +
                    $"screenPixels={w}x{h} safeAreaRect=({safeArea.x:F0},{safeArea.y:F0},{safeArea.width:F0},{safeArea.height:F0}) " +
                    $"normalizedAnchorMin=({min.x:F4},{min.y:F4}) normalizedAnchorMax=({max.x:F4},{max.y:F4}). " +
                    "Anchors are fractions of full screen; offsets on SafeArea are zero.");
            }

            _appliedSafeAnchorMin = min;
            _appliedSafeAnchorMax = max;

            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static bool VectorsApproximately(Vector2 a, Vector2 b) =>
            Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }
}
