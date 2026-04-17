using HybridGame.MasterBlaster.Scripts.Mobile;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class FlowScreenAccessibilityTextScaleTests
    {
        [Test]
        public void ComputeCombinedTextScale_NonMobile_AlwaysOne()
        {
            float r = FlowScreenAccessibilityTextScale.ComputeCombinedTextScale(
                false,
                1.5f,
                2f,
                1f,
                3f);
            Assert.AreEqual(1f, r, 0.0001f);
        }

        [Test]
        public void ComputeCombinedTextScale_Mobile_MultipliesBoostAndFontScale()
        {
            float r = FlowScreenAccessibilityTextScale.ComputeCombinedTextScale(
                true,
                1f,
                1.2f,
                1f,
                3f);
            Assert.AreEqual(1.2f, r, 0.0001f);
        }

        [Test]
        public void ComputeCombinedTextScale_Mobile_RespectsClampMax()
        {
            float r = FlowScreenAccessibilityTextScale.ComputeCombinedTextScale(
                true,
                2f,
                2f,
                1f,
                3f);
            Assert.AreEqual(3f, r, 0.0001f);
        }

        [Test]
        public void ComputeCombinedTextScale_Mobile_RespectsClampMin()
        {
            float r = FlowScreenAccessibilityTextScale.ComputeCombinedTextScale(
                true,
                1f,
                0.5f,
                1f,
                3f);
            Assert.AreEqual(1f, r, 0.0001f);
        }

        [Test]
        public void ComputeCombinedTextScale_Mobile_LargeSystemFontScale()
        {
            float r = FlowScreenAccessibilityTextScale.ComputeCombinedTextScale(
                true,
                1.3f,
                1.28f,
                1f,
                2.5f);
            Assert.AreEqual(1.664f, r, 0.0001f);
        }
    }
}
