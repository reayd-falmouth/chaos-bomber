using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Pure match clock math for arena alarm / shrink timing (testable without networking).
    /// Alarm is always defined to start slightly before shrink: when remaining time ≤ shrink + lead.
    /// </summary>
    public static class ArenaShrinkSchedule
    {
        /// <summary>
        /// Seconds left on the match clock when the wall shrink begins (remaining ≤ this value).
        /// </summary>
        public static float GetShrinkThresholdRemaining(float shrinkRemainingSeconds)
        {
            return Mathf.Max(0f, shrinkRemainingSeconds);
        }

        /// <summary>
        /// Alarm starts when remaining time ≤ shrink threshold + lead, so it begins before shrink by <paramref name="alarmLeadSecondsBeforeShrink"/> seconds of clock time.
        /// </summary>
        public static float GetAlarmThresholdRemainingBeforeShrink(
            float shrinkRemainingSeconds,
            float alarmLeadSecondsBeforeShrink
        )
        {
            return Mathf.Max(
                0f,
                GetShrinkThresholdRemaining(shrinkRemainingSeconds) + Mathf.Max(0f, alarmLeadSecondsBeforeShrink)
            );
        }

        public static bool ShouldAlarmBeOn(float timeRemaining, float alarmThresholdRemaining)
        {
            // When the clock hits zero the round is over; do not keep the alarm latched on 0s remaining.
            return timeRemaining > 0f && timeRemaining <= alarmThresholdRemaining;
        }

        public static bool ShouldStartShrinkByRemaining(float timeRemaining, float shrinkThresholdRemaining)
        {
            return timeRemaining <= shrinkThresholdRemaining;
        }

        /// <summary>
        /// After alarm has fired, shrink when this many seconds of real time have passed (server/host only).
        /// </summary>
        public static bool ShouldStartShrinkAfterAlarmDelay(
            bool alarmHasFired,
            float alarmStartTime,
            float delaySeconds,
            float timeNow
        )
        {
            if (!alarmHasFired || delaySeconds <= 0f)
                return false;
            return timeNow >= alarmStartTime + delaySeconds;
        }

        /// <summary>
        /// When arena shrink is enabled, the timer may sit at 0 while waiting for shrink to start/finish — do not exit the main loop on time alone.
        /// </summary>
        public static bool ShouldExitMainTimerWhenTimeReachesZero(bool shrinkingEnabled)
        {
            return !shrinkingEnabled;
        }
    }
}
