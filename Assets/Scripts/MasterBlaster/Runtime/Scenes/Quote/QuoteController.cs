using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Quote
{
    public class QuoteController : MonoBehaviour
    {
        [Header("Quote Timing")]
        [Min(0f)] [SerializeField] private float quoteSeconds = 3f;

        [Header("Scene References")]
        [SerializeField] private GameObject quotePanel;

        private Coroutine _routine;
        private Transform _quoteOriginalParent;
        private int _quoteOriginalSiblingIndex;

        void OnEnable()
        {
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

            float t = 0f;
            while (t < quoteSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            TearDownQuoteUi();
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

            // Ensure black background exists and fills screen.
            var img = quotePanel.GetComponent<Image>();
            if (img == null)
                img = quotePanel.AddComponent<Image>();
            img.color = Color.black;
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

