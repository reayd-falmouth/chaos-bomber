namespace HybridGame.MasterBlaster.Scripts.Utilities
{
    /// <summary>
    /// Central design resolution for Amiga-style display. Used by Canvas Scaler reference
    /// and letterboxing so the same aspect is used across UI and game view.
    /// </summary>
    public static class DesignResolution
    {
        /// <summary>Design width (e.g. Amiga 640).</summary>
        public const int Width = 640;

        /// <summary>Design height (e.g. Amiga 512). Use 320 for 644x320 if preferred.</summary>
        public const int Height = 512;

        /// <summary>Aspect ratio (width / height).</summary>
        public static float Aspect => Width / (float)Height;
    }
}
