using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Editor/dev helper: add to a scene GameObject, enable <see cref="simulateHandheldOverlay"/> in Play Mode
    /// to show the runtime mobile D-pad overlay without building to Android/iOS.
    /// Disable this component or GameObject for normal Editor play and for release builds.
    /// </summary>
    public class MobileOverlayPreviewController : MonoBehaviour
    {
        [Header("Preview")]
        [Tooltip("When enabled (and this component is enabled), the mobile overlay is built and shown in Play Mode even in the Editor.")]
        [SerializeField]
        private bool simulateHandheldOverlay;

        [Tooltip("While simulating, skip Quote/Prologue suppression so controls stay visible for layout.")]
        [SerializeField]
        private bool ignoreFlowStateForVisibility;

        [Tooltip("Letterbox mask while previewing: follow Game flow state, or force on/off for framing the D-pad.")]
        [SerializeField]
        private MobileOverlayPreviewLetterboxMode letterboxVisibility = MobileOverlayPreviewLetterboxMode.FollowGameFlow;

        [Header("Diagnostics")]
        [SerializeField]
        [Tooltip("Log once when preview simulation becomes active (subsystem [MasterBlaster][MobileOverlay][Preview]).")]
        private bool logWhenPreviewStarts = true;

        private bool _loggedPreviewStart;

        private void OnEnable()
        {
            SyncPreviewState();
        }

        private void OnDisable()
        {
            MobileOverlayBootstrap.SetPreviewOverlayState(
                false,
                false,
                MobileOverlayPreviewLetterboxMode.FollowGameFlow);
            _loggedPreviewStart = false;
        }

        private void Update()
        {
            SyncPreviewState();
        }

        private void SyncPreviewState()
        {
            bool simulate = isActiveAndEnabled && simulateHandheldOverlay;
            MobileOverlayBootstrap.SetPreviewOverlayState(
                simulate,
                simulate && ignoreFlowStateForVisibility,
                letterboxVisibility);

            if (!simulate)
            {
                _loggedPreviewStart = false;
                return;
            }

            MobileOverlayBootstrap.EnsurePresent();

            if (logWhenPreviewStarts && !_loggedPreviewStart)
            {
                _loggedPreviewStart = true;
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileOverlay][Preview] Simulation active. " +
                    $"ignoreFlowStateForVisibility={ignoreFlowStateForVisibility}, letterboxVisibility={letterboxVisibility}. " +
                    "Disable this component when finished tuning layout.");
            }
        }
    }
}
