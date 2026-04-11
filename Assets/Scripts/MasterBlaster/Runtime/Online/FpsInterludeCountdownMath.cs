namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>Pure helpers for the online FPS interlude countdown (testable without Netcode).</summary>
    public static class FpsInterludeCountdownMath
    {
        /// <summary>Returns the sequence of integers shown each tick from countdownSeconds down to 1 (inclusive).</summary>
        public static int[] BuildCountdownSequence(int countdownSeconds)
        {
            if (countdownSeconds < 1)
                return System.Array.Empty<int>();

            var seq = new int[countdownSeconds];
            for (int i = 0; i < countdownSeconds; i++)
                seq[i] = countdownSeconds - i;
            return seq;
        }
    }
}
