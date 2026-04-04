using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    public enum UiCanvasBackdropOverrideMode
    {
        None = 0,
        CopyFromImage = 1,
        ColorOnly = 2,
        SpriteAndColor = 3
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

        [Header("UI Canvas backdrop override")]
        [Tooltip("Optional override for the shared full-screen UI Canvas backdrop image.")]
        [SerializeField] private UiCanvasBackdropOverrideMode uiCanvasBackdropOverrideMode = UiCanvasBackdropOverrideMode.None;

        [Tooltip("Used by CopyFromImage mode. Sprite and color will be copied from this Image.")]
        [SerializeField] private Image uiCanvasBackdropOverrideSourceImage;

        [Tooltip("Used by SpriteAndColor mode. Clear this to intentionally set the backdrop sprite to 'None'.")]
        [SerializeField] private Sprite uiCanvasBackdropOverrideSprite;

        [Tooltip("Used by ColorOnly and SpriteAndColor modes.")]
        [SerializeField] private Color uiCanvasBackdropOverrideColor = Color.black;

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
                 "If empty, MonoBehaviours on this root and children whose type name ends with \"Controller\" are toggled " +
                 "(excluding nested FlowCanvasRoot subtrees).")]
        [SerializeField]
        private Behaviour[] managedBehaviours;

        private CanvasGroup _canvasGroup;

        public FlowIncomingTransition IncomingTransition => incomingTransition;

        public bool UseGameObjectActivation => useGameObjectActivation;

        /// <summary>
        /// Applies an override to the shared full-screen UI Canvas backdrop Image.
        /// </summary>
        /// <remarks>
        /// The <paramref name="defaultSprite"/>/<paramref name="defaultColor"/> values are captured from the scene-authored
        /// backdrop at load time so Flow can reliably restore them when leaving an overriding screen.
        /// </remarks>
        public void ApplyUiCanvasBackdropOverride(Image sharedBackdrop, Sprite defaultSprite, Color defaultColor)
        {
            if (sharedBackdrop == null)
                return;

            switch (uiCanvasBackdropOverrideMode)
            {
                case UiCanvasBackdropOverrideMode.None:
                    sharedBackdrop.sprite = defaultSprite;
                    sharedBackdrop.color = defaultColor;
                    break;

                case UiCanvasBackdropOverrideMode.ColorOnly:
                    sharedBackdrop.sprite = defaultSprite;
                    sharedBackdrop.color = uiCanvasBackdropOverrideColor;
                    break;

                case UiCanvasBackdropOverrideMode.SpriteAndColor:
                    // Always apply the override sprite even if it's intentionally cleared to null (sprite "None").
                    sharedBackdrop.sprite = uiCanvasBackdropOverrideSprite;
                    sharedBackdrop.color = uiCanvasBackdropOverrideColor;
                    break;

                case UiCanvasBackdropOverrideMode.CopyFromImage:
                    if (uiCanvasBackdropOverrideSourceImage != null)
                    {
                        sharedBackdrop.sprite = uiCanvasBackdropOverrideSourceImage.sprite;
                        sharedBackdrop.color = uiCanvasBackdropOverrideSourceImage.color;
                    }
                    else
                    {
                        sharedBackdrop.sprite = defaultSprite;
                        sharedBackdrop.color = defaultColor;
                    }
                    break;
            }
        }

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
            var mbs = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < mbs.Length; i++)
            {
                var mb = mbs[i];
                if (!IsAutoManagedMonoBehaviour(mb))
                    continue;
                // Another flow root under this subtree manages its own behaviours.
                if (mb.gameObject != gameObject && mb.gameObject.GetComponent<FlowCanvasRoot>() != null)
                    continue;
                list.Add(mb);
            }
            return list;
        }

        /// <summary>
        /// Disables managed behaviours on every <see cref="FlowCanvasRoot"/> in the loaded scene(s), before Awake/OnEnable.
        /// See <see cref="FlowScreenLifecycleBootstrap"/>.
        /// </summary>
        public static void DisableAllManagedBehavioursForInitialSceneLoad()
        {
            var roots = UnityEngine.Object.FindObjectsByType<FlowCanvasRoot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                    roots[i].SetManagedBehavioursEnabled(false);
            }
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
                if (b == null)
                    continue;

                if (!enabled)
                {
                    if (!b.enabled)
                        continue;
                    if (b is IFlowScreen fs)
                        fs.OnFlowDismissed();
                    b.enabled = false;
                }
                else
                {
                    if (b.enabled)
                        continue;
                    b.enabled = true;
                    if (b is IFlowScreen fs2)
                        fs2.OnFlowPresented();
                }
            }
        }

        /// <summary>
        /// Stops and clears every <see cref="ParticleSystem"/> under this flow root so VFX do not keep
        /// simulating or drawing when another <see cref="FlowState"/> is active (CanvasGroup alpha alone does not stop world/sim particles).
        /// Skips particle systems under a nested <see cref="FlowCanvasRoot"/> subtree.
        /// </summary>
        public void StopParticleSystemsWhenFlowHidden()
        {
            if (useGameObjectActivation)
                return;

            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null)
                    continue;
                if (BelongsToNestedFlowRootSubtree(ps.transform))
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// True if <paramref name="t"/> is under another <see cref="FlowCanvasRoot"/> before reaching this root
        /// (that nested screen manages its own particles).
        /// </summary>
        private bool BelongsToNestedFlowRootSubtree(Transform t)
        {
            for (var walk = t; walk != null; walk = walk.parent)
            {
                if (walk == transform)
                    return false;
                var nested = walk.GetComponent<FlowCanvasRoot>();
                if (nested != null && nested != this)
                    return true;
            }

            return false;
        }
    }
}
