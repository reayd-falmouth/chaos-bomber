using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Mobile;
using NUnit.Framework;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class MobileOverlayPreviewPolicyEditModeTests
    {
        [Test]
        public void ShouldShowControls_IgnoreFlow_OverridesQuoteAndPrologue()
        {
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(true, true, FlowState.Quote));
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(true, true, FlowState.Prologue));
        }

        [Test]
        public void ShouldShowControls_NoPreview_HidesOnQuotePrologue()
        {
            Assert.IsFalse(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(false, false, FlowState.Quote));
            Assert.IsFalse(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(false, false, FlowState.Prologue));
        }

        [Test]
        public void ShouldShowControls_NoPreview_ShowsOnOtherStates()
        {
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(false, false, FlowState.Title));
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(false, false, FlowState.Game));
        }

        [Test]
        public void ShouldShowControls_NullFlow_IsVisible()
        {
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldShowControlsForFlow(false, false, null));
        }

        [Test]
        public void MobileOverlayBootstrap_PreviewFlags_DefaultFalseWithoutEditorPreview()
        {
            MobileOverlayBootstrap.SetPreviewOverlayState(false, false);
            Assert.IsFalse(MobileOverlayBootstrap.PreviewSimulateHandheldActive);
            Assert.IsFalse(MobileOverlayBootstrap.PreviewIgnoreFlowStateActive);
        }

        [Test]
        public void SetPreviewOverlayState_FalseToTrue_FiresHandheldPreviewBecameActive()
        {
            MobileOverlayBootstrap.SetPreviewOverlayState(false, false);
            bool fired = false;
            void Handler()
            {
                fired = true;
            }

            MobileOverlayBootstrap.HandheldPreviewBecameActive += Handler;
            try
            {
                MobileOverlayBootstrap.SetPreviewOverlayState(true, false);
                Assert.IsTrue(fired, "GameManager subscribes to re-attach MobilePlayerInput when preview turns on after load.");
            }
            finally
            {
                MobileOverlayBootstrap.HandheldPreviewBecameActive -= Handler;
                MobileOverlayBootstrap.SetPreviewOverlayState(false, false);
            }
        }
    }
}
