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

        [Test]
        public void AlarmThreshold_IsShrinkPlusLead_DefaultStyle()
        {
            float shrinkTh = ArenaShrinkSchedule.GetShrinkThresholdRemaining(27f);
            float alarmTh = ArenaShrinkSchedule.GetAlarmThresholdRemainingBeforeShrink(27f, 3f);
            Assert.AreEqual(27f, shrinkTh, 1e-5f);
            Assert.AreEqual(30f, alarmTh, 1e-5f);
            Assert.Greater(alarmTh, shrinkTh);
        }

        [Test]
        public void AlarmStartsOnClockBeforeShrink_WhenLeadIsPositive()
        {
            float shrinkTh = ArenaShrinkSchedule.GetShrinkThresholdRemaining(27f);
            float alarmTh = ArenaShrinkSchedule.GetAlarmThresholdRemainingBeforeShrink(27f, 3f);

            Assert.IsTrue(ArenaShrinkSchedule.ShouldAlarmBeOn(30f, alarmTh), "Alarm at 30s left");
            Assert.IsFalse(
                ArenaShrinkSchedule.ShouldStartShrinkByRemaining(30f, shrinkTh),
                "Shrink not yet at 30s left"
            );
            Assert.IsTrue(ArenaShrinkSchedule.ShouldAlarmBeOn(27.5f, alarmTh));
            Assert.IsTrue(ArenaShrinkSchedule.ShouldStartShrinkByRemaining(27f, shrinkTh), "Shrink at 27s left");
        }

        [Test]
        public void CountdownPassesThroughAlarmWindowWhileShrinkPossible()
        {
            float alarmTh = ArenaShrinkSchedule.GetAlarmThresholdRemainingBeforeShrink(27f, 3f);
            bool sawAlarm = false;
            for (float tr = 35f; tr > 0f; tr -= 0.25f)
            {
                if (ArenaShrinkSchedule.ShouldAlarmBeOn(tr, alarmTh))
                    sawAlarm = true;
            }
            Assert.IsTrue(sawAlarm);
        }
    }
}
