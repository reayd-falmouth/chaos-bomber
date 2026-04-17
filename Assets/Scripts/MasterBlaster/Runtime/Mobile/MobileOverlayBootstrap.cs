using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Builds and controls the mobile gameplay overlay at runtime.
    /// </summary>
    public class MobileOverlayBootstrap : MonoBehaviour
    {
        private static MobileOverlayBootstrap _instance;

        private GameObject _root;
        private RectTransform _safeArea;

        public static void EnsurePresent()
        {
            if (_instance != null)
                return;

            var go = new GameObject("MobileOverlayBootstrap");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MobileOverlayBootstrap>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

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
            SetOverlayActive(IsGameplayFlowActive());
            if (!IsGameplayFlowActive())
                MobileOverlayState.ResetAll();
        }

        private bool ShouldUseMobileOverlay() => FlowScreenAccessibilityTextScale.IsHandheldMobile();

        private static bool IsGameplayFlowActive()
        {
            var flow = SceneFlowManager.I;
            if (flow == null)
                return true;
            return flow.CurrentState == FlowState.Game;
        }

        private void SetOverlayActive(bool active)
        {
            if (_root != null && _root.activeSelf != active)
                _root.SetActive(active);
        }

        private void EnsureOverlayBuilt()
        {
            if (_root != null)
                return;

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

            BuildWindowMask(_safeArea);
            BuildControls(_safeArea);
        }

        private static void EnsureEventSystemExists()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        private static void BuildWindowMask(RectTransform parent)
        {
            // Four bars create a centered "window" look where gameplay is visible.
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
