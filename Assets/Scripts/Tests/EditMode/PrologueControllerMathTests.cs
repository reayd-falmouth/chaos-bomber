using NUnit.Framework;
using HybridGame.MasterBlaster.Scripts.Scenes.Prologue;

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
    }
}

