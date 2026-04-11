using HybridGame.MasterBlaster.Scripts.Player;
using NUnit.Framework;
using Unity.FPS.Game;

namespace fps.Tests.EditMode
{
    public class FpsArenaMovementLockEditModeTests
    {
        [Test]
        public void PlayerDualModeController_implements_IFpsArenaMovementLock()
        {
            Assert.IsAssignableFrom(typeof(IFpsArenaMovementLock), typeof(PlayerDualModeController));
        }
    }
}
