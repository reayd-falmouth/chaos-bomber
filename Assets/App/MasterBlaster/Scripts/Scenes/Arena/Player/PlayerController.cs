using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.Abilities;
using HybridGame.MasterBlaster.Scripts.Scenes.Shop;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        // NetworkVariables so clients can animate from server-authoritative direction.
        private NetworkVariable<Vector2> _netDirection = new NetworkVariable<Vector2>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> _netStop = new NetworkVariable<bool>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public event System.Func<bool> OnExplosionHit;
        /// <summary>Logical move on XZ: (x, z) stored in Vector2.x / Vector2.y.</summary>
        public Vector2 Direction => direction;

        public enum PlayerVisualState
        {
            Normal,
            Death,
            Remote
        }

        private PlayerVisualState visualState = PlayerVisualState.Normal;

        private Rigidbody2D rb;
        private Vector2 direction = Vector2.down;

        [Header("Player Info")]
        public int playerId;
        public int wins = 0;
        public float speed = 5f;
        public int coins = 0;
        public bool stop;

        private RemoteBombController pushingBomb;
        private IPlayerInput _inputProvider;
        private GameManager _localGameManager;

        [Header("Input")]
        public KeyCode inputUp = KeyCode.W;
        public KeyCode inputDown = KeyCode.S;
        public KeyCode inputLeft = KeyCode.A;
        public KeyCode inputRight = KeyCode.D;

        [Header("Sprites")]
        public AnimatedSpriteRenderer spriteRendererUp;
        public AnimatedSpriteRenderer spriteRendererDown;
        public AnimatedSpriteRenderer spriteRendererLeft;
        public AnimatedSpriteRenderer spriteRendererRight;
        public AnimatedSpriteRenderer spriteRendererDeath;
        public AnimatedSpriteRenderer spriteRendererRemoteBomb;
        private AnimatedSpriteRenderer activeSpriteRenderer;

        [HideInInspector]
        public bool visualOverrideActive;

        [HideInInspector]
        public AnimatedSpriteRenderer visualOverrideRenderer;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player deathFeedbacks;
        [SerializeField] private MMF_Player randomItemFeedbacks;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            activeSpriteRenderer = spriteRendererDown;

            // Prefer a GameManager in the same arena hierarchy; fall back to the global Instance
            // for single-arena scenes where the player is a root-level GameObject.
            var root = transform.root != transform ? transform.root : null;
            _localGameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                                ?? GameManager.Instance;
        }

        private void Start()
        {
            if (playerId <= 0)
            {
                UnityEngine.Debug.LogWarning(
                    $"[PlayerController] {gameObject.name} has no playerId assigned, defaulting to 1"
                );
                playerId = 1;
            }

            _inputProvider = GetComponent<IPlayerInput>();
            UnityEngine.Debug.Log($"[PlayerController] {gameObject.name} Start: inputProvider={_inputProvider?.GetType().Name ?? "NULL"}");
            ApplyUpgrades();
        }

        // Fix 2: gate hot-path Debug.Log behind this flag (default off for training)
        [SerializeField] private bool verboseLogging = false;

        private float _lastPCLogTime = -999f;

        private void Update()
        {
            // When controlling a remote bomb, ignore movement input entirely.
            if (visualState == PlayerVisualState.Remote)
                return;

            // Pause / MMTimeScale freeze: ignore gameplay movement so UI can use the same actions.
            if (Time.timeScale <= 0f)
                return;

            // Lazy-resolve: also re-resolve if the interface reference points to a destroyed Unity component.
            // (C# null check alone misses destroyed MonoBehaviours held via an interface reference.)
            if (_inputProvider == null || (_inputProvider is UnityEngine.Object uo && uo == null))
                _inputProvider = GetComponent<IPlayerInput>();

            Vector2 move = _inputProvider != null ? _inputProvider.GetMoveDirection() : GetLegacyMove();

            if (verboseLogging && Time.time - _lastPCLogTime >= 2f)
            {
                _lastPCLogTime = Time.time;
                UnityEngine.Debug.Log($"[PlayerController] {gameObject.name} Update: provider={_inputProvider?.GetType().Name ?? "NULL"} move={move} stop={stop} enabled={enabled}");
            }

            if (move.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                {
                    if (move.x > 0)
                        SetDirection(Vector2.right, spriteRendererRight);
                    else
                        SetDirection(Vector2.left, spriteRendererLeft);
                }
                else
                {
                    if (move.y > 0)
                        SetDirection(Vector2.up, spriteRendererUp);
                    else
                        SetDirection(Vector2.down, spriteRendererDown);
                }
            }
            else
            {
                SetDirection(Vector2.zero, activeSpriteRenderer);
            }
        }

        private Vector2 GetLegacyMove()
        {
            if (Input.GetKey(inputUp)) return Vector2.up;
            if (Input.GetKey(inputDown)) return Vector2.down;
            if (Input.GetKey(inputLeft)) return Vector2.left;
            if (Input.GetKey(inputRight)) return Vector2.right;
            return Vector2.zero;
        }

        private void FixedUpdate()
        {
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

            if (isOnline)
            {
                // Clients animate from the replicated direction; only host moves the rigidbody.
                if (!IsServer) return;

                // Push state to clients via NetworkVariables.
                _netDirection.Value = direction;
                _netStop.Value = stop;
            }

            if (rb == null)
                return;

            Vector2 position = rb.position;
            if (!stop)
            {
                Vector2 translation = ArenaPlane.MoveDeltaXY(direction, speed, Time.fixedDeltaTime);
                rb.MovePosition(position + translation);
            }
        }

        private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
        {
            direction = newDirection;
            activeSpriteRenderer = spriteRenderer;
            UpdateVisualState();
        }

        /// <summary>Apply explosion damage: run OnExplosionHit handlers; if none block, run death. Call from trigger or from BombController.</summary>
        public void TryApplyExplosionDamage()
        {
            if (OnExplosionHit != null)
            {
                foreach (System.Func<bool> handler in OnExplosionHit.GetInvocationList())
                {
                    if (handler())
                        return; // blocked by ability
                }
            }
            DeathSequence();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
                TryApplyExplosionDamage();
        }

        private void DeathSequence()
        {
            enabled = false;
            GetComponent<BombController>().enabled = false;

            var rlAgent = GetComponent<Scenes.Arena.Player.AI.BombermanAgent>();
            if (rlAgent != null)
            {
                if (TrainingMode.IsActive)
                {
                    // Skip the 1.25s animation entirely: EndEpisode() calls OnEpisodeBegin()
                    // synchronously, which resets the arena before DeathSequence() can overwrite it.
                    rlAgent.NotifyDeath();
                    return;
                }
                rlAgent.NotifyDeath();
            }

            // Broadcast death animation to all clients when online.
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && IsServer)
                PlayDeathClientRpc(playerId);
            else
                PlayDeathLocally();
        }

        [ClientRpc]
        private void PlayDeathClientRpc(int pid)
        {
            PlayDeathLocally();
        }

        private void PlayDeathLocally()
        {
            visualState = PlayerVisualState.Death;
            UpdateVisualState();
            deathFeedbacks?.PlayFeedbacks(transform.position);
            Invoke(nameof(OnDeathSequenceEnded), 1.25f);
        }

        private void OnDeathSequenceEnded()
        {
            gameObject.SetActive(false);
            _localGameManager?.CheckWinState();
        }

        /// <summary>
        /// Called by GameManager to reset this player for a new training episode.
        /// Cancels any in-flight death sequence, restores visual state, and re-enables components.
        /// </summary>
        public void ResetForEpisode()
        {
            CancelInvoke(nameof(OnDeathSequenceEnded));
            stop = false;
            visualState = PlayerVisualState.Normal;
            UpdateVisualState();
        }

        // public void ApplyUpgrades()
        // {
        //     // Coins
        //     coins = PlayerPrefs.GetInt($"Player{playerId}_Coins", 0);
        //
        //     // Speed boost (stackable)
        //     int speedBoost = PlayerPrefs.GetInt($"Player{playerId}_{ShopItemType.SpeedUp}", 0);
        //     if (speedBoost > 0) {
        //         speed += 2 * speedBoost;
        //         Debug.Log($"[PlayerController] Player {playerId} speed upgraded by {2 * speedBoost}, total speed: {speed}");
        //     }
        //
        //     Debug.Log($"[PlayerController] Player {playerId} upgrades applied.");
        // }

        // Inside PlayerController.cs

        public void ApplyUpgrades()
        {
            // No valid ID yet (e.g. OnEnable fired before GameManager.EnablePlayer assigned one); skip until we have one
            if (playerId <= 0)
                return;
            // SessionManager may not exist in this scene (e.g. Game before Shop); treat as no upgrades
            if (SessionManager.Instance == null)
                return;

            // Reset player stats that are affected by stackable upgrades (like speed, bomb stats)
            // NOTE: You'll need to reset other stats here if they are affected by stackable upgrades.
            // For simplicity, we assume 'speed' starts at 5f (as defined in the header)
            // and this function is called on start/enable.
            speed = 5f;

            // Coins (session-only, from SessionManager)
            coins =
                SessionManager.Instance != null ? SessionManager.Instance.GetCoins(playerId) : 0;

            // ---------------------------------------------------------------------------------
            // 🔁 STACKABLE UPGRADES (PowerUp, ExtraBomb, SpeedUp)
            // ---------------------------------------------------------------------------------

            // Speed boost (stackable) - Multiplies base speed
            int speedBoost = SessionManager.Instance.GetUpgradeLevel(
                playerId,
                ShopItemType.SpeedUp
            );
            if (speedBoost > 0)
            {
                // Based on existing code: speed is upgraded by 2 units per stack
                speed += 2 * speedBoost;
                UnityEngine.Debug.Log(
                    $"[PlayerController] Player {playerId} speed upgraded by {2 * speedBoost}, total speed: {speed}"
                );
            }

            // Extra Bomb (stackable) - Increases max bomb count
            int extraBombCount = SessionManager.Instance.GetUpgradeLevel(
                playerId,
                ShopItemType.ExtraBomb
            );
            if (extraBombCount > 0)
            {
                // You will need to access and modify the BombController's bomb limit here.
                // Example: GetComponent<BombController>().AddBombLimit(extraBombCount);
                UnityEngine.Debug.Log(
                    $"[PlayerController] Player {playerId} received {extraBombCount} extra bombs."
                );
            }

            // Power Up (stackable) - Increases bomb range/power
            int powerUpCount = SessionManager.Instance.GetUpgradeLevel(
                playerId,
                ShopItemType.PowerUp
            );
            if (powerUpCount > 0)
            {
                // You will need to access and modify the BombController's explosion size/power here.
                // Example: GetComponent<BombController>().AddPower(powerUpCount);
                UnityEngine.Debug.Log(
                    $"[PlayerController] Player {playerId} received {powerUpCount} bomb power upgrades."
                );
            }

            // ---------------------------------------------------------------------------------
            // ✅ TOGGLE UPGRADES (Superman, Ghost, Protection, Controller, Timebomb)
            // ---------------------------------------------------------------------------------

            // Superman (Toggle) - Allows walking through walls
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Superman) == 1)
            {
                // You need to add or enable a component/ability script that handles wall-passing.
                // Example: GetComponent<SupermanAbility>().Activate();
                UnityEngine.Debug.Log($"[PlayerController] Player {playerId} is Superman.");
            }

            // Ghost (Toggle) - Allows walking through bombs
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Ghost) == 1)
            {
                // You need to add or enable a component/ability script that disables collision with bombs.
                // Example: GetComponent<GhostAbility>().Activate();
                UnityEngine.Debug.Log($"[PlayerController] Player {playerId} is a Ghost.");
            }

            // Protection (Toggle) - Protects against one death
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Protection) == 1)
            {
                // You need to add or enable a component/ability script that listens to OnExplosionHit
                // and returns 'true' once to block the death sequence.
                // Example: GetComponent<ProtectionAbility>().Activate();
                UnityEngine.Debug.Log($"[PlayerController] Player {playerId} has Protection.");
            }

            // Controller (Toggle) - Allows remote detonation of bombs
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Controller) == 1)
            {
                // You need to enable the remote bomb functionality in the BombController.
                // This is separate from the remote *visual* state.
                // Example: GetComponent<BombController>().EnableRemoteControl();
                UnityEngine.Debug.Log($"[PlayerController] Player {playerId} has a Remote Controller.");
            }

            // Timebomb (Toggle) - Allows setting bomb fuse time
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Timebomb) == 1)
            {
                // You need to enable the ability to set the time in the BombController.
                // Example: GetComponent<BombController>().EnableTimebombFeature();
                UnityEngine.Debug.Log($"[PlayerController] Player {playerId} has Timebombs.");
            }

            UnityEngine.Debug.Log($"[PlayerController] Player {playerId} upgrades applied.");

            // Arena items mutate ability components directly; SessionManager only stores shop purchases.
            // Re-sync so arena-granted Superman/Ghost/Protection do not carry into the next round.
            SyncAbilityComponentsFromSession();
        }

        /// <summary>
        /// Reapply toggle abilities from <see cref="SessionManager"/> only (arena pickups cleared first).
        /// </summary>
        public void SyncAbilityComponentsFromSession()
        {
            if (playerId <= 0)
                return;

            // Each ability handles missing SessionManager (clear arena state, no shop reapply).
            GetComponentInChildren<Superman>(true)?.SyncFromSession();
            GetComponentInChildren<Ghost>(true)?.SyncFromSession();
            GetComponentInChildren<Protection>(true)?.SyncFromSession();
        }

        public void ActivateStop(float duration = 10f)
        {
            StartCoroutine(StopRoutine(duration));
        }

        private IEnumerator StopRoutine(float duration)
        {
            stop = true;
            yield return new WaitForSeconds(duration);
            stop = false;
        }

        public void ApplyRandom()
        {
            ItemPickup.ItemType randomType;
            do
            {
                randomType = (ItemPickup.ItemType)
                    Random.Range(0, System.Enum.GetValues(typeof(ItemPickup.ItemType)).Length);
            } while (randomType == ItemPickup.ItemType.Random);

            randomItemFeedbacks?.PlayFeedbacks(transform.position);

            ItemPickup.ApplyItem(this.gameObject, randomType);
        }

        public void IncreaseSpeed()
        {
            speed++;
            UnityEngine.Debug.Log($"[PlayerController] Player {playerId} speed increased to {speed}");
        }

        public void AddCoin()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.AddCoins(playerId, 1);
            coins =
                SessionManager.Instance != null
                    ? SessionManager.Instance.GetCoins(playerId)
                    : coins + 1;
            UnityEngine.Debug.Log($"[PlayerController] Player {playerId} coins increased to {coins}");
        }

        public void ApplyDeath()
        {
            DeathSequence();
        }

        public void UpdateVisualState()
        {
            // Disable all first
            spriteRendererUp.enabled = false;
            spriteRendererDown.enabled = false;
            spriteRendererLeft.enabled = false;
            spriteRendererRight.enabled = false;
            spriteRendererDeath.enabled = false;
            spriteRendererRemoteBomb.enabled = false;

            // 🔹 If an override is active, only render that
            if (visualOverrideActive && visualOverrideRenderer != null)
            {
                visualOverrideRenderer.enabled = true;
                return;
            }

            // Otherwise use normal visual state
            switch (visualState)
            {
                case PlayerVisualState.Normal:
                    activeSpriteRenderer.enabled = true;
                    activeSpriteRenderer.idle = direction == Vector2.zero;
                    break;

                case PlayerVisualState.Death:
                    spriteRendererDeath.enabled = true;
                    break;

                case PlayerVisualState.Remote:
                    spriteRendererRemoteBomb.enabled = true;
                    break;
            }
        }

        public void SetRemoteBombVisual(bool active)
        {
            if (active)
            {
                // enter remote mode: force idle + show remote pose
                direction = Vector2.zero;
                visualState = PlayerVisualState.Remote;
                UpdateVisualState();
            }
            else
            {
                // exit remote mode: back to normal, re-evaluate current renderer
                visualState = PlayerVisualState.Normal;
                UpdateVisualState();
            }
        }

        // Inside PlayerController.cs
        public void SetVisualState(PlayerVisualState state)
        {
            visualState = state;
            UpdateVisualState();
        }

        private void OnEnable()
        {
            ApplyUpgrades();
        }
    }
}
