using HybridGame.MasterBlaster.Scripts.Core;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI
{
    /// <summary>
    /// RL agent modelled closely on the ML-Agents PushAgentBasic example.
    ///
    /// ── Training goal ────────────────────────────────────────────────────────
    /// Learn to run into the nearest opponent. A reward fires the instant the
    /// agent physically touches them — exactly like PushAgentBasic fires a reward
    /// when the block touches the goal. This gives the network an immediate,
    /// unambiguous signal with no credit-assignment delay.
    ///
    /// Bombs are disabled at this stage. Once the agent reliably chases and
    /// contacts opponents, add the bomb layer back on top.
    ///
    /// ── Observations (7 floats) ──────────────────────────────────────────────
    ///   [0-1] Self position x, z          — normalized by ArenaScale
    ///   [2-3] Delta to nearest opponent   — normalized by ArenaScale
    ///   [4]   Distance to nearest opponent — normalized to [0,1]
    ///   [5-6] Self velocity x, z          — lets the network detect when stuck
    ///
    /// ── Actions (1 branch, 5 discrete choices) ───────────────────────────────
    ///   0 = stand still
    ///   1 = up  2 = down  3 = left  4 = right
    ///
    ///   Bombs removed — single-branch keeps the action space small and speeds
    ///   up early training (fewer choices = easier to explore).
    ///
    /// ── Rewards ──────────────────────────────────────────────────────────────
    ///   Every step : -1 / MaxStep            (time pressure, same as PushAgentBasic)
    ///   Every step : ±approach * scale        (shaped: reward closing distance)
    ///   Touch opponent : +KillReward          (terminal, fires on collision)
    ///   Self dies      : +DeathPenalty        (terminal, from PlayerController)
    /// </summary>
    public class BombermanAgent : Agent
    {
        private const float ArenaScale = 15f;

        // ── Output properties (read by MLAgentsBrain every FixedUpdate) ───────
        // MLAgentsBrain translates these into IPlayerInput so PlayerController
        // drives movement/animation as normal — the rest of the game is unaware.
        public Vector2 LastMove         { get; private set; }
        public bool    LastPlaceBomb    { get; private set; } = false; // always off
        public bool    LastDetonateHeld { get; private set; } = true;

        // ── Reward hyperparameters ────────────────────────────────────────────
        // Overwritten by TrainingAcademyHelper.ApplyTrainingSettings() at runtime.
        private float _killReward          = 1f;
        private float _deathPenalty        = -1f;
        private float _approachRewardScale = 0.05f;

        // ── Runtime state ─────────────────────────────────────────────────────
        private Rigidbody2D _rb;
        private GameManager _localGameManager;
        private float _prevDistToOpponent;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // Prefer a GameManager on the same prefab root (TrainingArena),
            // fall back to the scene singleton.
            var root = transform.root != transform ? transform.root : null;
            _localGameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                                ?? GameManager.Instance;

            SetupBehaviorParameters();
            base.Awake(); // must be called after BehaviorParameters are configured
        }

        /// <summary>
        /// Called by TrainingAcademyHelper.Start() to push inspector values onto this
        /// agent before the first episode begins.
        /// </summary>
        public void ApplyTrainingSettings(TrainingAcademyHelper s)
        {
            _killReward          = s.KillReward;
            _deathPenalty        = s.DeathPenalty;
            _approachRewardScale = s.ApproachRewardScale;

            if (s.MaxStepsPerEpisode > 0)
                MaxStep = s.MaxStepsPerEpisode;

            var dr = GetComponent<DecisionRequester>();
            if (dr != null) dr.DecisionPeriod = s.DecisionPeriod;
        }

        /// <summary>
        /// Configures BehaviorParameters and DecisionRequester so ML-Agents knows
        /// the observation size and action spec before the first episode.
        ///
        /// BehaviorName must match behavior_name in bomberman_config.yaml.
        /// VectorObservationSize must exactly match the float count in CollectObservations.
        /// ActionSpec.MakeDiscrete(5) = one branch with 5 choices (no bomb branch).
        /// </summary>
        private void SetupBehaviorParameters()
        {
            var bp = GetComponent<BehaviorParameters>() ?? gameObject.AddComponent<BehaviorParameters>();
            bp.BehaviorName = "BombermanAgent";
            bp.BrainParameters.VectorObservationSize = 7;
            bp.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(5);
            bp.BehaviorType = TrainingMode.IsActive ? BehaviorType.Default : BehaviorType.HeuristicOnly;

            if (TrainingMode.IsActive && MaxStep == 0)
                MaxStep = 5000;

            // DecisionRequester asks the network for an action every DecisionPeriod
            // FixedUpdate ticks. TakeActionsBetweenDecisions repeats the last action
            // on non-decision frames so movement stays smooth.
            if (GetComponent<DecisionRequester>() == null)
            {
                var dr = gameObject.AddComponent<DecisionRequester>();
                dr.DecisionPeriod = 5; // overridden by ApplyTrainingSettings
                dr.TakeActionsBetweenDecisions = true;
            }
        }

        // ── ML-Agents callbacks ───────────────────────────────────────────────

        /// <summary>
        /// Called at the start of every episode. Resets state and asks GameManager
        /// to restore the arena (tiles, player positions, velocity zeroed).
        /// </summary>
        public override void OnEpisodeBegin()
        {
            LastMove = Vector2.zero;
            _prevDistToOpponent = float.MaxValue;
            _localGameManager?.ResetArenaForTraining();
        }

        /// <summary>
        /// Fills the 7-float observation vector each decision step.
        /// All values normalized to roughly [-1, 1] so no single feature
        /// dominates the network's weight updates.
        /// </summary>
        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 myPos = transform.position;

            // [0,1] Where am I? (XY plane)
            sensor.AddObservation(myPos.x / ArenaScale);
            sensor.AddObservation(myPos.y / ArenaScale);

            // [2,3,4] Where is the nearest opponent relative to me?
            var opponent = FindNearestOpponent();
            if (opponent != null)
            {
                Vector2 delta = ArenaPlane.LogicalXY(opponent.transform.position) - ArenaPlane.LogicalXY(myPos);
                sensor.AddObservation(Mathf.Clamp(delta.x / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp(delta.y / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp01(delta.magnitude / (ArenaScale * 2f)));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(1f);
            }

            // [5,6] How fast am I moving? (XY)
            Vector2 vel = _rb != null ? _rb.linearVelocity : Vector2.zero;
            sensor.AddObservation(Mathf.Clamp(vel.x / ArenaScale, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(vel.y / ArenaScale, -1f, 1f));
        }

        /// <summary>
        /// Called every decision step. Translates the network's integer action into a
        /// direction vector, then applies rewards for this step.
        ///
        /// The actual movement is performed by PlayerController.FixedUpdate() reading
        /// LastMove via MLAgentsBrain → AIPlayerInput, keeping the rest of the game
        /// unaware that an RL agent is in control.
        /// </summary>
        public override void OnActionReceived(ActionBuffers actions)
        {
            // Single branch: 0=none 1=up 2=down 3=left 4=right
            LastMove = actions.DiscreteActions[0] switch
            {
                1 => Vector2.up,
                2 => Vector2.down,
                3 => Vector2.left,
                4 => Vector2.right,
                _ => Vector2.zero
            };

            // Time penalty — same as PushAgentBasic, encourages finishing quickly
            AddReward(-1f / MaxStep);

            // Approach shaping — small reward for closing distance each step.
            // Provides a learning gradient before any contacts happen, matching
            // how PushAgentBasic's step penalty alone drives exploration toward the goal.
            var opponent = FindNearestOpponent();
            if (opponent != null)
            {
                float dist = Vector2.Distance(
                    ArenaPlane.LogicalXY(transform.position),
                    ArenaPlane.LogicalXY(opponent.transform.position));
                if (_prevDistToOpponent < float.MaxValue)
                    AddReward((_prevDistToOpponent - dist) * _approachRewardScale);
                _prevDistToOpponent = dist;
            }
        }

        /// <summary>
        /// Fires when this player's collider touches another collider.
        /// If the collision is with an opponent player, reward fires immediately
        /// and the episode ends — directly equivalent to PushAgentBasic's
        /// ScoredAGoal() which fires when the block touches the goal collider.
        ///
        /// Immediate reward = no credit assignment problem.
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!TrainingMode.IsActive) return;

            var players = _localGameManager?.GetPlayers();
            if (players == null) return;

            foreach (var p in players)
            {
                if (p != null && p != gameObject && collision.gameObject == p)
                {
                    AddReward(_killReward);
                    EndEpisode();
                    return;
                }
            }
        }

        /// <summary>
        /// Heuristic drives the agent when BehaviorType is HeuristicOnly (normal gameplay).
        /// Simple rule: move toward the nearest opponent along the dominant axis.
        /// </summary>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var d = actionsOut.DiscreteActions;
            d[0] = 0;

            var opponent = FindNearestOpponent();
            if (opponent == null) return;

            Vector2 toOpponent = ArenaPlane.LogicalXY(opponent.transform.position)
                - ArenaPlane.LogicalXY(transform.position);

            if (Mathf.Abs(toOpponent.x) > Mathf.Abs(toOpponent.y))
                d[0] = toOpponent.x > 0 ? 4 : 3; // right : left
            else
                d[0] = toOpponent.y > 0 ? 1 : 2; // +Y : -Y
        }

        // ── Event callbacks ───────────────────────────────────────────────────

        /// <summary>
        /// Called by BombController when a bomb is placed. No-op at this training stage
        /// — bombs are disabled via TrainingAcademyHelper.
        /// </summary>
        public void NotifyPlacedBomb() { }

        /// <summary>
        /// Called by BombController when a destructible tile is destroyed. Reserved for
        /// a future training stage where map control is rewarded.
        /// </summary>
        public void NotifyDestroyedBlock() { }

        /// <summary>
        /// Called by PlayerController when this agent's player dies.
        /// Applies the death penalty and ends the episode.
        /// </summary>
        public void NotifyDeath()
        {
            AddReward(_deathPenalty);
            try { EndEpisode(); }
            catch (System.NullReferenceException) { }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the nearest active player that is not this agent.
        /// Uses sqrMagnitude to avoid a sqrt per candidate.
        /// </summary>
        private GameObject FindNearestOpponent()
        {
            var players = _localGameManager?.GetPlayers();
            if (players == null) return null;
            GameObject nearest = null;
            float best = float.MaxValue;
            foreach (var p in players)
            {
                if (p == null || !p.activeInHierarchy || p == gameObject) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = p; }
            }
            return nearest;
        }
    }
}
