using System.Collections;
using HybridGame.MasterBlaster.Scripts.Player;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player.Abilities
{
    /// <summary>
    /// Ghost ability — lets the player walk through destructible walls and bombs.
    /// 3D port of MasterBlaster's Ghost.cs:
    ///   Rigidbody2D.excludeLayers → CharacterController.excludeLayers
    ///   SessionManager removed (not yet ported — always treats as arena pickup).
    /// </summary>
    [DisallowMultipleComponent]
    public class Ghost : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float defaultDuration = 15f;
        [SerializeField] private AnimatedSpriteRenderer spriteRendererGhost;

        private CharacterController _cc;
        private PlayerDualModeController _dual;
        private bool _active;
        private float _timer;
        private Coroutine _endCo;

        // Layer masks for DestructibleWall and Bomb3D
        private int _ghostExcludeLayers;

        private void EnsureInitialized()
        {
            _cc ??= GetComponentInParent<CharacterController>();
            _dual ??= GetComponentInParent<PlayerDualModeController>();

            if (_ghostExcludeLayers == 0)
                _ghostExcludeLayers = LayerMask.GetMask("DestructibleWall", "Bomb3D");
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            // Ghost can live on an inactive child (e.g. FPS Player prefab). When it is activated later
            // (or its methods are invoked directly), we still need it to initialize safely.
            EnsureInitialized();
        }

        private void Update()
        {
            if (!_active) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f && _endCo == null)
                _endCo = StartCoroutine(EndRoutine());
        }

        private void OnDisable()
        {
            if (_active) ForceDeactivate();
        }

        public void Activate(float duration = -1f)
        {
            EnsureInitialized();
            _active = true;
            _timer = duration > 0f ? duration : defaultDuration;

            if (_cc != null)
                _cc.excludeLayers |= _ghostExcludeLayers;

            if (spriteRendererGhost != null)
            {
                spriteRendererGhost.gameObject.SetActive(true);
                spriteRendererGhost.idle = false;
                spriteRendererGhost.StartAnimation();
            }

            _dual?.SetGhostVisualActive(true);
        }

        private void ClearGhostState()
        {
            EnsureInitialized();
            if (_cc != null)
                _cc.excludeLayers &= ~_ghostExcludeLayers;

            if (spriteRendererGhost != null)
            {
                spriteRendererGhost.StopAnimation();
                spriteRendererGhost.gameObject.SetActive(false);
            }

            _dual?.SetGhostVisualActive(false);
        }

        private void ForceDeactivate()
        {
            if (_endCo != null)
            {
                StopCoroutine(_endCo);
                _endCo = null;
            }

            _active = false;
            _timer = 0f;
            ClearGhostState();
        }

        private IEnumerator EndRoutine()
        {
            _active = false;
            ClearGhostState();
            yield return null;
            _endCo = null;
        }

        /// <summary>Immediate end (death, mode change, etc.). Safe when already inactive.</summary>
        public void DeactivateNow() => ForceDeactivate();

        public bool IsActive => _active;
    }
}
