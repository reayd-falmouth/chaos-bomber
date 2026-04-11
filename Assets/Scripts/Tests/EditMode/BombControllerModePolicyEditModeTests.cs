using HybridGame.MasterBlaster.Scripts;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class BombControllerModePolicyEditModeTests
    {
        [Test]
        public void ShouldEnableBombController_TrueForBombermanFpsAndArenaPerspective()
        {
            Assert.IsTrue(GameModeManager.ShouldEnableBombController(GameModeManager.GameMode.Bomberman));
            Assert.IsTrue(GameModeManager.ShouldEnableBombController(GameModeManager.GameMode.FPS));
            Assert.IsTrue(GameModeManager.ShouldEnableBombController(GameModeManager.GameMode.ArenaPerspective));
        }

        [Test]
        public void ShouldEnableBombController_MatchesGridOrFps()
        {
            foreach (GameModeManager.GameMode mode in System.Enum.GetValues(typeof(GameModeManager.GameMode)))
            {
                bool expected = GameModeManager.IsGridPresentationMode(mode) || mode == GameModeManager.GameMode.FPS;
                Assert.AreEqual(expected, GameModeManager.ShouldEnableBombController(mode), $"mode={mode}");
            }
        }
    }
}
