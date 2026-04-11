using HybridGame.MasterBlaster.Scripts.Online;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class FpsInterludeCountdownMathEditModeTests
    {
        [Test]
        public void BuildCountdownSequence_FiveSeconds_CountsDownToOne()
        {
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, FpsInterludeCountdownMath.BuildCountdownSequence(5));
        }

        [Test]
        public void BuildCountdownSequence_OneSecond_IsSingleOne()
        {
            CollectionAssert.AreEqual(new[] { 1 }, FpsInterludeCountdownMath.BuildCountdownSequence(1));
        }

        [Test]
        public void BuildCountdownSequence_ZeroOrNegative_IsEmpty()
        {
            Assert.IsEmpty(FpsInterludeCountdownMath.BuildCountdownSequence(0));
            Assert.IsEmpty(FpsInterludeCountdownMath.BuildCountdownSequence(-3));
        }
    }
}
