namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Visit order for filling the inner playable rectangle with indestructible shrink blocks.
    /// </summary>
    public enum ArenaShrinkPattern
    {
        /// <summary>Boustrophedon rows, top to bottom (alternating row direction).</summary>
        BoustrophedonSnake = 0,

        /// <summary>Layered perimeter: outer ring clockwise, then inward (classic “spiral” wall close-in).</summary>
        SpiralInward = 1,
    }
}
