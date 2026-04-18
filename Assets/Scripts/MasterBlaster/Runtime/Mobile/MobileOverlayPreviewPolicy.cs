using HybridGame.MasterBlaster.Scripts.Core;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Pure helpers for preview visibility; used by <see cref="MobileOverlayBootstrap"/> and EditMode tests.
    /// </summary>
    public static class MobileOverlayPreviewPolicy
    {
        public static bool ShouldShowControlsForFlow(
            bool previewSimulateHandheld,
            bool previewIgnoreFlowState,
            FlowState? currentState)
        {
            if (previewSimulateHandheld && previewIgnoreFlowState)
                return true;

            if (currentState == null)
                return true;

            FlowState s = currentState.Value;
            return s != FlowState.Quote && s != FlowState.Prologue;
        }
    }
}
