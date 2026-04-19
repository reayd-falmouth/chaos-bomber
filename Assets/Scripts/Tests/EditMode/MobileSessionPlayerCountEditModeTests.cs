using HybridGame.MasterBlaster.Scripts.Mobile;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class MobileSessionPlayerCountEditModeTests
    {
        [Test]
        public void GetEffectivePlayerCount_HandheldOverride_ForcesOne()
        {
            Assert.AreEqual(1, MobileSessionPlayerCount.GetEffectivePlayerCount(2, true));
            Assert.AreEqual(1, MobileSessionPlayerCount.GetEffectivePlayerCount(5, true));
        }

        [Test]
        public void GetEffectivePlayerCount_NotHandheld_ClampsLowAndHigh()
        {
            Assert.AreEqual(2, MobileSessionPlayerCount.GetEffectivePlayerCount(0, false));
            Assert.AreEqual(2, MobileSessionPlayerCount.GetEffectivePlayerCount(-3, false));
            Assert.AreEqual(5, MobileSessionPlayerCount.GetEffectivePlayerCount(9, false));
            Assert.AreEqual(3, MobileSessionPlayerCount.GetEffectivePlayerCount(3, false));
        }
    }
}
