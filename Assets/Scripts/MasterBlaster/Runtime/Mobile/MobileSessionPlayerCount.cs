namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Handheld Android/iOS sessions use a single local human (player 1). Overlay input and
    /// MobilePlayerInput are only attached to player 1; multi-player prefs desync shop turns and arena assignment.
    /// </summary>
    public static class MobileSessionPlayerCount
    {
        /// <summary>True on Android/iOS — one local human per device.</summary>
        public static bool IsHandheldSingleHumanSession() =>
            FlowScreenAccessibilityTextScale.IsHandheldMobile();

        /// <summary>
        /// Effective <c>Players</c> count for arena and shop. On handheld, always 1.
        /// Otherwise matches GameManager clamp: &lt;= 0 → 2, &gt; 5 → 5.
        /// </summary>
        /// <param name="playerCountFromPrefs">Raw value from PlayerPrefs <c>Players</c>.</param>
        /// <param name="handheldSingleHumanOverride">If set, used instead of <see cref="IsHandheldSingleHumanSession"/> (tests / diagnostics).</param>
        public static int GetEffectivePlayerCount(int playerCountFromPrefs, bool? handheldSingleHumanOverride = null)
        {
            bool handheld = handheldSingleHumanOverride ?? IsHandheldSingleHumanSession();
            if (handheld)
                return 1;
            if (playerCountFromPrefs <= 0)
                return 2;
            if (playerCountFromPrefs > 5)
                return 5;
            return playerCountFromPrefs;
        }
    }
}
