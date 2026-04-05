using HybridGame.MasterBlaster.Scripts.Levels;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    /// <summary>
    /// When in training mode, subscribes to the ML-Agents Academy environment reset.
    /// On reset (e.g. after max steps), reloads the Game scene so the next episode starts clean.
    /// Add this component to the Game scene (e.g. on an empty GameObject or GameManager).
    ///
    /// Runs at execution order -2000 so it sets PlayerPrefs before MapSelector (-500)
    /// and GameManager read them.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public class TrainingAcademyHelper : MonoBehaviour
    {
        [Header("Arena Settings")]
        [Tooltip("Which map layout to use during training.")]
        [SerializeField] private bool useNormalLevel = true;

        [Tooltip("Number of players in the arena (1–5). Player 1 is the RL agent; others are static dummies.")]
        [Range(1, 5)]
        [SerializeField] private int playerCount = 2;

        [Tooltip("Movement speed applied to all players. Default in-game value is 5.")]
        [SerializeField] private float playerSpeed = 5f;

        [Header("Bomb Settings")]
        [Tooltip("Whether players can place bombs at all.")]
        [SerializeField] private bool bombsEnabled = true;

        [Tooltip("Starting bomb capacity per player (ignored if Bombs Enabled is false).")]
        [SerializeField] private int bombAmount = 1;

        [Header("Episode Settings")]
        [Tooltip("Maximum steps before the episode is forced to end. 0 = unlimited.")]
        [SerializeField] private int maxStepsPerEpisode = 5000;

        [Tooltip("How many FixedUpdate ticks between agent decisions. Higher = smoother movement, lower = more reactive. 5 is a good starting point.")]
        [SerializeField] private int decisionPeriod = 5;

        [Header("Rewards")]
        [Tooltip("Reward given when the agent kills an opponent.")]
        [SerializeField] private float killReward = 1f;

        [Tooltip("Penalty applied when the agent dies.")]
        [SerializeField] private float deathPenalty = -1f;

        [Tooltip("Reward for placing a bomb adjacent to an opponent.")]
        [SerializeField] private float bombPlacementReward = 0.3f;

        [Tooltip("Scale applied to per-step distance-improvement reward. 0 = disabled.")]
        [SerializeField] private float approachRewardScale = 0.05f;

        [Header("Behaviour")]
        [Tooltip("Shuffle player spawn positions at the start of each episode. " +
                 "Prevents the agent overfitting to a fixed starting layout.")]
        [SerializeField] private bool randomizeSpawnPositions = true;

        [Tooltip("Steps the agent spends fleeing after placing a bomb (at the chosen decision period).")]
        [SerializeField] private int fleeStepsAfterBomb = 60;

        private void Awake()
        {
            if (!TrainingMode.IsActive) return;

            // Write settings to PlayerPrefs before MapSelector (-500) and GameManager read them
            PlayerPrefs.SetInt("NormalLevel", useNormalLevel ? 1 : 0);
            PlayerPrefs.SetInt(
                LevelSelectionPrefs.SelectedArenaIndexKey,
                LevelSelectionPrefs.ArenaIndexFromNormalLevel(useNormalLevel));
            PlayerPrefs.SetInt("Players", playerCount);
            PlayerPrefs.Save();
        }

        private void Start()
        {
            if (!TrainingMode.IsActive)
                return;

            // Push RL hyperparameters to every agent in the scene
            foreach (var agent in FindObjectsByType<BombermanAgent>(FindObjectsSortMode.None))
                agent.ApplyTrainingSettings(this);

            // Push arena settings to GameManager
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.SetRandomizeSpawnPositions(randomizeSpawnPositions);

            // Apply bomb settings to every player
            int effectiveBombAmount = bombsEnabled ? bombAmount : 0;
            foreach (var bc in FindObjectsByType<BombController>(FindObjectsSortMode.None))
                bc.SetBombAmount(effectiveBombAmount);

            // Apply speed to every player
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                pc.speed = playerSpeed;

            if (Academy.Instance != null)
                Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
        }

        private void OnDestroy()
        {
            if (Academy.IsInitialized)
                Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
        }

        private void OnEnvironmentReset()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ── Accessors ─────────────────────────────────────────────────────────
        public bool  UseNormalLevel      => useNormalLevel;
        public int   PlayerCount         => playerCount;
        public bool  BombsEnabled        => bombsEnabled;
        public int   BombAmount          => bombAmount;
        public int   MaxStepsPerEpisode  => maxStepsPerEpisode;
        public int   DecisionPeriod      => decisionPeriod;
        public float KillReward          => killReward;
        public float DeathPenalty        => deathPenalty;
        public float BombPlacementReward => bombPlacementReward;
        public float ApproachRewardScale => approachRewardScale;
        public int   FleeStepsAfterBomb  => fleeStepsAfterBomb;
    }
}
