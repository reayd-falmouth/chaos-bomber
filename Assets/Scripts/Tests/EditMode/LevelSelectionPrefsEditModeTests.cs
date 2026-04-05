using HybridGame.MasterBlaster.Scripts.Levels;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class LevelSelectionPrefsEditModeTests
    {
        [Test]
        public void ArenaIndexFromNormalLevel_True_IsZero()
        {
            Assert.AreEqual(0, LevelSelectionPrefs.ArenaIndexFromNormalLevel(true));
        }

        [Test]
        public void ArenaIndexFromNormalLevel_False_IsOne()
        {
            Assert.AreEqual(1, LevelSelectionPrefs.ArenaIndexFromNormalLevel(false));
        }
    }
}
