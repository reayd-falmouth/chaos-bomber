using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// Runtime grid for the hybrid arena. Holds references to WallBlock3D objects and
    /// provides walkability queries for AI and bomb explosion propagation.
    /// </summary>
    public class HybridArenaGrid : MonoBehaviour
    {
        [Header("Arena Dimensions")]
        public int columns = 15;
        public int rows = 13;

        [Header("Parents (assign in Inspector)")]
        public Transform destructibleWallsParent;
        public Transform indestructibleWallsParent;

        [Header("Destructible Layout (assign in Inspector)")]
        [Tooltip("Prefab containing the authored destructible wall layout (e.g. 'DestructibleWalls' root with children). " +
                 "Resets restore from this prefab baseline (recommended). If unset, the initial scene children are captured as the baseline instead.")]
        public GameObject destructibleWallsLayoutPrefab;
        public int safeZoneArmLength = 2;
        [Tooltip("Local-space XZ origin of cell (0,0) relative to destructibleWallsParent. " +
                 "Match this to the arena's top-left corner. Default: (-8, 0, 0). " +
                 "Use the HybridArenaGrid inspector alignment buttons or Context Menu → Diagnostics/Log Grid Alignment if shrink/walls look shifted.")]
        public Vector3 gridOriginLocal = new Vector3(-8f, 0f, 0f);

        [Header("Scene map thinning")]
        [Tooltip("Randomly remove destructible walls from the populated layout before BuildGrid.")]
        public bool thinSceneDestructibles;
        [Range(0f, 1f)]
        [Tooltip("Per-wall probability to KEEP ( Bernoulli ). Expected remaining ≈ keepChance × N.")]
        public float sceneDestructibleKeepChance = 0.75f;
        [Tooltip("If true, never thin cells in the same corner spawn safe arms as safeZoneArmLength.")]
        public bool thinRespectSpawnSafeZone = true;
        [Tooltip("If true, Start() does not thin; call ApplySceneDestructibleThinning() from code (e.g. GameManager) after load.")]
        public bool thinOnlyWhenInvokedExplicitly;
        [Tooltip("Non-zero: Random.InitState before thinning for reproducible layouts.")]
        public int proceduralSeed;

        private WallBlock3D[,] _destructibles;
        private bool[,] _indestructibleMask; // true = blocked by permanent wall

        private Transform _baselineDestructiblesRoot;
        private bool _baselineCaptured;

        public static HybridArenaGrid Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            RepublishGridOriginInternal();
        }

        private void Start()
        {
            CaptureBaselineDestructiblesIfNeeded();
            RestoreDestructiblesFromBaselineThenRethinAndRebuild();
        }

        private void EnsureGridBuilt()
        {
            if (_destructibles != null && _indestructibleMask != null)
                return;

            // If something queries walkability before Start(), build a best-effort grid
            // from whatever is currently in the scene hierarchy.
            BuildGrid();
        }

        private static void CloneChildPreserveLocal(Transform src, Transform dstParent)
        {
            if (src == null || dstParent == null) return;
            var go = Instantiate(src.gameObject);
            go.name = src.gameObject.name;
            var t = go.transform;

            // Netcode note: Some layout prefabs include NetworkObjects.
            // When NGO isn't running, setting parent on a NetworkObject can throw NotListeningException.
            // In offline mode, strip the NetworkObject component so we can parent normally.
            var nm = NetworkManager.Singleton;
            bool ngoListening = nm != null && nm.IsListening;
            if (!ngoListening && go.TryGetComponent<NetworkObject>(out var netObj) && netObj != null)
                DestroyImmediate(netObj);

            t.SetParent(dstParent, false);
            t.localPosition = src.localPosition;
            t.localRotation = src.localRotation;
            t.localScale = src.localScale;
            go.SetActive(true);
        }

        private void CaptureBaselineDestructiblesIfNeeded()
        {
            if (_baselineCaptured) return;
            if (destructibleWallsParent == null) return;

            var holder = new GameObject($"{name}_BaselineDestructibles");
            holder.SetActive(false);
            holder.hideFlags = HideFlags.HideAndDontSave;
            _baselineDestructiblesRoot = holder.transform;
            _baselineDestructiblesRoot.SetParent(transform, false);

            if (destructibleWallsLayoutPrefab != null)
            {
                // Instantiate the authored layout prefab under the hidden baseline root,
                // then treat its children as the baseline set.
                var layout = Instantiate(destructibleWallsLayoutPrefab, _baselineDestructiblesRoot);
                layout.name = destructibleWallsLayoutPrefab.name;
                layout.SetActive(true);
            }
            else
            {
                // Back-compat fallback: capture whatever is currently in the scene.
                for (int i = 0; i < destructibleWallsParent.childCount; i++)
                {
                    var child = destructibleWallsParent.GetChild(i);
                    if (child == null) continue;
                    CloneChildPreserveLocal(child, _baselineDestructiblesRoot);
                }
            }

            _baselineCaptured = true;
        }

        public void RestoreDestructiblesFromBaselineThenRethinAndRebuild()
        {
            if (!_baselineCaptured || _baselineDestructiblesRoot == null || destructibleWallsParent == null)
                return;

            // Clear current
            for (int i = destructibleWallsParent.childCount - 1; i >= 0; i--)
            {
                var child = destructibleWallsParent.GetChild(i);
                if (child == null) continue;
                // Detach first so the hierarchy updates immediately (Destroy is end-of-frame).
                // Netcode note: NetworkObjects can throw NotListeningException when re-parented if NGO isn't running.
                // In offline/single-player (not listening) we skip detaching and just destroy.
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            // Restore from baseline:
            // - If we captured from a prefab, the baseline root has one child (the instantiated prefab root),
            //   and we want that root's children.
            // - If we captured from the scene, the baseline root contains wall objects directly.
            Transform baselineChildrenRoot = _baselineDestructiblesRoot;
            if (_baselineDestructiblesRoot.childCount == 1
                && destructibleWallsLayoutPrefab != null
                && _baselineDestructiblesRoot.GetChild(0).name == destructibleWallsLayoutPrefab.name)
            {
                baselineChildrenRoot = _baselineDestructiblesRoot.GetChild(0);
            }

            for (int i = 0; i < baselineChildrenRoot.childCount; i++)
            {
                var src = baselineChildrenRoot.GetChild(i);
                if (src == null) continue;

                CloneChildPreserveLocal(src, destructibleWallsParent);
                var restored = destructibleWallsParent.GetChild(destructibleWallsParent.childCount - 1).gameObject;
                // Only spawn NGO NetworkObjects when NGO is actually running and we are server/host.
                if (restored.TryGetComponent<NetworkObject>(out var no)
                    && NetworkManager.Singleton != null
                    && NetworkManager.Singleton.IsListening
                    && NetworkManager.Singleton.IsServer
                    && !no.IsSpawned)
                {
                    no.Spawn();
                }
            }

            if (thinSceneDestructibles && ShouldRunThinningAuthority())
                ThinSceneDestructibles();

            BuildGrid();
        }

        /// <summary>
        /// Re-run scene thinning and rebuild the logical grid. Use after restoring walls for a new round,
        /// or when <see cref="thinOnlyWhenInvokedExplicitly"/> is true (e.g. from GameManager after one frame).
        /// </summary>
        public void ApplySceneDestructibleThinning()
        {
            if (!thinSceneDestructibles) return;
            if (!ShouldRunThinningAuthority()) return;
            if (proceduralSeed != 0)
                Random.InitState(proceduralSeed);
            ThinSceneDestructibles();
            BuildGrid();
        }

        private static bool ShouldRunThinningAuthority()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) return true;
            return nm.IsServer;
        }

        private void ThinSceneDestructibles()
        {
            if (destructibleWallsParent == null) return;
            if (proceduralSeed != 0)
                Random.InitState(proceduralSeed);

            foreach (var wall in destructibleWallsParent.GetComponentsInChildren<WallBlock3D>())
            {
                if (wall == null) continue;

                var cell = ArenaGrid3D.WorldToCell(wall.transform.position);
                if (!InBounds(cell)) continue;
                if (thinRespectSpawnSafeZone && IsInSpawnSafeZone(cell.x, cell.y)) continue;
                if (Random.value <= sceneDestructibleKeepChance) continue;

                wall.RemoveForProceduralLayout();
            }
        }

        private bool IsInSpawnSafeZone(int c, int r)
        {
            int arm = safeZoneArmLength;
            return (c <= arm && r <= arm)
                || (c <= arm && r >= rows - 1 - arm)
                || (c >= columns - 1 - arm && r <= arm)
                || (c >= columns - 1 - arm && r >= rows - 1 - arm);
        }

        /// <summary>
        /// World-space origin of cell (0,0), including floor height (Y). Must run in Awake so
        /// Bomberman movement sees the correct plane before the first frame.
        /// </summary>
        private void RepublishGridOriginInternal()
        {
            Vector3 worldOrigin = destructibleWallsParent != null
                ? destructibleWallsParent.TransformPoint(gridOriginLocal)
                : gridOriginLocal;
            // The hybrid arena gameplay uses a fixed XZ grid and expects "floor" to be world Y=0.
            // Keep GridOrigin.y at 0 so SnapToCell / movement do not lift characters vertically.
            ArenaGrid3D.GridOrigin = new Vector3(worldOrigin.x, 0f, worldOrigin.z);
            UnityEngine.Debug.Log($"[HybridArenaGrid] GridOrigin set to {ArenaGrid3D.GridOrigin}");
        }

        /// <summary>
        /// Recompute <see cref="ArenaGrid3D.GridOrigin"/> after <see cref="destructibleWallsParent"/> or
        /// <see cref="gridOriginLocal"/> changes (e.g. multi-arena root switcher).
        /// </summary>
        public void RepublishGridOrigin() => RepublishGridOriginInternal();

        /// <summary>
        /// Samples wall world positions (destructible <see cref="WallBlock3D"/> + indestructible colliders),
        /// compares them to <see cref="ArenaGrid3D"/> cell mapping, and logs min/max indices,
        /// out-of-bounds count, and suggested <see cref="gridOriginLocal"/> delta (XZ) when the minimum
        /// cell is not (0,0). Use in Play Mode or in the Editor with the active level roots enabled.
        /// </summary>
        [ContextMenu("Diagnostics/Log Grid Alignment")]
        public void LogGridAlignmentDiagnostics()
        {
            var report = GetGridAlignmentReport();
            if (!report.HasSamples)
            {
                UnityEngine.Debug.LogWarning(
                    "[HybridArenaGrid] Grid alignment: no wall samples (assign destructible/indestructible parents and ensure roots are active).");
                return;
            }

            var localDelta = ArenaGridAlignment.WorldDeltaToGridOriginLocalDelta(
                destructibleWallsParent,
                report.SuggestedGridOriginWorldDelta);
            UnityEngine.Debug.Log(
                "[HybridArenaGrid] Grid alignment — samples: " + report.SampleCount
                + ", OOB vs columns/rows: " + report.OutOfBoundsCount
                + ", MinCell: " + report.MinCell + ", MaxCell: " + report.MaxCell
                + ", columns×rows: " + columns + "×" + rows
                + ", GridOrigin: " + ArenaGrid3D.GridOrigin
                + ", CellSize: " + ArenaGrid3D.CellSize
                + "\n  Suggested GridOrigin world delta (anchors MinCell toward 0,0): "
                + report.SuggestedGridOriginWorldDelta
                + "\n  Same adjustment as gridOriginLocal delta (XZ, parent local): " + localDelta
                + "\n  (Add the local delta to gridOriginLocal if MinCell should be (0,0) for your authored corner.)");
        }

        /// <summary>
        /// Recomputes <see cref="ArenaGrid3D.GridOrigin"/> from current fields, samples wall positions,
        /// and returns min/max cell indices vs <see cref="columns"/> / <see cref="rows"/>.
        /// </summary>
        public ArenaGridAlignment.Report GetGridAlignmentReport()
        {
            RepublishGridOrigin();
            var samples = GatherAlignmentSampleWorldPositions(this, true);
            return ArenaGridAlignment.AnalyzeWorldSamples(
                samples,
                ArenaGrid3D.GridOrigin,
                ArenaGrid3D.CellSize,
                columns,
                rows);
        }

        private static List<Vector3> GatherAlignmentSampleWorldPositions(HybridArenaGrid grid, bool includeInactive)
        {
            var list = new List<Vector3>(256);
            if (grid.destructibleWallsParent != null)
            {
                foreach (var w in grid.destructibleWallsParent.GetComponentsInChildren<WallBlock3D>(includeInactive))
                {
                    if (w != null)
                        list.Add(w.transform.position);
                }
            }

            if (grid.indestructibleWallsParent != null)
            {
                foreach (var c in grid.indestructibleWallsParent.GetComponentsInChildren<Collider>(includeInactive))
                {
                    if (c != null)
                        list.Add(c.transform.position);
                }
            }

            return list;
        }

        /// <summary>
        /// Clears captured destructible baseline and logical grid so a new layout can be bound
        /// (see <see cref="RecaptureBaselineAndRestoreLayout"/>).
        /// </summary>
        public void ResetDestructibleBaselineState()
        {
            _destructibles = null;
            _indestructibleMask = null;
            if (_baselineDestructiblesRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(_baselineDestructiblesRoot.gameObject);
                else
                    Object.DestroyImmediate(_baselineDestructiblesRoot.gameObject);
                _baselineDestructiblesRoot = null;
            }
            _baselineCaptured = false;
        }

        /// <summary>
        /// After rebinding wall parents, rebuild baseline from current scene/prefab settings and
        /// repopulate <see cref="destructibleWallsParent"/>. Use when re-entering Game without a domain reload.
        /// </summary>
        public void RecaptureBaselineAndRestoreLayout()
        {
            ResetDestructibleBaselineState();
            CaptureBaselineDestructiblesIfNeeded();
            RestoreDestructiblesFromBaselineThenRethinAndRebuild();
        }

        private void BuildGrid()
        {
            _destructibles    = new WallBlock3D[columns, rows];
            _indestructibleMask = new bool[columns, rows];

            if (destructibleWallsParent != null)
            {
                foreach (var block in destructibleWallsParent.GetComponentsInChildren<WallBlock3D>())
                {
                    var cell = ArenaGrid3D.WorldToCell(block.transform.position);
                    if (InBounds(cell))
                        _destructibles[cell.x, cell.y] = block;
                }
            }

            if (indestructibleWallsParent != null)
            {
                foreach (var col in indestructibleWallsParent.GetComponentsInChildren<Collider>())
                {
                    var cell = ArenaGrid3D.WorldToCell(col.transform.position);
                    if (InBounds(cell))
                        _indestructibleMask[cell.x, cell.y] = true;
                }
            }
        }

        /// <summary>Remove a cell's destructible reference (called when a WallBlock3D is destroyed).</summary>
        public void ClearCell(Vector2Int cell)
        {
            if (!InBounds(cell)) return;
            EnsureGridBuilt();
            _destructibles[cell.x, cell.y] = null;
        }

        /// <summary>Register a destructible at runtime (e.g. Superman pushable spawn after <see cref="WallBlock3D.RemoveForProceduralLayout"/>).</summary>
        public void SetDestructible(Vector2Int cell, WallBlock3D block)
        {
            if (!InBounds(cell)) return;
            EnsureGridBuilt();
            _destructibles[cell.x, cell.y] = block;
        }

        /// <returns>True if the cell is blocked by an indestructible wall.</returns>
        public bool IsIndestructible(Vector2Int cell)
        {
            if (!InBounds(cell)) return true; // out of bounds = blocked
            EnsureGridBuilt();
            return _indestructibleMask[cell.x, cell.y];
        }

        /// <returns>The WallBlock3D at the cell, or null if empty/already destroyed.</returns>
        public WallBlock3D GetDestructible(Vector2Int cell)
        {
            if (!InBounds(cell)) return null;
            EnsureGridBuilt();
            return _destructibles[cell.x, cell.y];
        }

        /// <returns>True if the cell is completely open (no wall of any kind).</returns>
        public bool IsWalkable(Vector2Int cell)
        {
            if (!InBounds(cell)) return false;
            EnsureGridBuilt();
            return !_indestructibleMask[cell.x, cell.y] && _destructibles[cell.x, cell.y] == null;
        }

        /// <summary>
        /// Superman: player at <paramref name="playerCell"/> pushes the destructible at <c>playerCell+dir</c>
        /// into <c>playerCell+dir+dir</c> when that destination is walkable.
        /// </summary>
        public bool TryEvaluateSupermanPush(
            Vector2Int playerCell,
            Vector2Int dir,
            out Vector2Int blockCell,
            out Vector2Int destinationCell,
            out WallBlock3D wall)
        {
            blockCell = playerCell + dir;
            destinationCell = blockCell + dir;
            wall = null;
            if (dir.x != 0 && dir.y != 0) return false;
            if (dir == Vector2Int.zero) return false;
            if (!InBounds(blockCell) || !InBounds(destinationCell)) return false;
            wall = GetDestructible(blockCell);
            if (wall == null) return false;
            if (!IsWalkable(destinationCell)) return false;
            return true;
        }

        /// <summary>Update logical grid after a block moved from <paramref name="fromCell"/> to <paramref name="toCell"/>.</summary>
        public void ApplySupermanPushGrid(Vector2Int fromCell, Vector2Int toCell, WallBlock3D block)
        {
            if (block == null || !InBounds(fromCell) || !InBounds(toCell)) return;
            ClearCell(fromCell);
            SetDestructible(toCell, block);
        }

        private bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
        }
    }
}
