using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Prologue
{
    public class PrologueController : MonoBehaviour
    {
        [Header("Crawl")]
        [Tooltip("UI units per second (anchoredPosition.y increases upward).")]
        [Min(0f)] [SerializeField] private float scrollSpeedUnitsPerSecond = 30f;
        [HideInInspector] [SerializeField] private float crawlEndAnchoredY = 600f;

        [Header("Scene References")]
        [SerializeField] private GameObject prologuePanel;
        [SerializeField] private RectTransform prologueText;

        private CanvasGroup _prologueCanvasGroup;
        private bool _skipped;
        private Coroutine _routine;

        public static float StepScrollY(float currentY, float speedUnitsPerSecond, float deltaTime)
        {
            return currentY + speedUnitsPerSecond * deltaTime;
        }

        public static float ComputeStartDeltaY(float viewportYMin, float contentTopY, float padding)
        {
            return (viewportYMin - padding) - contentTopY;
        }

        public static bool IsCrawlFinished(float viewportYMax, float contentBottomY, float padding)
        {
            return contentBottomY >= (viewportYMax + padding);
        }

        public static float ComputeCrawlFadeAlpha(float viewportCenterY, float viewportYMax, float contentBottomY)
        {
            float t = Mathf.InverseLerp(viewportCenterY, viewportYMax, contentBottomY);
            return 1f - Mathf.Clamp01(t);
        }

        private RectTransform GetViewportRectTransform()
        {
            if (prologuePanel != null && prologuePanel.TryGetComponent<RectTransform>(out var panelRect))
                return panelRect;
            return prologueText != null ? (prologueText.parent as RectTransform) : null;
        }

        private void SnapPrologueTextStartOffscreen()
        {
            if (prologueText == null)
                return;

            var viewport = GetViewportRectTransform();
            if (viewport == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            Canvas.ForceUpdateCanvases();

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, prologueText);
            float deltaY = ComputeStartDeltaY(viewport.rect.yMin, bounds.max.y, 0f);

            var pos = prologueText.anchoredPosition;
            pos.y += deltaY;
            prologueText.anchoredPosition = pos;
        }

        void OnEnable()
        {
            _skipped = false;

            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(Run());
        }

        void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        void Update()
        {
            if (_skipped)
                return;

            if (AnySkipPressedThisFrame())
                SkipToTitle();
        }

        private IEnumerator Run()
        {
            // Prepare UI state
            if (prologuePanel != null)
            {
                prologuePanel.SetActive(true);
                _prologueCanvasGroup = prologuePanel.GetComponent<CanvasGroup>();
                if (_prologueCanvasGroup == null)
                    _prologueCanvasGroup = prologuePanel.AddComponent<CanvasGroup>();
                _prologueCanvasGroup.alpha = 1f;
            }

            SnapPrologueTextStartOffscreen();

            // Crawl
            var viewport = GetViewportRectTransform();
            while (true)
            {
                if (_skipped) yield break;
                if (prologueText == null)
                    break;

                var pos = prologueText.anchoredPosition;
                pos.y = StepScrollY(pos.y, scrollSpeedUnitsPerSecond, Time.unscaledDeltaTime);
                prologueText.anchoredPosition = pos;

                if (viewport != null)
                {
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, prologueText);
                    if (_prologueCanvasGroup != null)
                        _prologueCanvasGroup.alpha = ComputeCrawlFadeAlpha(viewport.rect.center.y, viewport.rect.yMax, bounds.min.y);

                    if (IsCrawlFinished(viewport.rect.yMax, bounds.min.y, 0f))
                        break;
                }
                else if (pos.y >= crawlEndAnchoredY)
                {
                    // Fallback if viewport is unavailable
                    break;
                }

                yield return null;
            }

            // Finished
            SceneFlowManager.I?.SignalScreenDone();
        }

        private void SkipToTitle()
        {
            _skipped = true;
            if (prologuePanel != null) prologuePanel.SetActive(false);
            SceneFlowManager.I?.SignalScreenDone();
        }

        private static bool AnySkipPressedThisFrame()
        {
            var k = Keyboard.current;
            if (k != null && (k.anyKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                return true;

            var m = Mouse.current;
            if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame))
                return true;

            foreach (var g in Gamepad.all)
            {
                if (g == null) continue;
                if (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame)
                    return true;
            }

            return false;
        }
    }
}

