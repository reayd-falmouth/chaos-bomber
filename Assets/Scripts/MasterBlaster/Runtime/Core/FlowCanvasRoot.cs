using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>How the shared "UI Canvas" root <see cref="UnityEngine.UI.Image"/> is painted while this flow screen is active.</summary>
    public enum UiCanvasBackgroundMode
    {
        /// <summary>Use <see cref="SceneFlowManager"/> default color and optional default backdrop sprite (or cached canvas sprite).</summary>
        Default = 0,
        SolidColor = 1,
        Sprite = 2
    }

    /// <summary>Transition played when navigating <em>to</em> this flow state (destination wins).</summary>
    public enum FlowIncomingTransition
    {
        Instant = 0,
        FadeThroughBlack = 1,
        PrologueToTitleCrtFlipPulse = 2
    }

    /// <summary>
    /// Marker for the single-scene flow system.
    ///
    /// When <see cref="SceneFlowManager"/> detects these markers in the active Unity scene,
    /// it will toggle exactly one root active per <see cref="FlowState"/> instead of calling
    /// <c>SceneManager.LoadScene</c>.
    /// </summary>
    public class FlowCanvasRoot : MonoBehaviour
    {
        [Tooltip("Which flow state this root represents.")]
        public FlowState state;

        [Header("UI Canvas (shared root)")]
        [Tooltip("Default = use SceneFlowManager default color/sprite. Solid Color / Sprite override that shared Image for this screen.")]
        [SerializeField]
        private UiCanvasBackgroundMode uiCanvasBackground = UiCanvasBackgroundMode.Default;

        [Tooltip("Only used when mode is Solid Color (overrides SceneFlowManager default).")]
        [SerializeField]
        private Color solidBackgroundColor = Color.black;

        [SerializeField]
        private Sprite backgroundSprite;

        [SerializeField]
        private Color spriteTint = Color.white;

        [Header("Transition when entering this screen")]
        [Tooltip("Incoming transition when navigating to this state (destination wins).")]
        [SerializeField]
        private FlowIncomingTransition incomingTransition = FlowIncomingTransition.Instant;

        public UiCanvasBackgroundMode UiCanvasBackground => uiCanvasBackground;
        public Color SolidBackgroundColor => solidBackgroundColor;
        public Sprite BackgroundSprite => backgroundSprite;
        public Color SpriteTint => spriteTint;
        public FlowIncomingTransition IncomingTransition => incomingTransition;
    }
}
