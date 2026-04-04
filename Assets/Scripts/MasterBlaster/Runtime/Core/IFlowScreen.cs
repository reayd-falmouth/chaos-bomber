namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Optional lifecycle for flow screens driven by <see cref="FlowCanvasRoot"/> managed behaviours.
    /// Invoked when the screen becomes the active flow state (after enable) or when it is dismissed (before disable).
    /// </summary>
    public interface IFlowScreen
    {
        void OnFlowPresented();
        void OnFlowDismissed();
    }
}
