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
        [Min(0f)] [SerializeField] private float scrollSpeedUnitsPerSecond = 100f;
        [Tooltip("Extra space below the viewport bottom before the crawl becomes visible (viewport-local units).")]
        [Min(0f)] [SerializeField] private float startPadding = 0f;
        [Tooltip("Added after snap; positive moves the whole crawl upward (parent-local Y).")]
        [SerializeField] private float crawlStartVerticalOffset = 0f;
        [HideInInspector] [SerializeField] private float crawlEndAnchoredY = 0f;

        [Header("Scene References")]
        [SerializeField] private GameObject prologuePanel;
        [SerializeField] private RectTransform prologueText;

        private CanvasGroup _prologueCanvasGroup;
        private bool _skipped;
        private Coroutine _routine;
        private float _ignoreSkipUntilUnscaledTime;

        [Header("Skip")]
        [Tooltip("Ignore skip input for a short time after enabling (prevents accidental skip from the click/key that started Play mode or advanced the previous screen).")]
        [Min(0f)] [SerializeField] private float skipGraceSeconds = 0.25f;

        // #region agent log
        private static void AgentLog(string runId, string hypothesisId, string location, string message, object data = null)
        {
            try
            {
                var payload = new
                {
                    sessionId = "d5eb48",
                    runId,
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                System.IO.File.AppendAllText("debug-d5eb48.log", UnityEngine.JsonUtility.ToJson(payload) + "\n");
            }
            catch { }
        }
        // #endregion

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

        /// <summary>
        /// Local height used for top/bottom math (Unity <see cref="RectTransform.rect"/> is unscaled; scale affects layout).
        /// </summary>
        private static float GetTextBlockExtentY(RectTransform text)
        {
            if (text == null)
                return 0f;
            return text.rect.height * Mathf.Abs(text.localScale.y);
        }

        /// <summary>Top edge Y of the text rect in the same space as <see cref="RectTransform.anchoredPosition"/> (parent = viewport).</summary>
        public static float GetContentTopY(RectTransform text)
        {
            if (text == null)
                return 0f;
            float h = GetTextBlockExtentY(text);
            float pivotY = text.pivot.y;
            return text.anchoredPosition.y + (1f - pivotY) * h;
        }

        /// <summary>Bottom edge Y of the text rect in parent-local space.</summary>
        public static float GetContentBottomY(RectTransform text)
        {
            if (text == null)
                return 0f;
            float h = GetTextBlockExtentY(text);
            float pivotY = text.pivot.y;
            return text.anchoredPosition.y - pivotY * h;
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

            // Use viewport.rect + anchored math in the text parent's space. Axis-aligned bounds from
            // RectTransformUtility often mis-size scaled/rotated nested canvases and push the start too far down.
            float viewportYMin = viewport.rect.yMin;
            float contentTopY = GetContentTopY(prologueText);
            float deltaY = ComputeStartDeltaY(viewportYMin, contentTopY, startPadding);

            var pos = prologueText.anchoredPosition;
            pos.y += deltaY + crawlStartVerticalOffset;
            prologueText.anchoredPosition = pos;
        }

        void OnEnable()
        {
            _skipped = false;
            _ignoreSkipUntilUnscaledTime = Time.unscaledTime + Mathf.Max(0f, skipGraceSeconds);
            AgentLog("pre-fix-1", "E", "PrologueController.cs:OnEnable", "enabled", new { hasPanel = prologuePanel != null, hasText = prologueText != null, scrollSpeedUnitsPerSecond });

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

            if (Time.unscaledTime >= _ignoreSkipUntilUnscaledTime && AnySkipPressedThisFrame())
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
                    float contentBottomY = GetContentBottomY(prologueText);
                    var rect = viewport.rect;

                    if (_prologueCanvasGroup != null)
                        _prologueCanvasGroup.alpha = ComputeCrawlFadeAlpha(
                            rect.center.y,
                            rect.yMax,
                            contentBottomY);

                    if (IsCrawlFinished(rect.yMax, contentBottomY, 0f))
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
            AgentLog("pre-fix-1", "E", "PrologueController.cs:Run", "crawl finished -> SignalScreenDone", null);
            SceneFlowManager.I?.SignalScreenDone();
        }

        private void SkipToTitle()
        {
            _skipped = true;
            if (prologuePanel != null) prologuePanel.SetActive(false);
            AgentLog("pre-fix-1", "E", "PrologueController.cs:SkipToTitle", "skip -> SignalScreenDone", null);
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

