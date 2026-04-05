using HybridGame.MasterBlaster.Scripts.Player;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class AnimatedSpriteRendererWalkTimingTests
    {
        [Test]
        public void ComputeScaledFrameInterval_DoubleSpeed_HalvesInterval()
        {
            float baseInterval = 1f / 6f;
            float referenceSpeed = 5f;
            float atRef = AnimatedSpriteRenderer.ComputeScaledFrameInterval(baseInterval, referenceSpeed, 5f);
            float atDouble = AnimatedSpriteRenderer.ComputeScaledFrameInterval(baseInterval, referenceSpeed, 10f);
            Assert.AreEqual(atRef * 0.5f, atDouble, 1e-5f);
        }
    }
}
