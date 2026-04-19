namespace Unity.FPS.Game
{
    /// <summary>
    /// Optional gate for <see cref="FpsTouchMoveBridge"/> so only the intended player (e.g. arena player 1)
    /// receives on-screen D-pad movement in hybrid scenes.
    /// </summary>
    public interface IMobileOverlayMoveGate
    {
        bool AllowTouchOverlayMove { get; }
    }
}
