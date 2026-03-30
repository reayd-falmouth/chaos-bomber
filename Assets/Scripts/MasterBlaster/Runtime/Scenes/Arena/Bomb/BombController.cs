using System.Collections;
using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb
{
    public class BombController : NetworkBehaviour
    {
        [Header("Bomb")]
        public KeyCode inputKey = KeyCode.LeftShift;
        public GameObject bombPrefab;
        public float bombFuseTime = 3f;
        public int bombAmount = 1;
        private int bombsRemaining;
        public bool timeBomb = false;
        public bool remoteBomb = false;

        [Header("Explosion")]
        public Explosion explosionPrefab;
        public LayerMask explosionLayerMask;
        public float explosionDuration = 0.5f;
        public int explosionRadius = 1;
        public float explosionDelay = 0.05f;

        [Header("Destructible")]
        public Tilemap destructibleTiles;
        public Destructible destructiblePrefab;

        [Header("Indestructible (auto-detected)")]
        [HideInInspector] public Tilemap indestructibleTiles;

        // Fix 8: HashSet gives O(1) Add/Remove/Contains vs O(n) List.Remove
        private readonly HashSet<GameObject> activeBombs = new HashSet<GameObject>();

        // Fix 5: pre-allocated overlap buffers — two separate buffers so ExplodeBomb center-overlap
        // doesn't overwrite the buffer mid-iteration in ExplodeCoroutine during chain reactions
        private static readonly Collider2D[] _explodeBuffer = new Collider2D[16];
        private static readonly Collider2D[] _centerBuffer  = new Collider2D[16];
        private static ContactFilter2D _overlapFilter;

        private static readonly Vector2 ExplosionProbeSize = new Vector2(0.5f, 0.5f);

        private int baseBombAmount;
        private int baseExplosionRadius;
        private Player.IPlayerInput _inputProvider;

        // Fix 6: bool flag avoids per-frame destroyed-object cast (is UnityEngine.Object uo && uo == null)
        private bool _inputProviderValid;

        // Fix 7: cached component references — set once in Awake, avoids GetComponent per bomb-place/destroy
        private PlayerController _playerController;
        private Scenes.Arena.Player.AI.BombermanAgent _agentNotify;

        // Non-null when the player sits under a shared arena root (multi-arena training).
        // Bombs and explosions are parented here so BombermanAgent scoped searches work correctly.
        private Transform _arenaRoot;

        // Always-active MonoBehaviour used to host coroutines. The player GameObject may be
        // inactive when a bomb detonates (player died before fuse expired), so we must not call
        // StartCoroutine on 'this' in that case. The arena's GameManager is always active.
        private MonoBehaviour _coroutineRunner;

        private void Awake()
        {
            baseBombAmount = bombAmount;
            baseExplosionRadius = explosionRadius;

            // Parent bombs/explosions under the Arena (MapSelector), not transform.root (Game prefab root).
            // Otherwise bombs appear as siblings of Arena and sorting/hierarchy is wrong.
            var mapSelector = GetComponentInParent<MapSelector>(true);
            _arenaRoot = mapSelector != null
                ? mapSelector.transform
                : (transform.root != transform ? transform.root : null);

            // Cache an always-active coroutine runner.
            var gm = (_arenaRoot != null
                ? _arenaRoot.GetComponentInChildren<GameManager>()
                : null) ?? GameManager.Instance;
            _coroutineRunner = (gm as MonoBehaviour) ?? this;

            // Fix 7: cache both references once
            _playerController = GetComponent<PlayerController>();
            _agentNotify      = GetComponent<Scenes.Arena.Player.AI.BombermanAgent>();

            // Auto-detect tilemaps from the active map.
            // MapSelector (ExecutionOrder -500) runs before this (order 0), so FindObjectsByType
            // only finds tilemaps on the currently-active map root — correct for both normal and alt levels.
            var allTilemaps = _arenaRoot != null
                ? _arenaRoot.GetComponentsInChildren<Tilemap>()
                : FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

            if (!indestructibleTiles)
            {
                foreach (var t in allTilemaps)
                    if (t.name == "Indestructibles") { indestructibleTiles = t; break; }
            }

            // If the inspector-assigned destructibleTiles is on an inactive GameObject (e.g. the normal
            // map root was disabled by MapSelector for the alt level), find the active one instead.
            if (destructibleTiles == null || !destructibleTiles.gameObject.activeInHierarchy)
            {
                foreach (var t in allTilemaps)
                    if (t.name == "Destructibles") { destructibleTiles = t; break; }
            }
        }

        private void EnsureArenaRootCached()
        {
            if (_arenaRoot != null)
                return;
            var mapSelector = GetComponentInParent<MapSelector>(true);
            _arenaRoot = mapSelector != null
                ? mapSelector.transform
                : (transform.root != transform ? transform.root : null);
        }

        /// <summary>
        /// Snap world position to the active arena tilemap cell center (XZ plane, Y from tilemap).
        /// </summary>
        private Vector3 SnapWorldPositionToTileGrid(Vector3 world)
        {
            Tilemap tm = null;
            if (destructibleTiles != null && destructibleTiles.gameObject.activeInHierarchy)
                tm = destructibleTiles;
            else if (indestructibleTiles != null && indestructibleTiles.gameObject.activeInHierarchy)
                tm = indestructibleTiles;
            else
                tm = destructibleTiles != null ? destructibleTiles : indestructibleTiles;

            return ArenaPlane.SnapWorldToTileGrid(tm, world);
        }

        private void Start()
        {
            // Fix 6: set valid flag so Update skips GetComponent in steady state
            _inputProvider = GetComponent<Player.IPlayerInput>();
            _inputProviderValid = _inputProvider != null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            // Re-resolve if null or if the reference points to a destroyed component
            // (mirrors PlayerController.Update; needed when AttachInputProvider Destroy+re-adds AIPlayerInput)
            if (!_inputProviderValid || (_inputProvider is UnityEngine.Object uoInput && uoInput == null))
            {
                _inputProvider = GetComponent<Player.IPlayerInput>();
                _inputProviderValid = _inputProvider != null;
            }

            if (bombsRemaining <= 0)
                return;

            // In online play, only the host places bombs.
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && !IsServer)
                return;

            bool wantBomb = _inputProvider != null ? _inputProvider.GetBombDown() : Input.GetKeyDown(inputKey);
            if (wantBomb)
            {
                GameObject bomb = SpawnBomb();
                if (bomb == null)
                    return;

                var brain = bomb.GetComponent<RemoteBombController>();

                var mode = remoteBomb
                    ? RemoteBombController.BombMode.Remote
                    : (
                        timeBomb
                            ? RemoteBombController.BombMode.Time
                            : RemoteBombController.BombMode.Fuse
                    );

                // Fix 7: use cached _playerController instead of GetComponent per bomb-place
                brain.Init(_playerController, this, mode, inputKey, bombFuseTime);
            }
        }

        /// <summary>
        /// Spawns a bomb at the player's grid cell and returns it.
        /// Handles destructible blocking and duplicate checks.
        /// </summary>
        private GameObject SpawnBomb()
        {
            EnsureArenaRootCached();

            Vector3 position = SnapWorldPositionToTileGrid(transform.position);

            // Skip tile-occupancy check when there's no destructible tilemap (e.g. empty training arena).
            if (destructibleTiles != null)
            {
                Vector3Int cell = destructibleTiles.WorldToCell(position);
                if (destructibleTiles.GetTile(cell) != null)
                    return null;
            }

            // Fix 8: HashSet enumeration still works; position check is now O(1) per entry
            foreach (var b in activeBombs)
            {
                if (b == null) continue;
                var bp = b.transform.position;
                if ((bp.x - position.x) * (bp.x - position.x) + (bp.y - position.y) * (bp.y - position.y) < 0.0001f)
                    return null;
            }

            GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity, _arenaRoot);
            var info = bomb.AddComponent<BombInfo>();
            info.explosionRadius = explosionRadius;
            activeBombs.Add(bomb);
            bombsRemaining--;

            // Fix 7: use cached _agentNotify instead of GetComponent per bomb-place
            _agentNotify?.NotifyPlacedBomb();

            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && IsServer)
                SpawnBombClientRpc(position, explosionRadius);

            return bomb;
        }

        public void Explode(
            Vector3 position,
            Vector3 direction,
            int length,
            float delayStep = 0.05f
        )
        {
            EnsureArenaRootCached();
            _coroutineRunner.StartCoroutine(ExplodeCoroutine(position, direction, length, delayStep));
        }

        private IEnumerator ExplodeCoroutine(
            Vector3 position,
            Vector3 direction,
            int length,
            float delayStep
        )
        {
            UnityEngine.Debug.Log($"[BombController] ExplodeCoroutine STARTED dir={direction} length={length} pos={position}");
            for (int i = 1; i <= length; i++)
            {
                Vector3 nextPos = position + direction * i;

                // Fix 5: NonAlloc reuses _explodeBuffer; separate buffer from _centerBuffer so
                // ExplodeBomb (called synchronously below) doesn't overwrite our iteration buffer
                int layerMask = explosionLayerMask.value != 0 ? explosionLayerMask.value : ~0;
                _overlapFilter = ContactFilter2D.noFilter;
                _overlapFilter.SetLayerMask(layerMask);
                int count = Physics2D.OverlapBox(
                    (Vector2)nextPos,
                    ExplosionProbeSize,
                    0f,
                    _overlapFilter,
                    _explodeBuffer);

                UnityEngine.Debug.Log($"[BombController] ExplodeCoroutine dir={direction} i={i} nextPos={nextPos} count={count} layerMask={layerMask}");
                for (int dbg = 0; dbg < count; dbg++)
                    if (_explodeBuffer[dbg] != null)
                        UnityEngine.Debug.Log($"  hit: {_explodeBuffer[dbg].gameObject.name} layer={LayerMask.LayerToName(_explodeBuffer[dbg].gameObject.layer)}");

                bool blocked = false;
                bool spawnFireOnBlock = false;

                for (int j = 0; j < count; j++)
                {
                    var hit = _explodeBuffer[j];
                    if (hit == null)
                        continue;

                    // Player in explosion cell – apply damage, explosion keeps going
                    var pc = hit.GetComponent<PlayerController>();
                    if (pc != null && pc.enabled)
                    {
                        pc.TryApplyExplosionDamage();
                        continue;
                    }

                    // Pushable destructible prefab
                    var destructible = hit.GetComponent<Destructible>();
                    if (destructible != null)
                    {
                        destructible.DestroyBlock();
                        spawnFireOnBlock = false;
                        blocked = true; // stop propagation
                        break;
                    }

                    // Item
                    var item = hit.GetComponent<ItemPickup>();
                    if (item != null)
                    {
                        Destroy(item.gameObject);
                        continue; // explosion keeps going
                    }

                    // Bomb chain reaction (⚡ doesn't block)
                    if (hit.gameObject.layer == LayerMask.NameToLayer("Bomb"))
                    {
                        ExplodeBomb(hit.gameObject);
                        continue;
                    }

                    // Tilemap destructible tile — clear it and show fire; indestructible wall — block only
                    if (((1 << hit.gameObject.layer) & explosionLayerMask) != 0)
                    {
                        // CHANGE THIS WHOLE BLOCK: Clear the tile if it's destructible, but never spawn fire
                        if (LayerMask.LayerToName(hit.gameObject.layer) == "Destructible")
                        {
                            ClearDestructible(nextPos);
                        }
    
                        spawnFireOnBlock = false; 
                        blocked = true; // stop propagation
                        break;
                    }
                }

                // stop propagation; show fire at the blocked cell if it was a destructible
                if (blocked)
                {
                    if (spawnFireOnBlock)
                    {
                        Explosion blockExplosion = Instantiate(explosionPrefab, nextPos, Quaternion.identity, _arenaRoot);
                        // Visual only — disable trigger so it doesn't destroy freshly-dropped items
                        // or interact with the debris spawned by ClearDestructible at this cell.
                        var blockCol = blockExplosion.GetComponent<Collider2D>();
                        if (blockCol != null) blockCol.enabled = false;
                        blockExplosion.SetDirection(new Vector2(direction.x, direction.y));
                        blockExplosion.DestroyAfter(explosionDuration);
                    }
                    yield break;
                }

                // Spawn explosion fire
                Explosion explosion = Instantiate(explosionPrefab, nextPos, Quaternion.identity, _arenaRoot);
                explosion.SetDirection(new Vector2(direction.x, direction.y));
                explosion.DestroyAfter(explosionDuration);

                yield return new WaitForSeconds(delayStep);
            }
        }

        public void ExplodeBomb(GameObject bomb)
        {
            if (bomb == null)
                return;

            UnityEngine.Debug.Log($"[BombController] ExplodeBomb called, explosionRadius={explosionRadius}");
            EnsureArenaRootCached();

            Vector3 position = SnapWorldPositionToTileGrid(bomb.transform.position);

            // Fix 8: HashSet.Remove is O(1)
            activeBombs.Remove(bomb);
            Destroy(bomb);
            bombsRemaining++;

            // Center fire – play explosion sound on this instance only
            Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity, _arenaRoot);
            explosion.PlayExplosionSound();
            explosion.DestroyAfter(explosionDuration);

            // Fix 5: use _centerBuffer (separate from _explodeBuffer) so chain-reaction calls
            // from within ExplodeCoroutine don't overwrite the coroutine's iteration buffer
            int layerMaskCenter = explosionLayerMask.value != 0 ? explosionLayerMask.value : ~0;
            _overlapFilter = ContactFilter2D.noFilter;
            _overlapFilter.SetLayerMask(layerMaskCenter);
            int centerCount = Physics2D.OverlapBox(
                (Vector2)position,
                ExplosionProbeSize,
                0f,
                _overlapFilter,
                _centerBuffer);
            for (int i = 0; i < centerCount; i++)
            {
                var hit = _centerBuffer[i];
                if (hit == null) continue;
                var pc = hit.GetComponent<PlayerController>();
                if (pc != null && pc.enabled)
                    pc.TryApplyExplosionDamage();
            }

            // Defer propagation to next frame to avoid stack overflow when two bombs chain.
            // Use _coroutineRunner (the arena's GameManager) so this works even when the player
            // is inactive (e.g. player died before the fuse expired).
            _coroutineRunner.StartCoroutine(ExplodeBombPropagateNextFrame(position));
        }

        private IEnumerator ExplodeBombPropagateNextFrame(Vector3 position)
        {
            yield return null;

            UnityEngine.Debug.Log($"[BombController] Propagating from {position}, radius={explosionRadius}, runner={_coroutineRunner?.name}");
            Explode(position, Vector3.up, explosionRadius, explosionDelay);
            Explode(position, Vector3.down, explosionRadius, explosionDelay);
            Explode(position, Vector3.left, explosionRadius, explosionDelay);
            Explode(position, Vector3.right, explosionRadius, explosionDelay);
        }

        /// <summary>
        /// Item drops for *tilemap* destructibles: explosion spawns short-lived debris with isDebris=true,
        /// and we intentionally skip SpawnItem in Destructible.OnDestroy for debris — so roll the drop here
        /// when the tile is actually cleared (same rules as Destructible.spawnableItems / itemSpawnChance).
        /// Pushable blocks (isDebris=false) still drop from OnDestroy when DestroyBlock() runs.
        /// </summary>
        private void TrySpawnPickupForClearedDestructibleCell(Vector3Int cell)
        {
            if (destructiblePrefab == null || destructibleTiles == null)
                return;

            var template = destructiblePrefab.GetComponent<Destructible>();
            if (template == null || template.spawnableItems == null || template.spawnableItems.Length == 0)
                return;
            if (Random.value >= template.itemSpawnChance)
                return;

            EnsureArenaRootCached();
            int idx = Random.Range(0, template.spawnableItems.Length);
            Vector3 world = destructibleTiles.GetCellCenterWorld(cell);
            Instantiate(template.spawnableItems[idx], world, Quaternion.identity, _arenaRoot);
        }

        private void ClearDestructible(Vector3 position)
        {
            Vector3Int cell = destructibleTiles.WorldToCell(position);
            TileBase tile = destructibleTiles.GetTile(cell);

            if (tile != null)
            {
                TrySpawnPickupForClearedDestructibleCell(cell);

                var debris = Instantiate(destructiblePrefab, position, Quaternion.identity);
                var destructible = debris.GetComponent<Destructible>();
                if (destructible != null)
                {
                    destructible.isDebris = true; // auto-destroy after destructionTime
                }

                destructibleTiles.SetTile(cell, null);
                // Fix 7: use cached _agentNotify instead of GetComponent per block-destroy
                _agentNotify?.NotifyDestroyedBlock();

                bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
                if (isOnline && IsServer)
                    ClearDestructibleClientRpc(cell);
            }
        }

        // ── Online ClientRpcs ───────────────────────────────────────────────────────

        /// <summary>Tells clients to clear the destructible tile at <paramref name="cell"/>.</summary>
        [ClientRpc]
        private void ClearDestructibleClientRpc(Vector3Int cell)
        {
            // Already cleared on host; clients need to update their local tilemap.
            if (IsServer) return;
            if (destructibleTiles != null)
                destructibleTiles.SetTile(cell, null);
        }

        /// <summary>Tells clients to show an explosion visual segment.</summary>
        [ClientRpc]
        public void ShowExplosionClientRpc(Vector3 pos, Vector3 dir, int length)
        {
            if (IsServer) return;
            Explode(pos, dir, length, explosionDelay);
        }

        /// <summary>Tells clients to spawn a visual-only bomb at <paramref name="pos"/>.</summary>
        [ClientRpc]
        public void SpawnBombClientRpc(Vector3 pos, int radius)
        {
            if (IsServer) return;
            EnsureArenaRootCached();
            // Spawn a visual-only bomb (no fuse logic; host drives detonation).
            if (bombPrefab != null)
            {
                var visual = Instantiate(bombPrefab, pos, Quaternion.identity, _arenaRoot);
                // Disable the RemoteBombController so it doesn't self-detonate on clients.
                var rbc = visual.GetComponent<RemoteBombController>();
                if (rbc != null) rbc.enabled = false;
            }
        }

        public void AddBomb()
        {
            bombAmount++;
            bombsRemaining++;
        }

        /// <summary>Override the bomb capacity (and base amount) at runtime, e.g. from TrainingAcademyHelper.</summary>
        public void SetBombAmount(int amount)
        {
            baseBombAmount = amount;
            bombAmount = amount;
            bombsRemaining = amount;
        }

        public void IncreaseBlastRadius()
        {
            explosionRadius++;
            UnityEngine.Debug.Log($"[BombController] IncreaseBlastRadius -> new radius={explosionRadius}");
        }

        public void EnableTimeBomb()
        {
            timeBomb = true;
            remoteBomb = false;
        }

        public void EnableRemoteBomb()
        {
            remoteBomb = true;
            timeBomb = false;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Bomb"))
            {
                other.isTrigger = false;
            }
        }

        private void OnEnable()
        {
            activeBombs.Clear();
            bombsRemaining = bombAmount;

            // Fix 7: use cached _playerController instead of GetComponent
            if (_playerController != null)
            {
                ApplyUpgrades(_playerController.playerId);
            }
        }

        public void ApplyUpgrades(int playerId)
        {
            if (Core.SessionManager.Instance == null)
                return;
            // Capture base from current values if Awake hasn't run yet (e.g. in EditMode tests)
            if (baseBombAmount == 0)
                baseBombAmount = bombAmount;
            if (baseExplosionRadius == 0)
                baseExplosionRadius = explosionRadius;
            // Reset to base values so multiple calls (e.g. OnEnable + GameManager) are idempotent
            bombAmount = baseBombAmount;
            explosionRadius = baseExplosionRadius;
            timeBomb = false;
            remoteBomb = false;

            // Extra bombs
            int extraBombs = Core.SessionManager.Instance.GetUpgradeLevel(
                playerId,
                Scenes.Shop.ShopItemType.ExtraBomb
            );
            bombAmount += extraBombs;
            bombsRemaining = bombAmount;

            // Blast radius
            int powerUps = Core.SessionManager.Instance.GetUpgradeLevel(
                playerId,
                Scenes.Shop.ShopItemType.PowerUp
            );
            explosionRadius += powerUps;

            // Timebomb toggle
            if (
                Core.SessionManager.Instance.GetUpgradeLevel(
                    playerId,
                    Scenes.Shop.ShopItemType.Timebomb
                ) == 1
            )
                timeBomb = true;

            // Remote bomb toggle
            if (
                Core.SessionManager.Instance.GetUpgradeLevel(
                    playerId,
                    Scenes.Shop.ShopItemType.Controller
                ) == 1
            )
                remoteBomb = true;

            UnityEngine.Debug.Log($"[BombController] ApplyUpgrades player={playerId}: base={baseExplosionRadius} shopPowerUps={powerUps} final radius={explosionRadius}");
        }
    }
}
