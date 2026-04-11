namespace Unity.FPS.Game
{
    /// <summary>
    /// Optional hook from hybrid arena code (e.g. Stop pickup) to block FPS locomotion while still allowing look/camera.
    /// Implemented by the hybrid player’s PlayerDualModeController on the player.
    /// </summary>
    public interface IFpsArenaMovementLock
    {
        bool IsArenaStopActive { get; }
    }
}
