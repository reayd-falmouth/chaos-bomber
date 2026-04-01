using System;
using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>How the shared "UI Canvas" root <see cref="UnityEngine.UI.Image"/> is painted while this flow screen is active.</summary>
    public enum UiCanvasBackgroundMode
    {
        /// <summary>Use <see cref="SceneFlowManager"/> default color/sprite. Solid Color / Sprite override that shared Image for this screen.</summary>
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
    /// it drives flow via <see cref="CanvasGroup"/> visibility and optional <see cref="managedBehaviours"/>
    /// instead of repeatedly calling <c>SetActive</c> on UI roots. Use <see cref="useGameObjectActivation"/>
    /// for the arena (<see cref="FlowState.Game"/>) so simulation stays off while in menus.
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
        private Color solidBackgroundColor = Color.white;

        [SerializeField]
        private Sprite backgroundSprite;

        [SerializeField]
        private Color spriteTint = Color.white;

        [Header("Transition when entering this screen")]
        [Tooltip("Incoming transition when navigating to this state (destination wins).")]
        [SerializeField]
        private FlowIncomingTransition incomingTransition = FlowIncomingTransition.Instant;

        [Header("Activation mode")]
        [Tooltip("If true, this root is shown/hidden with GameObject.SetActive (use for Game/arena). " +
                 "If false, SceneFlowManager uses CanvasGroup alpha + managed behaviours on the active state.")]
        [SerializeField]
        private bool useGameObjectActivation;

        [Tooltip("Controllers to enable only while this flow screen is active (CanvasGroup mode). " +
                 "If empty, same-GameObject MonoBehaviours whose type name ends with \"Controller\" are toggled.")]
        [SerializeField]
        private Behaviour[] managedBehaviours;

        private CanvasGroup _canvasGroup;

        public UiCanvasBackgroundMode UiCanvasBackground => uiCanvasBackground;
        public Color SolidBackgroundColor => solidBackgroundColor;
        public Sprite BackgroundSprite => backgroundSprite;
        public Color SpriteTint => spriteTint;
        public FlowIncomingTransition IncomingTransition => incomingTransition;

        public bool UseGameObjectActivation => useGameObjectActivation;

        /// <summary>Sets alpha, interactable, and blocksRaycasts for a flow screen.</summary>
        public static void ApplyCanvasGroupVisibility(CanvasGroup cg, bool visible)
        {
            if (cg == null)
                return;
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }

        /// <summary>CanvasGroup on this GameObject; created at runtime if missing.</summary>
        public CanvasGroup GetOrEnsureCanvasGroup()
        {
            if (_canvasGroup != null)
                return _canvasGroup;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            return _canvasGroup;
        }

        /// <summary>Behaviours toggled with flow when not using GameObject activation.</summary>
        public IReadOnlyList<Behaviour> GetManagedBehavioursForFlow()
        {
            if (managedBehaviours != null && managedBehaviours.Length > 0)
            {
                var list = new List<Behaviour>(managedBehaviours.Length);
                for (int i = 0; i < managedBehaviours.Length; i++)
                {
                    if (managedBehaviours[i] != null)
                        list.Add(managedBehaviours[i]);
                }
                return list;
            }

            return CollectAutoManagedBehaviours();
        }

        private static bool IsAutoManagedMonoBehaviour(MonoBehaviour mb)
        {
            if (mb == null || mb is FlowCanvasRoot)
                return false;
            var name = mb.GetType().Name;
            return name.EndsWith("Controller", StringComparison.Ordinal);
        }

        private List<Behaviour> CollectAutoManagedBehaviours()
        {
            var list = new List<Behaviour>();
            var mbs = GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++)
            {
                var mb = mbs[i];
                if (IsAutoManagedMonoBehaviour(mb))
                    list.Add(mb);
            }
            return list;
        }

        /// <summary>Enables or disables managed behaviours (no-op for GameObject activation mode).</summary>
        public void SetManagedBehavioursEnabled(bool enabled)
        {
            if (useGameObjectActivation)
                return;
            var list = GetManagedBehavioursForFlow();
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b != null)
                    b.enabled = enabled;
            }
        }
    }
}
