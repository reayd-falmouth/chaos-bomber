using System.Reflection;
using HybridGame.MasterBlaster.Scripts.Core;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class ScanlinesToggleEditModeTests
    {
        sealed class DummyWithActiveProperty
        {
            public bool active { get; set; } = true;
        }

        sealed class DummyWithActiveField
        {
            public bool active;
        }

        [Test]
        public void TrySetVolumeComponentActive_PublicProperty_SetsValue()
        {
            var dummy = new DummyWithActiveProperty { active = true };
            var ok = InvokeTrySetVolumeComponentActive(dummy, false);
            Assert.IsTrue(ok);
            Assert.IsFalse(dummy.active);
        }

        [Test]
        public void TrySetVolumeComponentActive_PublicField_SetsValue()
        {
            var dummy = new DummyWithActiveField { active = true };
            var ok = InvokeTrySetVolumeComponentActive(dummy, false);
            Assert.IsTrue(ok);
            Assert.IsFalse(dummy.active);
        }

        [Test]
        public void TrySetVolumeComponentActive_Null_ReturnsFalse()
        {
            var ok = InvokeTrySetVolumeComponentActive(null, false);
            Assert.IsFalse(ok);
        }

        static bool InvokeTrySetVolumeComponentActive(object target, bool active)
        {
            var mi = typeof(GlobalPauseMenuController).GetMethod(
                "TrySetVolumeComponentActive",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "TrySetVolumeComponentActive should exist on GlobalPauseMenuController.");
            return (bool)mi.Invoke(null, new object[] { target, active });
        }
    }
}
