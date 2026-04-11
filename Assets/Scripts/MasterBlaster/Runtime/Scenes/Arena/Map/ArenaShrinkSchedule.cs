using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Pure match clock math for arena alarm / shrink timing (testable without networking).
    /// </summary>
    public static class ArenaShrinkSchedule
    {
        public static float ThresholdRemainingFromFraction(float matchDuration, float fraction)
        {
            return matchDuration * Mathf.Clamp01(fraction);
        }

        /// <param name="useRemainingSeconds">
        /// When true, <paramref name="alarmRemainingSeconds"/> is used as a direct “seconds left” threshold (same unit as the match clock).
        /// When false, <paramref name="alarmThresholdFraction"/> × <paramref name="matchDuration"/> is used.
        /// </param>
        public static float GetAlarmThresholdRemaining(
            float matchDuration,
            bool useRemainingSeconds,
            float alarmRemainingSeconds,
            float alarmThresholdFraction
        )
        {
            return useRemainingSeconds
                ? Mathf.Max(0f, alarmRemainingSeconds)
                : ThresholdRemainingFromFraction(matchDuration, alarmThresholdFraction);
        }

        public static float GetShrinkThresholdRemaining(
            float matchDuration,
            bool useRemainingSeconds,
            float shrinkRemainingSeconds,
            float shrinkThresholdFraction
        )
        {
            return useRemainingSeconds
                ? Mathf.Max(0f, shrinkRemainingSeconds)
                : ThresholdRemainingFromFraction(matchDuration, shrinkThresholdFraction);
        }

        public static bool ShouldAlarmBeOn(float timeRemaining, float alarmThresholdRemaining)
        {
            // When the clock hits zero the round is over; do not keep the alarm latched on 0s remaining.
            return timeRemaining > 0f && timeRemaining <= alarmThresholdRemaining;
        }

        public static bool ShouldStartShrinkByRemaining(
            float timeRemaining,
            float shrinkThresholdRemaining
        )
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
