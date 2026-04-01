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

        [Test]
        public void GetBackdropApplyStateForSceneLoaded_AfterBootstrap_UsesPersistedState()
        {
            Assert.AreEqual(
                FlowState.Menu,
                SceneFlowManager.GetBackdropApplyStateForSceneLoaded(
                    flowBootstrapComplete: true,
                    persistedState: FlowState.Menu,
                    overrideStartState: false,
                    overrideStartStateValue: FlowState.Game,
                    bootFromHierarchy: FlowState.Quote));
        }

        [Test]
        public void GetBackdropApplyStateForSceneLoaded_BeforeBootstrap_UsesOverrideWhenEnabled()
        {
            Assert.AreEqual(
                FlowState.Game,
                SceneFlowManager.GetBackdropApplyStateForSceneLoaded(
                    flowBootstrapComplete: false,
                    persistedState: FlowState.Controls,
                    overrideStartState: true,
                    overrideStartStateValue: FlowState.Game,
                    bootFromHierarchy: FlowState.Quote));
        }

        [Test]
        public void GetBackdropApplyStateForSceneLoaded_BeforeBootstrap_UsesHierarchyWhenNoOverride()
        {
            Assert.AreEqual(
                FlowState.Quote,
                SceneFlowManager.GetBackdropApplyStateForSceneLoaded(
                    flowBootstrapComplete: false,
                    persistedState: FlowState.Controls,
                    overrideStartState: false,
                    overrideStartStateValue: FlowState.Game,
                    bootFromHierarchy: FlowState.Quote));
        }
    }
}

