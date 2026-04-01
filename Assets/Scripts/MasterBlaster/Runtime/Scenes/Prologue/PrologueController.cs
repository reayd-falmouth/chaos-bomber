using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Prologue
{
    public class PrologueController : MonoBehaviour
    {
        [Header("Quote Timing")]
        [Min(0f)] [SerializeField] private float quoteSeconds = 3f;
        [Min(0.01f)] [SerializeField] private float quoteFadeSeconds = 1f;

        [Header("Crawl")]
        [Tooltip("UI units per second (anchoredPosition.y increases upward).")]
        [Min(0f)] [SerializeField] private float scrollSpeedUnitsPerSecond = 30f;
        [Tooltip("Extra padding below the screen (UI units) before the crawl becomes visible.")]
        [Min(0f)] [SerializeField] private float startOffscreenPadding = 0f;
        [Tooltip("AnchoredPosition.y at/above which the crawl is considered finished.")]
        [SerializeField] private float crawlEndAnchoredY = 600f;

        [Header("Scene References")]
        [SerializeField] private GameObject quotePanel;
        [SerializeField] private GameObject prologuePanel;
        [SerializeField] private RectTransform prologueText;

        private CanvasGroup _quoteCanvasGroup;
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

        private void SnapPrologueTextStartOffscreen()
        {
            if (prologueText == null)
                return;

            RectTransform viewport = null;
            if (prologuePanel != null)
                viewport = prologuePanel.GetComponent<RectTransform>();
            if (viewport == null)
                viewport = prologueText.parent as RectTransform;
            if (viewport == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            Canvas.ForceUpdateCanvases();

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, prologueText);
            float deltaY = ComputeStartDeltaY(viewport.rect.yMin, bounds.max.y, startOffscreenPadding);

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
            if (quotePanel != null)
            {
                quotePanel.SetActive(true);

                // Ensure black background exists and fills screen.
                var img = quotePanel.GetComponent<Image>();
                if (img == null)
                    img = quotePanel.AddComponent<Image>();
                img.color = Color.black;
                img.raycastTarget = false;

                _quoteCanvasGroup = quotePanel.GetComponent<CanvasGroup>();
                if (_quoteCanvasGroup == null)
                    _quoteCanvasGroup = quotePanel.AddComponent<CanvasGroup>();
                _quoteCanvasGroup.alpha = 1f;
            }

            if (prologuePanel != null)
            {
                prologuePanel.SetActive(true);
                _prologueCanvasGroup = prologuePanel.GetComponent<CanvasGroup>();
                if (_prologueCanvasGroup == null)
                    _prologueCanvasGroup = prologuePanel.AddComponent<CanvasGroup>();
                _prologueCanvasGroup.alpha = 0f;
            }

            SnapPrologueTextStartOffscreen();

            // Quote hold
            float t = 0f;
            while (t < quoteSeconds)
            {
                if (_skipped) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Crossfade quote -> prologue
            if (_quoteCanvasGroup != null || _prologueCanvasGroup != null)
            {
                float fadeT = 0f;
                while (fadeT < quoteFadeSeconds)
                {
                    if (_skipped) yield break;
                    fadeT += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(fadeT / quoteFadeSeconds);
                    float quoteA = 1f - u;
                    float prologueA = u;
                    if (_quoteCanvasGroup != null)
                        _quoteCanvasGroup.alpha = quoteA;
                    if (_prologueCanvasGroup != null)
                        _prologueCanvasGroup.alpha = prologueA;
                    yield return null;
                }
                if (_quoteCanvasGroup != null)
                    _quoteCanvasGroup.alpha = 0f;
                if (_prologueCanvasGroup != null)
                    _prologueCanvasGroup.alpha = 1f;
            }

            if (quotePanel != null)
                quotePanel.SetActive(false);

            // Crawl
            while (true)
            {
                if (_skipped) yield break;
                if (prologueText == null)
                    break;

                var pos = prologueText.anchoredPosition;
                pos.y = StepScrollY(pos.y, scrollSpeedUnitsPerSecond, Time.unscaledDeltaTime);
                prologueText.anchoredPosition = pos;

                if (pos.y >= crawlEndAnchoredY)
                    break;

                yield return null;
            }

            // Finished
            SceneFlowManager.I?.GoTo(FlowState.Title);
        }

        private void SkipToTitle()
        {
            _skipped = true;
            if (quotePanel != null) quotePanel.SetActive(false);
            if (prologuePanel != null) prologuePanel.SetActive(false);
            SceneFlowManager.I?.GoTo(FlowState.Title);
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

