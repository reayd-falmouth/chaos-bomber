using NUnit.Framework;
using HybridGame.MasterBlaster.Scripts.Scenes.Prologue;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class PrologueControllerMathTests
    {
        [Test]
        public void StepScrollY_IncreasesBySpeedTimesDeltaTime()
        {
            float y0 = -600f;
            float speed = 30f;
            float dt = 0.5f;

            float y1 = PrologueController.StepScrollY(y0, speed, dt);

            Assert.AreEqual(-585f, y1, 0.0001f);
        }

        [Test]
        public void ComputeStartDeltaY_MovesContentTopToViewportBottomMinusPadding()
        {
            float viewportYMin = -540f;
            float contentTopY = 120f;
            float padding = 20f;

            float deltaY = PrologueController.ComputeStartDeltaY(viewportYMin, contentTopY, padding);

            Assert.AreEqual((viewportYMin - padding), contentTopY + deltaY, 0.0001f);
            Assert.AreEqual((viewportYMin - padding) - contentTopY, deltaY, 0.0001f);
        }

        [Test]
        public void ComputeStartDeltaY_IsNegativeWhenContentIsAboveViewportBottom()
        {
            float viewportYMin = -540f;
            float contentTopY = 120f;
            float padding = 0f;

            float deltaY = PrologueController.ComputeStartDeltaY(viewportYMin, contentTopY, padding);

            Assert.Less(deltaY, 0f);
        }

        [Test]
        public void IsCrawlFinished_ReturnsTrueWhenContentBottomIsAboveViewportTopPlusPadding()
        {
            float viewportYMax = 540f;
            float padding = 10f;

            Assert.IsFalse(PrologueController.IsCrawlFinished(viewportYMax, 549.9f, padding));
            Assert.IsTrue(PrologueController.IsCrawlFinished(viewportYMax, 550f, padding));
            Assert.IsTrue(PrologueController.IsCrawlFinished(viewportYMax, 700f, padding));
        }

        [Test]
        public void ComputeCrawlFadeAlpha_IsOneUntilBottomReachesCenter_ThenLinearlyToZeroAtTop()
        {
            float centerY = 0f;
            float topY = 100f;

            Assert.AreEqual(1f, PrologueController.ComputeCrawlFadeAlpha(centerY, topY, -50f), 0.0001f);
            Assert.AreEqual(1f, PrologueController.ComputeCrawlFadeAlpha(centerY, topY, 0f), 0.0001f);
            Assert.AreEqual(0.5f, PrologueController.ComputeCrawlFadeAlpha(centerY, topY, 50f), 0.0001f);
            Assert.AreEqual(0f, PrologueController.ComputeCrawlFadeAlpha(centerY, topY, 100f), 0.0001f);
            Assert.AreEqual(0f, PrologueController.ComputeCrawlFadeAlpha(centerY, topY, 150f), 0.0001f);
        }

        [Test]
        public void GetContentTopY_AndBottom_UsePivotAndScaledRectHeight()
        {
            var go = new GameObject("Rt", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 200f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = new Vector3(2f, 2f, 1f);
            rt.anchoredPosition = new Vector2(0f, 100f);

            float h = rt.rect.height * Mathf.Abs(rt.localScale.y);
            float top = PrologueController.GetContentTopY(rt);
            float bottom = PrologueController.GetContentBottomY(rt);

            Assert.AreEqual(rt.anchoredPosition.y + (1f - rt.pivot.y) * h, top, 0.02f);
            Assert.AreEqual(rt.anchoredPosition.y - rt.pivot.y * h, bottom, 0.02f);

            Object.DestroyImmediate(go);
        }
    }
}

