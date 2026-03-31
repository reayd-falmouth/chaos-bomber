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

        [Header("Runtime Generation")]
        public GameObject destructibleWallPrefab;
        public bool generateOnStart = true;
        [Range(0f, 1f)] public float fillRate = 0.7f;
        public int safeZoneArmLength = 2;
        [Tooltip("Local-space XZ origin of cell (0,0) relative to destructibleWallsParent. " +
                 "Match this to the arena's top-left corner. Default: (-8, 0, 0).")]
        public Vector3 gridOriginLocal = new Vector3(-8f, 0f, 0f);

        [Header("Scene map thinning (when generateOnStart is off)")]
        [Tooltip("Randomly remove scene-placed destructible walls before BuildGrid. Ignored if generateOnStart is on.")]
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

        public static HybridArenaGrid Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            PublishGridOrigin();
        }

        private void Start()
        {
            GenerateWalls();
            if (thinSceneDestructibles && !generateOnStart && !thinOnlyWhenInvokedExplicitly &&
                ShouldRunThinningAuthority())
                ThinSceneDestructibles();
            BuildGrid();
        }

        /// <summary>
        /// Re-run scene thinning and rebuild the logical grid. Use after restoring walls for a new round,
        /// or when <see cref="thinOnlyWhenInvokedExplicitly"/> is true (e.g. from GameManager after one frame).
        /// </summary>
        public void ApplySceneDestructibleThinning()
        {
            if (!thinSceneDestructibles || generateOnStart) return;
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
        private void PublishGridOrigin()
        {
            Vector3 worldOrigin = destructibleWallsParent != null
                ? destructibleWallsParent.TransformPoint(gridOriginLocal)
                : gridOriginLocal;
            // The hybrid arena gameplay uses a fixed XZ grid and expects "floor" to be world Y=0.
            // Keep GridOrigin.y at 0 so SnapToCell / movement do not lift characters vertically.
            ArenaGrid3D.GridOrigin = new Vector3(worldOrigin.x, 0f, worldOrigin.z);
            UnityEngine.Debug.Log($"[HybridArenaGrid] GridOrigin set to {ArenaGrid3D.GridOrigin}");
        }

        private void GenerateWalls()
        {
            if (!generateOnStart || destructibleWallPrefab == null || destructibleWallsParent == null) return;

            for (int i = destructibleWallsParent.childCount - 1; i >= 0; i--)
                Destroy(destructibleWallsParent.GetChild(i).gameObject);

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < rows; r++)
                {
                    if (!IsCellDestructible(c, r)) continue;
                    var localPos = gridOriginLocal + new Vector3(c * ArenaGrid3D.CellSize, 0f, r * ArenaGrid3D.CellSize);
                    var worldPos = destructibleWallsParent.TransformPoint(localPos);
                    var go = Instantiate(destructibleWallPrefab, worldPos, Quaternion.identity, destructibleWallsParent);
                    go.name = $"DWall_{c}_{r}";
                }
        }

        private bool IsCellDestructible(int c, int r)
        {
            // Border cells — always indestructible
            if (c == 0 || r == 0 || c == columns - 1 || r == rows - 1) return false;
            // Pillar grid — every even column+row intersection
            if (c % 2 == 0 && r % 2 == 0) return false;
            if (IsInSpawnSafeZone(c, r)) return false;
            return Random.value < fillRate;
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
            _destructibles[cell.x, cell.y] = null;
        }

        /// <summary>Register a destructible at runtime (e.g. Superman pushable spawn after <see cref="WallBlock3D.RemoveForProceduralLayout"/>).</summary>
        public void SetDestructible(Vector2Int cell, WallBlock3D block)
        {
            if (!InBounds(cell)) return;
            _destructibles[cell.x, cell.y] = block;
        }

        /// <returns>True if the cell is blocked by an indestructible wall.</returns>
        public bool IsIndestructible(Vector2Int cell)
        {
            if (!InBounds(cell)) return true; // out of bounds = blocked
            return _indestructibleMask[cell.x, cell.y];
        }

        /// <returns>The WallBlock3D at the cell, or null if empty/already destroyed.</returns>
        public WallBlock3D GetDestructible(Vector2Int cell)
        {
            if (!InBounds(cell)) return null;
            return _destructibles[cell.x, cell.y];
        }

        /// <returns>True if the cell is completely open (no wall of any kind).</returns>
        public bool IsWalkable(Vector2Int cell)
        {
            if (!InBounds(cell)) return false;
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
