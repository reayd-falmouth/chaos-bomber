using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Quote
{
    public class QuoteController : MonoBehaviour
    {
        [Header("Quote Timing")]
        [Min(0f)] [SerializeField] private float quoteSeconds = 10f;

        [Header("Scene References")]
        [SerializeField] private GameObject quotePanel;

        private Coroutine _routine;
        private Transform _quoteOriginalParent;
        private int _quoteOriginalSiblingIndex;

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

        void OnEnable()
        {
            AgentLog("pre-fix-1", "D", "QuoteController.cs:OnEnable", "enabled", new { quoteSeconds, hasQuotePanel = quotePanel != null });
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

        private IEnumerator Run()
        {
            PrepareQuoteUi();
            AgentLog("pre-fix-1", "D", "QuoteController.cs:Run", "quote started", null);

            float t = 0f;
            while (t < quoteSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            TearDownQuoteUi();
            AgentLog("pre-fix-1", "D", "QuoteController.cs:Run", "quote finished -> SignalScreenDone", null);
            SceneFlowManager.I?.SignalScreenDone();
        }

        private static void EnsureFullScreenStretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void EnsureCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
                return;
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                return;
            if (canvas.worldCamera != null)
                return;

            var uiCanvasCamera = GetComponentInParent<UnityEngine.Camera>();
            canvas.worldCamera = uiCanvasCamera != null ? uiCanvasCamera : UnityEngine.Camera.main;
        }

        private static Transform FindUiCanvasRoot()
        {
            var go = GameObject.Find("UI Canvas");
            if (go != null)
                return go.transform;

            var anyCanvas = FindAnyObjectByType<Canvas>();
            return anyCanvas != null ? anyCanvas.transform : null;
        }

        private void PrepareQuoteUi()
        {
            if (quotePanel == null)
                return;

            quotePanel.SetActive(true);

            var quoteCanvas = quotePanel.GetComponentInParent<Canvas>();
            EnsureCanvasCamera(quoteCanvas);

            // Full-screen Image so layout/stretch works; keep transparent so the shared UI Canvas
            // root (tinted by SceneFlowManager / FlowCanvasRoot) remains visible underneath.
            var img = quotePanel.GetComponent<Image>();
            if (img == null)
                img = quotePanel.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;

            var quoteRt = quotePanel.GetComponent<RectTransform>();
            if (quoteRt != null)
            {
                var targetParent = FindUiCanvasRoot();
                if (targetParent != null)
                {
                    _quoteOriginalParent = quoteRt.parent;
                    _quoteOriginalSiblingIndex = quoteRt.GetSiblingIndex();

                    quoteRt.SetParent(targetParent, worldPositionStays: false);
                    quoteRt.localScale = Vector3.one;
                    EnsureFullScreenStretch(quoteRt);
                    quoteRt.SetAsLastSibling();
                }
                else
                {
                    EnsureFullScreenStretch(quoteRt);
                }
            }
        }

        private void TearDownQuoteUi()
        {
            if (quotePanel == null)
                return;

            quotePanel.SetActive(false);

            var quoteRt = quotePanel.GetComponent<RectTransform>();
            if (quoteRt == null)
                return;

            if (_quoteOriginalParent == null)
                return;

            quoteRt.SetParent(_quoteOriginalParent, worldPositionStays: false);
            quoteRt.SetSiblingIndex(_quoteOriginalSiblingIndex);
            _quoteOriginalParent = null;
        }
    }
}

