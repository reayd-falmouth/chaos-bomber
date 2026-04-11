using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class ArenaShrinkScheduleEditModeTests
    {
        [Test]
        public void ShouldExitMainTimerWhenTimeReachesZero_TrueWhenShrinkingDisabled()
        {
            Assert.IsTrue(ArenaShrinkSchedule.ShouldExitMainTimerWhenTimeReachesZero(shrinkingEnabled: false));
        }

        [Test]
        public void ShouldExitMainTimerWhenTimeReachesZero_FalseWhenShrinkingEnabled()
        {
            Assert.IsFalse(ArenaShrinkSchedule.ShouldExitMainTimerWhenTimeReachesZero(shrinkingEnabled: true));
        }
    }
}
