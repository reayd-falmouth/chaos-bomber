using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// A destructible wall block in the 3D hybrid arena.
    /// 3D port of MasterBlaster's Destructible — replaces Rigidbody2D/Collider2D with BoxCollider.
    /// Destruction is networked via NetworkVariable so all clients stay in sync.
    /// After the destruction animation the server spawns a coloured ItemBlock3D at this position.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WallBlock3D : NetworkBehaviour
    {
        [Header("Destruction")]
        public float destructionAnimTime = 0.5f;
        public MultiFaceDestroySpriteAnimation destroyVfx;

        [Header("Health")]
        [Min(1f)]
        public float maxHealth = 30f;

        [Header("Item Drop")]
        [Range(0f, 1f)]
        public float itemSpawnChance = 0.2f;
        [Tooltip("Pool of item prefabs to randomly pick from. Each prefab has its itemType pre-set. " +
                 "Add or remove entries here to control which items can drop.")]
        public GameObject[] itemPrefabs;

        [Tooltip("Additional meters above the default drop height. Default drop height is GridOrigin.y + (0.5 × CellSize) " +
                 "so a unit cube pickup (center pivot) rests on the floor when CellSize is 1.")]
        public float itemSpawnYOffset = 0f;

        private NetworkVariable<bool> _isDestroyed = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<float> _health = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private BoxCollider _collider;
        private bool _destroyStarted;
        private int _pendingItemIdx = -1;

        private bool IsAuthority => !IsSpawned || IsServer;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
        }

        /// <summary>Used during Superman grid push so the CharacterController does not fight the block.</summary>
        public void SetPhysicsColliderEnabled(bool enabled)
        {
            if (_collider != null)
                _collider.enabled = enabled;
        }

        public override void OnNetworkSpawn()
        {
            _isDestroyed.OnValueChanged += OnDestroyedChanged;

            if (IsServer && !_isDestroyed.Value)
            {
                _health.Value = Mathf.Max(1f, maxHealth);
            }

            // Apply initial state for late-joining clients
            if (_isDestroyed.Value)
                ApplyDestroyed();
        }

        public override void OnNetworkDespawn()
        {
            _isDestroyed.OnValueChanged -= OnDestroyedChanged;
        }

        private void OnDestroyedChanged(bool prev, bool next)
        {
            if (next) ApplyDestroyed();
        }

        private void ApplyDestroyed()
        {
            if (_collider != null) _collider.enabled = false;

            // Hide the cube mesh but leave the destroy FX renderer alone
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // Play destruction sprite animation on all configured faces
            if (destroyVfx != null)
            {
                destroyVfx.gameObject.SetActive(true);
                destroyVfx.enabled = true;
                destroyVfx.Play();
            }

            if (!_destroyStarted)
            {
                _destroyStarted = true;
                Invoke(nameof(DestroyObject), destructionAnimTime);
            }
        }

        private void DestroyObject()
        {
            if (IsServer && TryGetComponent<NetworkObject>(out var no))
            {
                SpawnPendingItem(); // spawn before despawn so the GO is still alive
                no.Despawn(true);
            }
            else if (!IsServer)
            {
                SpawnPendingItem();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Arena layout thinning: remove this wall without item drops or destruction FX timers.
        /// Server-only when networked. Does not use <see cref="DestroyBlock"/>.
        /// </summary>
        public void RemoveForProceduralLayout()
        {
            if (_destroyStarted) return;

            if (IsSpawned)
            {
                if (!IsServer) return;
                _pendingItemIdx = -1;
                _destroyStarted = true;
                if (TryGetComponent<NetworkObject>(out var no))
                    no.Despawn(true);
                else
                    Destroy(gameObject);
                return;
            }

            _destroyStarted = true;
            Destroy(gameObject);
        }

        /// <summary>Called by BombController3D / explosions. Clients request the server to destroy networked walls.</summary>
        public void DestroyBlock()
        {
            if (_destroyStarted) return;

            if (IsSpawned && !IsServer)
            {
                RequestDestroyFromExplosionRpc();
                return;
            }

            DestroyBlockAuthoritative();
        }

        private void DestroyBlockAuthoritative()
        {
            if (!IsAuthority) return;

            if (IsSpawned)
            {
                _health.Value = 0f;
            }

            _pendingItemIdx = RollItemDrop();

            if (IsSpawned)
            {
                // Networked: NetworkVariable triggers OnDestroyedChanged on all clients
                _isDestroyed.Value = true;
            }
            else
            {
                // Singleplayer / unspawned: apply directly — don't touch NetworkVariable
                ApplyDestroyed();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestDestroyFromExplosionRpc()
        {
            DestroyBlockAuthoritative();
        }

        /// <summary>
        /// Server-authoritative damage entrypoint for FPS projectiles.
        /// Clients route to server; server applies damage and destroys at 0 health.
        /// </summary>
        public void ApplyProjectileDamage(float amount, GameObject damageSource = null)
        {
            if (_destroyStarted) return;
            if (amount <= 0f) return;

            if (IsSpawned && !IsServer)
            {
                ApplyProjectileDamageRpc(amount);
                return;
            }

            ApplyProjectileDamageAuthoritative(amount);
        }

        private void ApplyProjectileDamageAuthoritative(float amount)
        {
            if (!IsAuthority) return;
            if (_destroyStarted) return;

            if (IsSpawned)
            {
                if (_health.Value <= 0f)
                    _health.Value = Mathf.Max(1f, maxHealth);

                _health.Value = Mathf.Max(0f, _health.Value - amount);

                if (_health.Value <= 0f)
                    DestroyBlockAuthoritative();
            }
            else
            {
                // Fallback for unspawned/singleplayer use (kept for completeness)
                maxHealth = Mathf.Max(1f, maxHealth);
                _health.Value = Mathf.Max(0f, _health.Value - amount);
                if (_health.Value <= 0f)
                    DestroyBlockAuthoritative();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ApplyProjectileDamageRpc(float amount)
        {
            ApplyProjectileDamageAuthoritative(amount);
        }

        private int RollItemDrop()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0) return -1;
            if (Random.value >= itemSpawnChance) return -1;
            return Random.Range(0, itemPrefabs.Length);
        }

        public static float GetItemDropSpawnY(float extraYOffset = 0f)
        {
            return ArenaGrid3D.GridOrigin.y + (0.5f * ArenaGrid3D.CellSize) + extraYOffset;
        }

        private void SpawnPendingItem()
        {
            if (_pendingItemIdx < 0 || itemPrefabs == null || _pendingItemIdx >= itemPrefabs.Length) return;
            var prefab = itemPrefabs[_pendingItemIdx];
            if (prefab == null) return;

            Vector3 pos = ArenaGrid3D.SnapToCell(transform.position);
            pos.y = GetItemDropSpawnY(itemSpawnYOffset);

            Transform parent = null;
            if (HybridArenaGrid.Instance != null)
            {
                if (HybridArenaGrid.Instance.destructibleWallsParent != null)
                    parent = HybridArenaGrid.Instance.destructibleWallsParent;
                else
                    parent = HybridArenaGrid.Instance.transform;
            }

            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, 180f, 0f), parent);
            if (go.TryGetComponent<NetworkObject>(out var no) && IsSpawned && IsServer)
                no.Spawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsAuthority) return;
            if (other.gameObject.layer == LayerMask.NameToLayer("Explosion3D"))
                DestroyBlock();
        }
    }
}
