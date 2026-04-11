using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class AlarmPresentationPulseEditModeTests
    {
        [Test]
        public void SinPulse01_AtTimeZero_IsHalf()
        {
            float v = AlarmPresentationPulse.SinPulse01(0f, 5f);
            Assert.AreEqual(0.5f, v, 1e-6f);
        }

        [Test]
        public void SinPulse01WithPhase_AtTimeZero_DependsOnPhase()
        {
            float v = AlarmPresentationPulse.SinPulse01WithPhase(0f, 5f, Mathf.PI * 0.5f);
            Assert.AreEqual(1f, v, 1e-5f);
        }
    }
}
