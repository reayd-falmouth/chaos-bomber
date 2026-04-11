using System.Collections;
using System.Collections.Generic;
using System.Text;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Debug;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Bomb
{
    /// <summary>
    /// 3D port of MasterBlaster's BombController.
    /// Physics2D → Physics (3D), XY plane → XZ plane.
    /// Server-authoritative logic kept but wrapped in IsServer guards for future NGO upgrade.
    /// Enabled only in Bomberman mode (GameModeManager disables it in FPS mode).
    /// </summary>
    public class BombController3D : MonoBehaviour
    {
        [Header("Bomb")]
        public GameObject bombPrefab;
        [SerializeField] private float bombYOffset = 0.5f; // Adjust this value in Inspector
        public float bombFuseTime = 3f;
        public int bombAmount = 1;
        public bool timeBomb = false;
        public bool remoteBomb = false;
        [Tooltip("When no IPlayerInput: time/remote bomb detonate-held uses this key (matches 2D BombController.inputKey).")]
        [SerializeField] private KeyCode detonateKey = KeyCode.LeftShift;

        [Header("Explosion")]
        public Explosion3D explosionPrefab;
        [Tooltip("Leave empty (Nothing) so overlap uses all layers (~0). If set, include IndestructibleWall and other gameplay layers.")]
        public LayerMask explosionLayerMask;
        public float explosionDuration = 0.5f;
        public int explosionRadius = 1;
        public float explosionDelay = 0.05f;

        [Header("Input (assign PlayerControls InputActionAsset)")]
        public InputActionAsset inputActions;

        [Header("Debug")]
        [Tooltip("When enabled, each successful bomb spawn logs player Billbox, bomb BillBox directional sprites, and BillboardSprite roots (mode/camera path).")]
        [SerializeField] private bool debugLogBombBillBoxOnSpawn;

        // ── runtime state ──────────────────────────────────────────────────────────
        private int m_BombsRemaining;
        private readonly HashSet<GameObject> m_ActiveBombs = new HashSet<GameObject>();

        // Pre-allocated overlap buffer — avoids allocations per explosion step
        private static readonly Collider[] s_OverlapBuffer = new Collider[16];

        private static readonly Vector3 k_OverlapHalfExtents = new Vector3(0.4f, 1.5f, 0.4f);

        // NameToLayer must not run in static field initializers (Unity throws). Cache once from Awake.
        private static bool s_LayerIdsCached;
        private static int s_Bomb3DLayer = -1;
        private static int s_IndestructibleWallLayer = -1;

        private InputAction m_BombAction;
        private PlayerDualModeController m_DualMode;

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            CacheLayerIds();
            m_BombsRemaining = bombAmount;
            m_DualMode = GetComponent<PlayerDualModeController>();
            if (inputActions == null && m_DualMode != null && m_DualMode.inputActions != null)
                inputActions = m_DualMode.inputActions;
            BindActions();
        }

        private static void CacheLayerIds()
        {
            if (s_LayerIdsCached) return;
            s_Bomb3DLayer = LayerMask.NameToLayer("Bomb3D");
            s_IndestructibleWallLayer = LayerMask.NameToLayer("IndestructibleWall");
            s_LayerIdsCached = true;
        }

        private void OnEnable()
        {
            m_BombsRemaining = bombAmount;
            m_ActiveBombs.Clear();
            RefreshBombActionEnabled();
        }

        private void Start()
        {
            // OnEnable often runs before PlayerDualModeController.Start() fixes playerId — re-sync PlaceBomb enable state.
            RefreshBombActionEnabled();
            // #region agent log
            if (m_DualMode != null)
            {
                var ip = GetComponent<IPlayerInput>();
                string d = "{\"ownsKb\":" + (m_DualMode.OwnsBombermanSharedInput() ? "true" : "false") +
                           ",\"bombActionNull\":" + (m_BombAction == null ? "true" : "false") +
                           ",\"bombActionEnabled\":" +
                           (m_BombAction != null && m_BombAction.enabled ? "true" : "false") +
                           ",\"hasIPlayerInput\":" + (ip != null ? "true" : "false") +
                           ",\"prefabNull\":" + (bombPrefab == null ? "true" : "false") + "}";
                AgentDebugNdjson.Log("BOMB", "BombController3D.Start", "state", d);
            }
            // #endregion
        }

        private void OnDisable()
        {
            m_BombAction?.Disable();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (m_DualMode == null) return;
            if (m_BombsRemaining <= 0) return;

            // Always prefer IPlayerInput when present: HumanPlayerInput reads gamepad South directly and
            // keyboard via PlaceBomb; using only m_BombAction.WasPressedThisFrame() for "keyboard owner"
            // misses gamepad because South is not routed the same way as the shared action on all setups.
            var ip = GetComponent<IPlayerInput>();
            bool wantBomb;
            if (ip != null)
                wantBomb = ip.GetBombDown();
            else if (m_DualMode.OwnsBombermanSharedInput() && m_BombAction != null && m_BombAction.enabled)
                wantBomb = m_BombAction.WasPressedThisFrame();
            else
                return;

            if (wantBomb)
            {
                UnityEngine.Debug.Log($"[BombController3D] {gameObject.name} placing bomb (viaIPlayerInput={ip != null})");
                // #region agent log
                AgentDebugNdjson.Log("F", "BombController3D.Update", "want_bomb",
                    "{\"viaIPlayerInput\":" + (ip != null ? "true" : "false") + ",\"hasHuman\":" +
                    (GetComponent<HumanPlayerInput>() != null ? "true" : "false") + "}");
                // #endregion
                SpawnBomb();
            }
        }

        // ── Bomb spawning ──────────────────────────────────────────────────────────

        private void SpawnBomb()
        {
            if (bombPrefab == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BombController3D] {gameObject.name}: bombPrefab is not assigned — cannot spawn.");
                // #region agent log
                AgentDebugNdjson.Log("BOMB", "BombController3D.SpawnBomb", "prefab_null",
                    "{\"player\":\"" + gameObject.name + "\"}");
                // #endregion
                return;
            }

            Vector3 pos = ArenaGrid3D.SnapToCell(transform.position);

            // Add the Y offset here
            pos.y += bombYOffset;
            
            // Don't place if a bomb already occupies this cell
            foreach (var b in m_ActiveBombs)
            {
                if (b == null) continue;
                if (ArenaGrid3D.WorldToCell(b.transform.position) == ArenaGrid3D.WorldToCell(pos))
                    return;
            }

            // Don't place on top of a destructible wall
            if (HybridArenaGrid.Instance != null)
            {
                var cell = ArenaGrid3D.WorldToCell(pos);
                if (HybridArenaGrid.Instance.GetDestructible(cell) != null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[BombController3D] {gameObject.name}: cell {cell} has a destructible block — bomb not placed.");
                    return;
                }
            }

            var bomb = Instantiate(bombPrefab, pos, Quaternion.identity, GetArenaSpawnParent());
            bomb.AddComponent<BombPassThroughGrid3D>().Init(transform);
            if (!remoteBomb)
            {
                foreach (var asr in bomb.GetComponentsInChildren<AnimatedSpriteRenderer>())
                    asr.idle = false;
            }

            m_ActiveBombs.Add(bomb);
            m_BombsRemaining--;

            if (timeBomb && !remoteBomb)
                StartCoroutine(TimeBombWaitRelease(bomb));
            else if (remoteBomb)
            {
                var owner = m_DualMode != null ? m_DualMode : GetComponent<PlayerDualModeController>();
                if (owner == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[BombController3D] {gameObject.name}: remote bomb requires PlayerDualModeController — using fuse.");
                    StartCoroutine(FuseBomb(bomb));
                }
                else
                {
                    var remote = bomb.AddComponent<RemoteBombController3D>();
                    remote.Init(this, owner, detonateKey);
                }
            }
            else
                StartCoroutine(FuseBomb(bomb));

            LogBombBillBoxSpawnDebug(bomb);
        }

        private void LogBombBillBoxSpawnDebug(GameObject bomb)
        {
            if (!debugLogBombBillBoxOnSpawn || bomb == null)
                return;

            var gmm = GameModeManager.Instance;
            string mode = gmm != null ? gmm.CurrentMode.ToString() : "GameModeManager=null";
            var main = UnityEngine.Camera.main;
            string camInfo = main != null ? $"{main.name} ortho={main.orthographic}" : "Camera.main=null";

            Transform playerBillbox = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Billbox")
                {
                    playerBillbox = t;
                    break;
                }
            }

            string playerBillboxLine = playerBillbox != null
                ? $"localPos={playerBillbox.localPosition} localEuler={playerBillbox.localRotation.eulerAngles}"
                : "Billbox (player) not found";

            Transform bombBillBox = bomb.transform.Find("BillBox");
            string bombBillBoxLine = bombBillBox != null
                ? $"BillBox worldEuler={bombBillBox.rotation.eulerAngles} localEuler={bombBillBox.localRotation.eulerAngles}"
                : "BillBox (bomb) missing under prefab";

            var boards = bomb.GetComponentsInChildren<BillboardSprite>(true);
            var boardsSb = new StringBuilder();
            for (int i = 0; i < boards.Length; i++)
            {
                var bb = boards[i];
                boardsSb.Append(bb.gameObject.name).Append(" euler=").Append(bb.transform.rotation.eulerAngles);
                if (i < boards.Length - 1) boardsSb.Append("; ");
            }

            var spritesSb = new StringBuilder();
            if (bombBillBox != null)
            {
                foreach (var asr in bombBillBox.GetComponentsInChildren<AnimatedSpriteRenderer>(true))
                {
                    var go = asr.gameObject;
                    if (spritesSb.Length > 0) spritesSb.Append("; ");
                    spritesSb.Append(go.name).Append(": active=").Append(go.activeSelf)
                        .Append(" asr.enabled=").Append(asr.enabled).Append(" idle=").Append(asr.idle);
                }
            }

            UnityEngine.Debug.Log(
                "[BombController3D] BillBox spawn debug — player=" + gameObject.name +
                " mode=" + mode + " mainCam=" + camInfo +
                " remoteBomb=" + remoteBomb + " timeBomb=" + timeBomb +
                "\n  player " + playerBillboxLine +
                "\n  bomb " + bomb.name + " " + bombBillBoxLine +
                "\n  BillboardSprite (" + boards.Length + "): " + (boards.Length == 0 ? "(none)" : boardsSb.ToString()) +
                "\n  BillBox AnimatedSpriteRenderer: " + (spritesSb.Length == 0 ? "(none under BillBox)" : spritesSb.ToString()),
                this);
        }

        /// <summary>
        /// Same semantics as 2D <see cref="HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb.RemoteBombController"/> Time mode:
        /// hold detonate to delay, release to explode.
        /// </summary>
        private IEnumerator TimeBombWaitRelease(GameObject bomb)
        {
            while (bomb != null && m_ActiveBombs.Contains(bomb))
            {
                var ip = GetComponent<IPlayerInput>();
                bool detonateHeld = ip != null ? ip.GetDetonateHeld() : Input.GetKey(detonateKey);
                if (!detonateHeld)
                {
                    ExplodeBomb(bomb);
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator FuseBomb(GameObject bomb)
        {
            yield return new WaitForSeconds(bombFuseTime);
            if (bomb != null)
                ExplodeBomb(bomb);
        }

        public void ExplodeBomb(GameObject bomb)
        {
            if (bomb == null) return;

            // Snap XZ to grid, but preserve the bomb's world Y so the explosion appears at the same height.
            // SnapToCell forces Y=0 which can cause vertical misalignment with the bomb prefab.
            Vector3 pos = ArenaGrid3D.SnapToCell(bomb.transform.position);
            pos.y = bomb.transform.position.y;
            m_ActiveBombs.Remove(bomb);
            Destroy(bomb);
            m_BombsRemaining++;

            // Centre explosion
            if (explosionPrefab != null)
            {
                var e = Instantiate(explosionPrefab, pos, Quaternion.identity, GetArenaSpawnParent());
                e.PlayExplosionSound();
                e.DestroyAfter(explosionDuration);
            }

            // Centre overlap — damage players and destroy items at the detonation cell
            OverlapDamagePlayers(pos);
            OverlapDestroyItems(pos);

            // Propagate next frame to avoid stack overflow from chain reactions
            StartCoroutine(PropagateNextFrame(pos));
        }

        private IEnumerator PropagateNextFrame(Vector3 pos)
        {
            yield return null;
            foreach (var dir in ArenaGrid3D.ExplosionDirections)
                Explode(pos, dir, explosionRadius, explosionDelay);
        }

        // ── Explosion propagation ─────────────────────────────────────────────────

        public void Explode(Vector3 position, Vector3 direction, int length, float delayStep = 0.05f)
        {
            StartCoroutine(ExplodeCoroutine(position, direction, length, delayStep));
        }

        private IEnumerator ExplodeCoroutine(Vector3 position, Vector3 direction, int length, float delayStep)
        {
            CacheLayerIds();
            for (int i = 1; i <= length; i++)
            {
                // Wait before each outward step so the first ring is delayed after the centre blast (visible wave).
                yield return new WaitForSeconds(delayStep);

                Vector3 nextPos = position + direction * i;

                // Authoritative grid block (pillars/border) — works even if colliders use the wrong layer.
                var arena = HybridArenaGrid.Instance;
                if (arena != null)
                {
                    var cell = ArenaGrid3D.WorldToCell(nextPos);
                    if (arena.IsIndestructible(cell))
                        yield break;

                    var destructible = arena.GetDestructible(cell);
                    if (destructible != null)
                    {
                        destructible.DestroyBlock();
                        yield break;
                    }
                }

                int maskValue = explosionLayerMask.value != 0 ? explosionLayerMask.value : ~0;
                int count = Physics.OverlapBoxNonAlloc(nextPos, k_OverlapHalfExtents, s_OverlapBuffer,
                    Quaternion.identity, maskValue, QueryTriggerInteraction.Collide);

                bool blocked = false;

                for (int j = 0; j < count; j++)
                {
                    var hit = s_OverlapBuffer[j];
                    if (hit == null || hit.gameObject == gameObject) continue;

                    // Player hit — apply damage, keep propagating
                    var hp = hit.GetComponentInParent<Unity.FPS.Game.Health>();
                    if (hp != null)
                    {
                        if (!hp.TryAbsorbExplosionHit())
                            hp.TakeDamage(100f, gameObject);
                        continue;
                    }

                    // Destructible wall — destroy and stop
                    if (hit.TryGetComponent<WallBlock3D>(out var wall))
                    {
                        wall.DestroyBlock();
                        blocked = true;
                        break;
                    }

                    // Another bomb — chain reaction, keep propagating
                    if (s_Bomb3DLayer >= 0 && hit.gameObject.layer == s_Bomb3DLayer)
                    {
                        // Find the owning BombController3D and trigger early detonation
                        var allControllers = FindObjectsByType<BombController3D>(FindObjectsSortMode.None);
                        foreach (var bc in allControllers)
                            bc.TryChainDetonate(hit.gameObject);
                        continue;
                    }

                    // Item pickup — collect it (plays feedbacks), keep propagating
                    if (hit.TryGetComponent<ItemPickup3D>(out var item))
                    {
                        item.Collect();
                        continue;
                    }

                    // Indestructible wall — stop (layer fallback when grid is unavailable or misaligned)
                    if (s_IndestructibleWallLayer >= 0 && hit.gameObject.layer == s_IndestructibleWallLayer)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked) yield break;

                // Spawn fire visual
                if (explosionPrefab != null)
                {
                    var e = Instantiate(explosionPrefab, nextPos, Quaternion.identity, GetArenaSpawnParent());
                    e.SetDirection(direction);
                    e.DestroyAfter(explosionDuration);
                }

                OverlapDamagePlayers(nextPos);
            }
        }

        public void TryChainDetonate(GameObject bomb)
        {
            if (m_ActiveBombs.Contains(bomb))
                ExplodeBomb(bomb);
        }

        /// <summary>Parent for bombs/explosions so they live under the arena (not scene root) and disable with the match UI.</summary>
        private static Transform GetArenaSpawnParent()
        {
            var grid = HybridArenaGrid.Instance;
            if (grid == null) return null;
            return grid.destructibleWallsParent != null ? grid.destructibleWallsParent : grid.transform;
        }

        private void OverlapDestroyItems(Vector3 pos)
        {
            int count = Physics.OverlapBoxNonAlloc(pos, k_OverlapHalfExtents, s_OverlapBuffer,
                Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (s_OverlapBuffer[i] != null &&
                    s_OverlapBuffer[i].TryGetComponent<ItemPickup3D>(out var item))
                    item.Collect();
            }
        }

        private void OverlapDamagePlayers(Vector3 pos)
        {
            var damaged = new HashSet<Health>();

            int count = Physics.OverlapBoxNonAlloc(pos, k_OverlapHalfExtents, s_OverlapBuffer,
                Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var hit = s_OverlapBuffer[i];
                if (hit == null) continue;
                var hp = hit.GetComponentInParent<Health>();
                if (hp == null || damaged.Contains(hp)) continue;
                damaged.Add(hp);
                if (!hp.TryAbsorbExplosionHit())
                    hp.TakeDamage(100f, gameObject);
            }

            // CharacterController players have no Physics Collider — damage by grid cell.
            var explosionCell = ArenaGrid3D.WorldToCell(pos);
            foreach (var hp in FindObjectsByType<Health>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (hp == null || damaged.Contains(hp)) continue;
                if (ArenaGrid3D.WorldToCell(hp.transform.position) != explosionCell) continue;
                damaged.Add(hp);
                if (!hp.TryAbsorbExplosionHit())
                    hp.TakeDamage(100f, gameObject);
            }
        }

        // ── Upgrades ──────────────────────────────────────────────────────────────

        public void AddBomb() { bombAmount++; m_BombsRemaining++; }
        public void IncreaseBlastRadius() { explosionRadius++; }
        public void EnableTimeBomb()   { timeBomb = true;  remoteBomb = false; }
        public void EnableRemoteBomb() { remoteBomb = true; timeBomb = false; }

        // ── Input ─────────────────────────────────────────────────────────────────

        private void BindActions()
        {
            if (inputActions == null) return;
            var map = inputActions.FindActionMap("Player");
            m_BombAction = map?.FindAction("PlaceBomb");
        }

        /// <summary>
        /// Call after <see cref="PlayerDualModeController"/> finalizes playerId / shared keyboard flags
        /// (same asset action may have been left disabled from an early OnEnable).
        /// </summary>
        public void RefreshBombInputFromDualMode()
        {
            if (!enabled) return;
            RefreshBombActionEnabled();
        }

        /// <summary>
        /// HumanPlayerInput enables PlaceBomb on the asset for keyboard; do not duplicate-enable here.
        /// Fallback: only the keyboard owner without HumanPlayerInput uses this copy.
        /// </summary>
        private void RefreshBombActionEnabled()
        {
            if (GetComponent<HumanPlayerInput>() != null)
            {
                m_BombAction?.Disable();
                return;
            }

            if (m_DualMode != null && m_DualMode.OwnsBombermanSharedInput())
                m_BombAction?.Enable();
            else
                m_BombAction?.Disable();
        }
    }
}
