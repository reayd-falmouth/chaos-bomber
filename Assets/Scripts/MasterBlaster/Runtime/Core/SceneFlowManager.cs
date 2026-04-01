using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using HybridGame.MasterBlaster.Scripts.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    public enum FlowState
    {
        Controls,
        Title,
        Credits,
        Menu,
        Countdown,
        Game,
        Standings,
        Wheel,
        Shop,
        Overs,
        // IMPORTANT: Append-only to avoid breaking serialized enum ints in scenes/prefabs.
        Prologue,
        Quote
    }

    public class SceneFlowManager : PersistentSingleton<SceneFlowManager>
    {
        public static SceneFlowManager I => Instance;

        /// <summary>
        /// True when this scene contains <see cref="FlowCanvasRoot"/> markers and we should
        /// toggle state roots instead of loading additional Unity scenes.
        /// </summary>
        public bool IsSingleSceneMode => _singleSceneMode;

        [Header("Scene Names")]
        [SerializeField]
        string controlsScene = "Controls";
        
        [SerializeField]
        string titleScene = "Title";
        
        [SerializeField]
        string creditsScene = "Credits";

        [SerializeField]
        string prologueScene = "Prologue";

        [SerializeField]
        string menuScene = "Menu";

        [SerializeField]
        string countdownScene = "Countdown";

        [SerializeField]
        string gameScene = "Game";

        [SerializeField]
        string standingsScene = "Standings";

        [SerializeField]
        string wheelScene = "Wheel";

        [SerializeField]
        string shopScene = "Shop";

        [SerializeField]
        string oversScene = "Overs";

        FlowState state;

        [Header("Start State Override")]
        [Tooltip("When enabled, single-scene mode boots into overrideStartState instead of auto-detecting the active root. " +
                 "Use in hybrid scenes (e.g. MasterBlaster_FPS) to skip the menu flow and start directly in Game.")]
        [SerializeField] private bool overrideStartState = false;
        [SerializeField] private FlowState overrideStartStateValue = FlowState.Game;

        // -------- Single-scene mode --------
        private bool _singleSceneMode;
        private FlowCanvasRoot[] _roots;
        private readonly Dictionary<FlowState, FlowCanvasRoot> _rootByState = new();
        private Coroutine _transitionRoutine;
        private CanvasGroup _transitionOverlay;

        /// <summary>
        /// Current flow state (used by ContinueOnAnyInput to decide if any key should advance).
        /// </summary>
        public FlowState CurrentState => state;

        /// <summary>
        /// True for screens where "continue on any input" should advance (Credits, Title, Standings, etc.).
        /// Menu and other screens must use their own UI (e.g. Return to start).
        /// </summary>
        public static bool ShouldAdvanceOnAnyInput(FlowState state)
        {
            return state == FlowState.Credits
                || state == FlowState.Overs
                || state == FlowState.Controls
                || state == FlowState.Standings
                || state == FlowState.Quote
                || state == FlowState.Prologue;
        }

        void Start()
        {
            _singleSceneMode = TryInitSingleSceneRoots();

            if (_singleSceneMode)
            {
                state = (overrideStartState) ? overrideStartStateValue : GetActiveRootStateOrDefault();

                // Enforce "exactly one root active" even if the scene author forgot.
                ActivateStateRoot(state);

                // In single-scene mode, AudioController is not driven by sceneLoaded routing.
                AudioController.I?.PreviewSceneMusic(SceneFor(state));

                string availableStates;
                if (_rootByState != null && _rootByState.Count > 0)
                    availableStates = string.Join(",", _rootByState.Keys);
                else
                    availableStates = "<none>";
                UnityEngine.Debug.Log(
                    $"[Flow] Single-scene roots: {availableStates}; activeState={state}"
                );
                UnityEngine.Debug.Log($"[Flow] Booted in single-scene mode → {state}");
                return;
            }

            // Legacy: infer state from the active scene name.
            var sceneName = SceneManager.GetActiveScene().name;
            state = StateForSceneName(sceneName);
            UnityEngine.Debug.Log($"[Flow] Booted in '{sceneName}' → {state}");
        }

        private bool TryInitSingleSceneRoots()
        {
            _roots = FindObjectsByType<FlowCanvasRoot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            if (_roots == null || _roots.Length == 0)
            {
                // No designer-added markers: auto-wire them by object name so the
                // toggle-based flow still works with existing scenes.
                TryAutoAttachFlowCanvasRootsByName();

                _roots = FindObjectsByType<FlowCanvasRoot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
            }

            if (_roots == null || _roots.Length == 0)
                return false;

            _rootByState.Clear();
            for (int i = 0; i < _roots.Length; i++)
            {
                var r = _roots[i];
                if (r == null) continue;
                if (_rootByState.ContainsKey(r.state))
                {
                    UnityEngine.Debug.LogWarning($"[Flow] Duplicate FlowCanvasRoot for state '{r.state}'. Using first.");
                    continue;
                }
                _rootByState[r.state] = r;
            }

            return _rootByState.Count > 0;
        }

        private void TryAutoAttachFlowCanvasRootsByName()
        {
            // If the scene already has markers, do nothing.
            if (
                FindObjectsByType<FlowCanvasRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Length
                > 0
            )
                return;

            // 1) Robust wiring from existing controller components (avoids name collisions).
            TryAutoAttachFromControllers();

            // 2) Name-based fallback for states without controllers (Credits/Title) and
            // for any controller-root we failed to attach (e.g., Menu root not found).
            TryAutoAttachFromNameMatchingForMissingStates();
        }

        private void TryAutoAttachFromControllers()
        {
            TryAttachRootFromFirstController<Scenes.MainMenu.MainMenuController>(FlowState.Menu);
            TryAttachRootFromFirstController<Scenes.Arena.CountdownController>(FlowState.Countdown);
            TryAttachRootFromFirstController<Scenes.Arena.GameManager>(FlowState.Game);
            TryAttachRootFromFirstController<Scenes.Standings.StandingsController>(FlowState.Standings);
            TryAttachRootFromFirstController<Scenes.WheelOFortune.WheelController>(FlowState.Wheel);
            TryAttachRootFromFirstController<Scenes.Shop.ShopController>(FlowState.Shop);
            TryAttachRootFromFirstController<Scenes.GameOver.WinnerController>(FlowState.Overs);
        }

        private void TryAutoAttachFromNameMatchingForMissingStates()
        {
            // If controllers successfully attached everything we care about, skip.
            var existing = FindObjectsByType<FlowCanvasRoot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            var has = new HashSet<FlowState>();
            if (existing != null)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    var r = existing[i];
                    if (r != null) has.Add(r.state);
                }
            }

            // Map of candidate root GameObjects by name (including inactive).
            var byName = new Dictionary<string, GameObject>(System.StringComparer.Ordinal);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                CollectByName(root.transform, byName);

            // Attach markers for missing states, preferring Canvas objects when possible.
            foreach (FlowState s in System.Enum.GetValues(typeof(FlowState)))
            {
                if (has.Contains(s))
                    continue;

                var candidates = CandidateRootNamesFor(s);

                // Canvas preference: if a Canvas GameObject matches by name, attach marker to it.
                var canvases = FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
                bool attachedByCanvas = false;
                for (int i = 0; i < canvases.Length && !attachedByCanvas; i++)
                {
                    var canvas = canvases[i];
                    if (canvas == null) continue;

                    foreach (var candidate in candidates)
                    {
                        if (canvas.gameObject.name == candidate)
                        {
                            var toggleRoot = ChooseToggleRoot(canvas.gameObject, s);
                            if (toggleRoot == null)
                                toggleRoot = canvas.gameObject;

                            var marker = toggleRoot.GetComponent<FlowCanvasRoot>();
                            if (marker == null)
                                marker = toggleRoot.AddComponent<FlowCanvasRoot>();
                            marker.state = s;
                            attachedByCanvas = true;
                            break;
                        }
                    }
                }

                if (attachedByCanvas)
                    continue;

                // Fallback to name matching: attach marker to the matched object,
                // but choose a unique toggle root (not a shared Canvas).
                GameObject found = null;
                foreach (var candidate in candidates)
                {
                    if (byName.TryGetValue(candidate, out found))
                        break;
                }

                if (found == null)
                    continue;

                var chosenRoot = ChooseToggleRoot(found, s);
                var marker2 = chosenRoot.GetComponent<FlowCanvasRoot>();
                if (marker2 == null)
                    marker2 = chosenRoot.AddComponent<FlowCanvasRoot>();
                marker2.state = s;
            }
        }

        private void TryAttachRootFromFirstController<TController>(FlowState state)
            where TController : Component
        {
            var controller = FindObjectsByType<TController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            if (controller == null || controller.Length == 0)
                return;

            var comp = controller[0];
            if (comp == null) return;

            var root = ChooseToggleRoot(comp.gameObject, state);
            if (root == null) return;

            var marker = root.GetComponent<FlowCanvasRoot>();
            if (marker == null)
                marker = root.AddComponent<FlowCanvasRoot>();
            marker.state = state;
        }

        private GameObject ChooseToggleRoot(GameObject from, FlowState desiredState)
        {
            // The key for the single-scene toggle system is to pick a *unique* root per state.
            // In many Unity setups, there is a shared Canvas that contains multiple panels.
            // Toggling the shared Canvas (or overwriting its FlowCanvasRoot marker) breaks
            // state activation. So we explicitly avoid choosing Canvas as the toggle root.
            //
            // We also prefer the *highest* eligible ancestor below the shared Canvas.
            // Otherwise, if the named object is a nested child, enabling just that child
            // may still leave it under an inactive parent (so nothing appears).
            GameObject best = null;
            for (var t = from.transform; t != null; t = t.parent)
            {
                var go = t.gameObject;

                // Stop at the first Canvas boundary: anything above it could be shared/system-wide.
                if (go.GetComponent<Canvas>() != null)
                    break;

                var existingMarker = go.GetComponent<FlowCanvasRoot>();
                if (existingMarker != null)
                {
                    // If already marked for the correct state, reuse it.
                    if (existingMarker.state == desiredState)
                        return go;

                    // Otherwise keep searching above so we don't overwrite marker.state.
                    continue;
                }

                best = go;

                // Key heuristic: choose the direct child of the Canvas as the toggle root.
                // This prevents nested roots (parent deactivates child) which would make
                // the target screen appear missing even though state changes.
                if (t.parent != null && t.parent.GetComponent<Canvas>() != null)
                {
                    // Nested screen canvas (e.g. Menu prefab): Canvas under another Canvas.
                    // Toggle the inner Canvas GameObject so SetActive affects the whole panel
                    // (scene often keeps that root inactive while a child controller is the
                    // "logical" root — activating only the child leaves the panel invisible).
                    var parentCanvasGo = t.parent.gameObject;
                    if (
                        t.parent.parent != null
                        && t.parent.parent.GetComponent<Canvas>() != null
                    )
                        return parentCanvasGo;

                    return go;
                }
            }

            return best ?? from;
        }

        private void CollectByName(Transform t, Dictionary<string, GameObject> byName)
        {
            if (t == null) return;
            if (!byName.ContainsKey(t.name))
                byName[t.name] = t.gameObject;

            for (int i = 0; i < t.childCount; i++)
                CollectByName(t.GetChild(i), byName);
        }

        private IEnumerable<string> CandidateRootNamesFor(FlowState s)
        {
            // Use the same serialized scene-name mapping as the legacy system, then
            // add known typos present in the project assets.
            var names = new List<string> { SceneFor(s) };
            if (s == FlowState.Controls)
                names.Add("controls"); // common lowercase scene authoring
            if (s == FlowState.Title)
                names.Add("TItle"); // typo found in separate Title scene
            if (s == FlowState.Standings)
                names.Add("Stangings"); // typo found in separate Standings scene

            return names;
        }

        private FlowState GetActiveRootStateOrDefault()
        {
            // Prefer an explicitly active root in the hierarchy.
            for (int i = 0; i < _roots.Length; i++)
            {
                var r = _roots[i];
                if (r == null) continue;
                if (r.gameObject.activeInHierarchy)
                    return r.state;
            }

            // If nothing is active, default to Quote (the intended entry screen).
            UnityEngine.Debug.LogWarning("[Flow] Single-scene mode: no active FlowCanvasRoot found. Defaulting to Quote.");
            return FlowState.Quote;
        }

        private bool ActivateStateRoot(FlowState next)
        {
            if (!_singleSceneMode || _roots == null || _roots.Length == 0)
                return false;
            if (!_rootByState.TryGetValue(next, out var target) || target == null)
                return false;

            UnityEngine.Debug.Log(
                $"[Flow] Activating root for {next}: {target.gameObject.name} (before activeSelf={target.gameObject.activeSelf} activeInHierarchy={target.gameObject.activeInHierarchy})"
            );

            for (int i = 0; i < _roots.Length; i++)
            {
                var r = _roots[i];
                if (r == null) continue;
                if (r.gameObject.activeSelf)
                    r.gameObject.SetActive(false);
            }

            target.gameObject.SetActive(true);
            UnityEngine.Debug.Log(
                $"[Flow] Root for {next} is now activeSelf={target.gameObject.activeSelf} activeInHierarchy={target.gameObject.activeInHierarchy}"
            );
            return true;
        }

        private void RefreshSingleSceneRoots()
        {
            _roots = FindObjectsByType<FlowCanvasRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _rootByState.Clear();
            for (int i = 0; i < _roots.Length; i++)
            {
                var r = _roots[i];
                if (r == null) continue;
                if (!_rootByState.ContainsKey(r.state))
                    _rootByState[r.state] = r;
            }
        }

        /// <summary>
        /// Returns the next flow state when the current scene signals "done". Uses PlayerPrefs for Gambling/Shop.
        /// Used by SignalScreenDone and by EditMode tests. Countdown always precedes Game (arena).
        /// </summary>
        public static FlowState GetNextState(FlowState currentState)
        {
            switch (currentState)
            {
                case FlowState.Controls:
                    return FlowState.Title;
                case FlowState.Quote:
                    return FlowState.Prologue;
                case FlowState.Prologue:
                    return FlowState.Title;
                case FlowState.Title:
                    // Title is now a menu; do not auto-advance from "press any key".
                    return FlowState.Title;
                case FlowState.Credits:
                    // Credits are only reachable from Title and should return there.
                    return FlowState.Title;
                case FlowState.Menu:
                    return FlowState.Countdown;
                case FlowState.Countdown:
                    return FlowState.Game;
                case FlowState.Standings:
                    if (PlayerPrefs.GetInt("Gambling", 1) == 1)
                        return FlowState.Wheel;
                    if (PlayerPrefs.GetInt("Shop", 1) == 1)
                        return FlowState.Shop;
                    return FlowState.Countdown;
                case FlowState.Wheel:
                    if (PlayerPrefs.GetInt("Shop", 1) == 1)
                        return FlowState.Shop;
                    return FlowState.Countdown;
                case FlowState.Shop:
                    return FlowState.Countdown;
                case FlowState.Overs:
                    return FlowState.Menu;
                default:
                    return currentState;
            }
        }

        // -------- Public signals from scenes --------
        public void SignalScreenDone()
        {
            GoTo(GetNextState(state));
        }

        public void SignalMenuStart()
        {
            GoTo(FlowState.Countdown);
        }

        public void SignalRoundFinished()
        {
            GoTo(FlowState.Standings);
        }

        // -------- Core --------
        public void GoTo(FlowState next)
        {
            var previous = state;
            UnityEngine.Debug.Log($"[Flow] {previous} → {next}");
            state = next;
            string sceneName = SceneFor(next);
            AudioController.I?.PreviewSceneMusic(sceneName);

            // Single-scene system: toggle root instead of loading new scenes.
            if (_singleSceneMode)
            {
                if (ShouldTransition(previous, next))
                {
                    if (_transitionRoutine != null)
                        StopCoroutine(_transitionRoutine);
                    _transitionRoutine = StartCoroutine(TransitionFadeThroughBlack(previous, next, 3f));
                    return;
                }

                // Any flow screen other than the arena: clear loose world pickups/explosions/debris.
                // (1) Orphan drops parented to scene root could still render over UI after Game is disabled.
                // (2) Standings → Wheel/Shop does not pass "previous == Game", so we must clear on every
                //     non-Game transition, not only when leaving Game directly.
                if (next != FlowState.Game)
                    GameManager.Instance?.ClearMatchTransientObjects();

                if (!ActivateStateRoot(next))
                {
                    // Cache may be stale — refresh roots from scene and retry once.
                    RefreshSingleSceneRoots();
                    if (!ActivateStateRoot(next))
                    {
                        string availableStates = _rootByState != null && _rootByState.Count > 0
                            ? string.Join(",", _rootByState.Keys)
                            : "<none>";
                        UnityEngine.Debug.LogWarning(
                            $"[Flow] Single-scene mode: no root found for state '{next}'. Available roots: {availableStates}. Falling back to scene load '{sceneName}'."
                        );
                        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                        return;
                    }
                }
                return;
            }

            // Legacy: load next scene.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private static bool ShouldTransition(FlowState previous, FlowState next)
        {
            return previous == FlowState.Quote && next == FlowState.Prologue;
        }

        private CanvasGroup GetOrCreateTransitionOverlay()
        {
            if (_transitionOverlay != null)
                return _transitionOverlay;

            var uiCanvas = GameObject.Find("UI Canvas");
            var parent = uiCanvas != null ? uiCanvas.transform : null;
            if (parent == null)
            {
                var anyCanvas = FindAnyObjectByType<Canvas>();
                parent = anyCanvas != null ? anyCanvas.transform : null;
            }

            var go = new GameObject("FlowTransitionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            if (parent != null)
                go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            _transitionOverlay = go.GetComponent<CanvasGroup>();
            _transitionOverlay.alpha = 0f;

            rt.SetAsLastSibling();
            return _transitionOverlay;
        }

        private IEnumerator TransitionFadeThroughBlack(FlowState previous, FlowState next, float durationSeconds)
        {
            var overlay = GetOrCreateTransitionOverlay();
            if (overlay == null)
            {
                // Fallback if we couldn't create the overlay
                ActivateStateRoot(next);
                yield break;
            }

            var rt = overlay.GetComponent<RectTransform>();
            if (rt != null) rt.SetAsLastSibling();

            float half = Mathf.Max(0.01f, durationSeconds * 0.5f);

            // Fade to black
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                overlay.alpha = Mathf.Clamp01(t / half);
                yield return null;
            }
            overlay.alpha = 1f;

            // Switch state root while fully black
            ActivateStateRoot(next);

            // Fade back in
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                overlay.alpha = 1f - Mathf.Clamp01(t / half);
                yield return null;
            }
            overlay.alpha = 0f;

            _transitionRoutine = null;
        }

        SceneNamesConfig GetConfig() =>
            new SceneNamesConfig
            {
                Controls = controlsScene,
                Credits = creditsScene,
                Prologue = prologueScene,
                Title = titleScene,
                Menu = menuScene,
                Countdown = countdownScene,
                Game = gameScene,
                Standings = standingsScene,
                Wheel = wheelScene,
                Shop = shopScene,
                Overs = oversScene
            };

        string SceneFor(FlowState s) => SceneFlowMapper.SceneFor(s, GetConfig());

        public FlowState StateForSceneName(string n) =>
            SceneFlowMapper.StateForSceneName(n, GetConfig());

        public void GoToOvers()
        {
            GoTo(FlowState.Overs);
        }
    }
}
