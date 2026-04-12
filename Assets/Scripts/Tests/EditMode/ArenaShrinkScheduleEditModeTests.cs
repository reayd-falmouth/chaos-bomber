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

        /// <summary>
        /// Default prefabs use shrink at 27s and alarm at 10s remaining. The match clock must keep counting
        /// after shrink begins so the last-10s alarm window is still reachable (TimerRoutine no longer zeros time when shrink starts).
        /// </summary>
        [Test]
        public void AlarmWindow_IsReachable_WhenShrinkThresholdExceedsAlarmThreshold()
        {
            const float matchDuration = 180f;
            float alarmTh = ArenaShrinkSchedule.GetAlarmThresholdRemaining(
                matchDuration,
                useRemainingSeconds: true,
                alarmRemainingSeconds: 10f,
                alarmThresholdFraction: 0.1f
            );
            float shrinkTh = ArenaShrinkSchedule.GetShrinkThresholdRemaining(
                matchDuration,
                useRemainingSeconds: true,
                shrinkRemainingSeconds: 27f,
                shrinkThresholdFraction: 0.15f
            );
            Assert.Greater(shrinkTh, alarmTh, "Sanity: reproduces default prefab ordering.");

            bool sawAlarm = false;
            for (float tr = 35f; tr > 0f; tr -= 0.25f)
            {
                if (ArenaShrinkSchedule.ShouldAlarmBeOn(tr, alarmTh))
                    sawAlarm = true;
            }
            Assert.IsTrue(sawAlarm, "Countdown must pass through the alarm window (last N seconds with time > 0).");
        }
    }
}
