using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class ArenaShrinkScheduleEditModeTests
    {
        [Test]
        public void ThresholdRemainingFromFraction_LastTenPercent_IsEighteenSecondsOfThreeMinutes()
        {
            Assert.AreEqual(18f, ArenaShrinkSchedule.ThresholdRemainingFromFraction(180f, 0.1f), 0.001f);
        }

        [Test]
        public void ShouldAlarmBeOn_TrueWhenWithinWindowAndTimeLeft()
        {
            Assert.IsTrue(ArenaShrinkSchedule.ShouldAlarmBeOn(18f, 18f));
            Assert.IsTrue(ArenaShrinkSchedule.ShouldAlarmBeOn(1f, 18f));
        }

        [Test]
        public void ShouldAlarmBeOn_FalseWhenClockAtZero()
        {
            Assert.IsFalse(ArenaShrinkSchedule.ShouldAlarmBeOn(0f, 18f));
        }

        [Test]
        public void GetAlarmThresholdRemaining_SecondsMode_IgnoresFraction()
        {
            float t = ArenaShrinkSchedule.GetAlarmThresholdRemaining(
                180f,
                true,
                12f,
                0.99f
            );
            Assert.AreEqual(12f, t);
        }

        [Test]
        public void ShouldStartShrinkAfterAlarmDelay_RespectsDelay()
        {
            Assert.IsFalse(
                ArenaShrinkSchedule.ShouldStartShrinkAfterAlarmDelay(true, 10f, 5f, 12f)
            );
            Assert.IsTrue(
                ArenaShrinkSchedule.ShouldStartShrinkAfterAlarmDelay(true, 10f, 5f, 15.1f)
            );
        }

        [Test]
        public void ShouldStartShrinkAfterAlarmDelay_ZeroDelay_NeverTrue()
        {
            Assert.IsFalse(
                ArenaShrinkSchedule.ShouldStartShrinkAfterAlarmDelay(true, 10f, 0f, 100f)
            );
        }
    }
}
