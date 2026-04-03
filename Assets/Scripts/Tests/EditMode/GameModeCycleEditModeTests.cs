using HybridGame.MasterBlaster.Scripts;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class GameModeCycleEditModeTests
    {
        [Test]
        public void GetNext_CyclesThreeModes()
        {
            Assert.AreEqual(
                GameModeManager.GameMode.FPS,
                GameModeCycle.GetNext(GameModeManager.GameMode.Bomberman));
            Assert.AreEqual(
                GameModeManager.GameMode.ArenaPerspective,
                GameModeCycle.GetNext(GameModeManager.GameMode.FPS));
            Assert.AreEqual(
                GameModeManager.GameMode.Bomberman,
                GameModeCycle.GetNext(GameModeManager.GameMode.ArenaPerspective));
        }

        [Test]
        public void IsGridPresentationMode_TrueForBombermanAndPerspective()
        {
            Assert.IsTrue(GameModeManager.IsGridPresentationMode(GameModeManager.GameMode.Bomberman));
            Assert.IsTrue(GameModeManager.IsGridPresentationMode(GameModeManager.GameMode.ArenaPerspective));
            Assert.IsFalse(GameModeManager.IsGridPresentationMode(GameModeManager.GameMode.FPS));
        }
    }
}
