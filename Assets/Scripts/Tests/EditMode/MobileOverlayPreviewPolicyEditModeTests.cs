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
        public void ShouldLetterbox_FollowGame_UsesFlowFlag()
        {
            Assert.IsFalse(MobileOverlayPreviewPolicy.ShouldLetterboxBeActive(
                true,
                MobileOverlayPreviewLetterboxMode.FollowGameFlow,
                false));
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldLetterboxBeActive(
                true,
                MobileOverlayPreviewLetterboxMode.FollowGameFlow,
                true));
        }

        [Test]
        public void ShouldLetterbox_ForceModes_IgnoreFlow()
        {
            Assert.IsFalse(MobileOverlayPreviewPolicy.ShouldLetterboxBeActive(
                true,
                MobileOverlayPreviewLetterboxMode.ForceHidden,
                true));
            Assert.IsTrue(MobileOverlayPreviewPolicy.ShouldLetterboxBeActive(
                true,
                MobileOverlayPreviewLetterboxMode.ForceShown,
                false));
        }

        [Test]
        public void ShouldLetterbox_PreviewOff_AlwaysFollowsFlow()
        {
            Assert.IsFalse(MobileOverlayPreviewPolicy.ShouldLetterboxBeActive(
                false,
                MobileOverlayPreviewLetterboxMode.ForceShown,
                false));
        }
    }
}
