using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI
{
    /// <summary>
    /// Adapts BombermanAgent (ML-Agents) to IAIBrain so AIPlayerInput can use a trained policy.
    /// Decisions are driven by the agent's DecisionRequester (Academy step cycle); we only read LastMove here.
    /// </summary>
    [RequireComponent(typeof(BombermanAgent))]
    public class MLAgentsBrain : MonoBehaviour, IAIBrain
    {
        private BombermanAgent _agent;
        // Fix 2: gate hot-path Debug.Log behind this flag (default off for training)
        [SerializeField] private bool verboseLogging = false;
        private float _lastLogTime = -999f;

        private void Awake()
        {
            _agent = GetComponent<BombermanAgent>();
        }

        public void Tick(
            Transform self,
            BombController bombController,
            GameObject[] allPlayers,
            out Vector2 move,
            out bool placeBomb,
            out bool detonateHeld
        )
        {
            // Re-resolve in case the cached reference was to the old (Destroy-marked) component.
            if (_agent == null)
                _agent = GetComponent<BombermanAgent>();

            if (_agent == null)
            {
                move = Vector2.zero;
                placeBomb = false;
                detonateHeld = true;
                return;
            }

            move = _agent.LastMove;
            placeBomb = _agent.LastPlaceBomb;
            detonateHeld = _agent.LastDetonateHeld;

            if (verboseLogging && Time.time - _lastLogTime >= 2f)
            {
                _lastLogTime = Time.time;
                UnityEngine.Debug.Log($"[MLAgentsBrain] {gameObject.name} → LastMove={move} (zero={move.sqrMagnitude < 0.01f})");
            }
        }
    }
}
