using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    /// <summary>
    /// Smoke test so HybridMatchAlarmTimer stays referenced by the test assembly (compile coverage).
    /// </summary>
    public class HybridMatchAlarmTimerEditModeTests
    {
        [Test]
        public void HybridMatchAlarmTimer_Type_IsLoadable()
        {
            Assert.AreEqual(
                nameof(HybridMatchAlarmTimer),
                typeof(HybridMatchAlarmTimer).Name);
        }
    }
}
