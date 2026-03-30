using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    /// <summary>
    /// Input provider driven by an AI brain. Ticks the brain each frame and exposes move/bomb/detonate for the rest of the player.
    /// </summary>
    public class AIPlayerInput : MonoBehaviour, IPlayerInput
    {
        private IAIBrain _brain;
        private BombController _bombController;
        private Vector2 _lastMove;
        private bool _pendingBombDown;
        private bool _detonateHeld = true;
        // Fix 2: gate hot-path Debug.Log behind this flag (default off for training)
        [SerializeField] private bool verboseLogging = false;
        private float _lastLogTime = -999f;
        private int _lastTickFrame = -1;
        private GameManager _localGameManager;

        private void Awake()
        {
            var root = transform.root != transform ? transform.root : null;
            _localGameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                                ?? GameManager.Instance;
        }

        public void Init(IAIBrain brain)
        {
            _brain = brain;
            _bombController = GetComponent<BombController>();
        }

        private void EnsureTicked()
        {
            if (Time.frameCount == _lastTickFrame) return;
            _lastTickFrame = Time.frameCount;
            if (_brain == null) return;
            GameObject[] allPlayers = _localGameManager?.GetPlayers();
            _brain.Tick(transform, _bombController, allPlayers,
                out _lastMove, out bool placeBomb, out bool detonateHeld);
            if (placeBomb) _pendingBombDown = true;
            _detonateHeld = detonateHeld;
            if (verboseLogging && Time.time - _lastLogTime >= 1.5f)
            {
                _lastLogTime = Time.time;
                UnityEngine.Debug.Log($"[AIPlayerInput] {gameObject.name} move={_lastMove} (zero={_lastMove.sqrMagnitude < 0.01f})");
            }
        }

        public Vector2 GetMoveDirection() { EnsureTicked(); return _lastMove; }
        public bool GetBombDown()         { EnsureTicked(); if (!_pendingBombDown) return false; _pendingBombDown = false; return true; }
        public bool GetDetonateHeld()     { EnsureTicked(); return _detonateHeld; }
    }
}
