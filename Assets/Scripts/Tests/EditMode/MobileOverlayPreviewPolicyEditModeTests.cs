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
    }
}
