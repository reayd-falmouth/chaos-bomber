using HybridGame.MasterBlaster.Scripts;

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    /// <summary>
    /// Priority rules for <see cref="CinemachineModeSwitcher"/> (pure logic for unit tests).
    /// </summary>
    public static class CinemachineModePriorityLogic
    {
        public static int Compute(
            GameModeManager.GameMode mode,
            bool isPlayerCamera,
            bool registryHasAnyArenaPerspectiveMarker,
            bool thisCameraHasArenaPerspectiveMarker,
            int activePriority,
            int inactivePriority)
        {
            if (mode == GameModeManager.GameMode.FPS)
                return isPlayerCamera ? activePriority : inactivePriority;

            if (mode == GameModeManager.GameMode.ArenaPerspective)
            {
                if (!registryHasAnyArenaPerspectiveMarker)
                    return !isPlayerCamera ? activePriority : inactivePriority;
                return thisCameraHasArenaPerspectiveMarker ? activePriority : inactivePriority;
            }

            // Bomberman: only classic arena vcams (not perspective-marked) may tie for high priority.
            if (mode == GameModeManager.GameMode.Bomberman && thisCameraHasArenaPerspectiveMarker)
                return inactivePriority;

            return !isPlayerCamera ? activePriority : inactivePriority;
        }
    }
}
