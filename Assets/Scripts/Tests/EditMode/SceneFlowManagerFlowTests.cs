using NUnit.Framework;
using HybridGame.MasterBlaster.Scripts.Core;

namespace fps.Tests.EditMode
{
    public class SceneFlowManagerFlowTests
    {
        [Test]
        public void GetNextState_QuoteThenPrologueThenTitle()
        {
            Assert.AreEqual(FlowState.Prologue, SceneFlowManager.GetNextState(FlowState.Quote));
            Assert.AreEqual(FlowState.Title, SceneFlowManager.GetNextState(FlowState.Prologue));
        }

        [Test]
        public void ShouldAdvanceOnAnyInput_IncludesQuoteAndPrologue()
        {
            Assert.IsTrue(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Quote));
            Assert.IsTrue(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Prologue));
        }
    }
}

