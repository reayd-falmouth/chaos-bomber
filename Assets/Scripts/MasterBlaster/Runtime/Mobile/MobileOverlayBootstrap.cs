using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Builds and controls the mobile gameplay overlay at runtime.
    /// Optionally uses a scene-authored hierarchy under this GameObject so D-pad / letterbox layout can be edited in the Inspector.
    /// </summary>
    public class MobileOverlayBootstrap : MonoBehaviour
    {
        private static MobileOverlayBootstrap _instance;

        private static bool s_previewSimulateHandheld;
        private static bool s_previewIgnoreFlowState;
        private static MobileOverlayPreviewLetterboxMode s_previewLetterboxMode = MobileOverlayPreviewLetterboxMode.FollowGameFlow;

        [Header("Scene hierarchy (optional)")]
        [Tooltip("Assign after using the Inspector Generate button or authoring by hand. If any field is null, runtime falls back to code-built UI.")]
        [SerializeField]
        private Canvas _authoringCanvas;

        [SerializeField]
        private RectTransform _authoringOverlayRoot;

        [SerializeField]
        private RectTransform _authoringSafeArea;

        [SerializeField]
        private RectTransform _authoringLetterboxRoot;

        private GameObject _root;
        private RectTransform _safeArea;
        private GameObject _letterboxRoot;

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
        public static void SetPreviewOverlayState(
            bool simulateHandheld,
            bool ignoreFlowStateForVisibility,
            MobileOverlayPreviewLetterboxMode letterboxMode)
        {
            s_previewSimulateHandheld = simulateHandheld;
            s_previewIgnoreFlowState = ignoreFlowStateForVisibility;
            s_previewLetterboxMode = letterboxMode;
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
        /// Editor / tooling: builds default canvas, safe area, letterbox, D-pad, and bomb under this transform and assigns authoring fields.
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

            var letterboxGo = new GameObject("GameplayLetterbox");
            letterboxGo.transform.SetParent(_authoringSafeArea, false);
            var letterboxRect = letterboxGo.AddComponent<RectTransform>();
            letterboxRect.anchorMin = Vector2.zero;
            letterboxRect.anchorMax = Vector2.one;
            letterboxRect.offsetMin = Vector2.zero;
            letterboxRect.offsetMax = Vector2.zero;
            _authoringLetterboxRoot = letterboxRect;

            BuildWindowMask(letterboxRect);
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
            else
                UpdateLetterboxVisibility();
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

        private void UpdateLetterboxVisibility()
        {
            if (_letterboxRoot == null)
                return;
            bool flowIsGame = SceneFlowManager.I != null && SceneFlowManager.I.CurrentState == FlowState.Game;
            bool active = MobileOverlayPreviewPolicy.ShouldLetterboxBeActive(
                s_previewSimulateHandheld,
                s_previewLetterboxMode,
                flowIsGame);
            if (_letterboxRoot.activeSelf != active)
                _letterboxRoot.SetActive(active);
        }

        private void SetOverlayActive(bool active)
        {
            if (_root != null && _root.activeSelf != active)
                _root.SetActive(active);
        }

        private bool UseAuthoringHierarchy() =>
            _authoringCanvas != null
            && _authoringOverlayRoot != null
            && _authoringSafeArea != null
            && _authoringLetterboxRoot != null;

        private void EnsureOverlayBuilt()
        {
            if (_root != null)
                return;

            if (UseAuthoringHierarchy())
            {
                _root = _authoringOverlayRoot.gameObject;
                _safeArea = _authoringSafeArea;
                _letterboxRoot = _authoringLetterboxRoot.gameObject;
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

            _letterboxRoot = new GameObject("GameplayLetterbox");
            _letterboxRoot.transform.SetParent(_safeArea, false);
            var letterboxRect = _letterboxRoot.AddComponent<RectTransform>();
            letterboxRect.anchorMin = Vector2.zero;
            letterboxRect.anchorMax = Vector2.one;
            letterboxRect.offsetMin = Vector2.zero;
            letterboxRect.offsetMax = Vector2.zero;

            BuildWindowMask(letterboxRect);
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

        private static void BuildWindowMask(RectTransform parent)
        {
            CreateBar(parent, "TopBar", new Vector2(0f, 0.75f), new Vector2(1f, 1f));
            CreateBar(parent, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0.25f));
            CreateBar(parent, "LeftBar", new Vector2(0f, 0.25f), new Vector2(0.2f, 0.75f));
            CreateBar(parent, "RightBar", new Vector2(0.8f, 0.25f), new Vector2(1f, 0.75f));
        }

        private static void CreateBar(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.65f);
            img.raycastTarget = false;
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

        private void UpdateSafeArea()
        {
            if (_safeArea == null)
                return;

            var safeArea = Screen.safeArea;
            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _safeArea.anchorMin = min;
            _safeArea.anchorMax = max;
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
        }
    }
}
