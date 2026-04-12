using HybridGame.MasterBlaster.Scripts.Debug;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    // Ensure this runs before ArenaShrinkers' Start()
    [DefaultExecutionOrder(-500)]
    public class MapSelector : MonoBehaviour
    {
        [Header("Map roots (children of Arena)")]
        [SerializeField]
        private GameObject normalRoot; // e.g. Arena/Normal

        [SerializeField]
        private GameObject altRoot; // e.g. Arena/Alt

        [Header("Timer control")]
        [Tooltip("If true, selector will start the chosen shrinker's timer after switching.")]
        [SerializeField]
        private bool startTimerOnApply = true;

        void Awake()
        {
            if (SelectedLevelLoader.SuppressDefaultMapSelector)
                return;

            bool useNormal = PlayerPrefs.GetInt("NormalLevel", 1) == 1; // default: normal on
            Apply(useNormal);
        }

        /// <summary>Call this if you toggle NormalLevel at runtime.</summary>
        public void RefreshFromPrefs() => Apply(PlayerPrefs.GetInt("NormalLevel", 1) == 1);

        /// <summary>Disables normal and alt map roots (used when a dynamically selected level prefab is active).</summary>
        public void DisableBothRoots()
        {
            if (normalRoot != null)
                normalRoot.SetActive(false);
            if (altRoot != null)
                altRoot.SetActive(false);
        }

        public void Apply(bool useNormal)
        {
            var enableGO = useNormal ? normalRoot : altRoot;
            var disableGO = useNormal ? altRoot : normalRoot;

            // Stop and hide the inactive map/shrinker
            if (disableGO != null && disableGO.activeSelf)
            {
                foreach (var s in disableGO.GetComponentsInChildren<ArenaShrinker>(true))
                    s.ResetMatchStateForNewRound();
                foreach (var f in disableGO.GetComponentsInChildren<FpsArenaShrinker>(true))
                    f.ResetMatchStateForNewRound();
                disableGO.SetActive(false);
            }

            // Show and prep the active map/shrinker
            if (enableGO != null && !enableGO.activeSelf)
                enableGO.SetActive(true);

            var onShrinker = enableGO ? enableGO.GetComponentInChildren<ArenaShrinker>(true) : null;
            var onFpsShrinker = enableGO ? enableGO.GetComponentInChildren<FpsArenaShrinker>(true) : null;

            // If the shrinker finds its own tilemaps in Awake, nothing else needed.
            // If you assign tilemaps manually in the inspector, they're already scoped to each root.

            // #region agent log
            AgentDebugNdjson_624424.Log(
                "H1",
                "MapSelector.Apply",
                "shrinker_resolve",
                "{\"hasArenaShrinker\":" + (onShrinker != null ? "true" : "false") +
                ",\"hasFpsShrinker\":" + (onFpsShrinker != null ? "true" : "false") +
                ",\"startTimerOnApply\":" + (startTimerOnApply ? "true" : "false") +
                ",\"useNormal\":" + (useNormal ? "true" : "false") + "}",
                "pre"
            );
            // #endregion

            if (startTimerOnApply)
            {
                onShrinker?.StartTimer();
                onFpsShrinker?.StartTimer();
                // #region agent log
                AgentDebugNdjson_624424.Log(
                    "H1b",
                    "MapSelector.Apply",
                    "after_StartTimer_both",
                    "{\"calledFpsStartTimer\":" + (onFpsShrinker != null ? "true" : "false") + "}",
                    "post-fix"
                );
                // #endregion
            }
        }
    }
}
