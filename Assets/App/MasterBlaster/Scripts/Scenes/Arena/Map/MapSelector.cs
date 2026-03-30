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
            bool useNormal = PlayerPrefs.GetInt("NormalLevel", 1) == 1; // default: normal on
            Apply(useNormal);
        }

        /// <summary>Call this if you toggle NormalLevel at runtime.</summary>
        public void RefreshFromPrefs() => Apply(PlayerPrefs.GetInt("NormalLevel", 1) == 1);

        public void Apply(bool useNormal)
        {
            var enableGO = useNormal ? normalRoot : altRoot;
            var disableGO = useNormal ? altRoot : normalRoot;

            // Stop and hide the inactive map/shrinker
            if (disableGO != null && disableGO.activeSelf)
            {
                // var offShrinker = disableGO.GetComponentInChildren<ArenaShrinker>(true);
                // offShrinker?.StopTimerAndAlarm();   // guarantees siren stops
                disableGO.SetActive(false);
            }

            // Show and prep the active map/shrinker
            if (enableGO != null && !enableGO.activeSelf)
                enableGO.SetActive(true);

            var onShrinker = enableGO ? enableGO.GetComponentInChildren<ArenaShrinker>(true) : null;

            // If the shrinker finds its own tilemaps in Awake, nothing else needed.
            // If you assign tilemaps manually in the inspector, they're already scoped to each root.

            if (startTimerOnApply)
                onShrinker?.StartTimer();
        }
    }
}
