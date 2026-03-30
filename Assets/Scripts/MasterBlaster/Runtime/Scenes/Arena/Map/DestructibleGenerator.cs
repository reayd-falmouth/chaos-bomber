using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Randomly fills the Destructibles tilemap on Awake, keeping L-shaped safe zones clear at
    /// each corner and a clear square at the centre so players can always place a bomb and retreat.
    ///
    /// Attach this to the same GameObject as the Grid (the map root). If the root starts inactive
    /// (e.g. the alt map in MapSelector), Awake fires when MapSelector calls SetActive(true) —
    /// which runs at ExecutionOrder -500 — so the tilemap is ready before BombController reads it.
    ///
    /// If the root starts active (normal map), add [DefaultExecutionOrder(-400)] above this class
    /// or ensure MapSelector is absent; the script still works correctly either way.
    /// </summary>
    public class DestructibleGenerator : MonoBehaviour
    {
        [Header("Tilemaps")]
        [Tooltip("Leave null to auto-find by name below.")]
        [SerializeField] private Tilemap destructiblesTilemap;
        [SerializeField] private Tilemap indestructiblesTilemap;

        [Tooltip("Name used to find child Tilemaps when fields above are unassigned.")]
        [SerializeField] private string destructiblesName = "Destructibles";
        [SerializeField] private string indestructiblesName = "Indestructibles";

        [Header("Tile")]
        [Tooltip("Tile asset placed for each destructible block. If null, the first tile already " +
                 "in the Destructibles tilemap is reused (pre-paint at least one tile in the editor).")]
        [SerializeField] private TileBase destructibleTile;

        [Header("Generation")]
        [Range(0f, 1f)]
        [Tooltip("Probability that an eligible cell receives a destructible block (0 = empty, 1 = full).")]
        [SerializeField] private float fillRate = 0.7f;

        [Tooltip("Number of cells extending from each corner in each arm of the L-shaped safe zone " +
                 "(arm = 2 means 3 clear cells per arm: the corner itself + 2 more).")]
        [SerializeField] private int safeZoneArmLength = 2;

        [Tooltip("Half-size of the centre safe zone in cells (1 → 3×3, 2 → 5×5).")]
        [SerializeField] private int centreSafeRadius = 1;

        private void Awake()
        {
            // Auto-detect tilemaps by searching direct children.
            if (!destructiblesTilemap)
            {
                var t = transform.Find(destructiblesName);
                if (t) destructiblesTilemap = t.GetComponent<Tilemap>();
            }
            if (!indestructiblesTilemap)
            {
                var t = transform.Find(indestructiblesName);
                if (t) indestructiblesTilemap = t.GetComponent<Tilemap>();
            }

            if (!destructiblesTilemap)
            {
                UnityEngine.Debug.LogWarning("[DestructibleGenerator] Destructibles tilemap not found — skipping generation.");
                return;
            }

            Generate();
        }

        private void Generate()
        {
            // ── 1. Resolve the tile asset ────────────────────────────────────────────
            TileBase tile = destructibleTile;
            if (tile == null)
            {
                destructiblesTilemap.CompressBounds();
                foreach (var pos in destructiblesTilemap.cellBounds.allPositionsWithin)
                {
                    tile = destructiblesTilemap.GetTile(pos);
                    if (tile != null) break;
                }
            }

            if (tile == null)
            {
                UnityEngine.Debug.LogWarning("[DestructibleGenerator] No destructible tile found. " +
                                             "Assign 'Destructible Tile' in the Inspector or pre-paint at least one tile.");
                return;
            }

            // ── 2. Playable bounds (inside the outer wall) ───────────────────────────
            int minX, maxX, minY, maxY;

            if (indestructiblesTilemap)
            {
                indestructiblesTilemap.CompressBounds();
                BoundsInt b = indestructiblesTilemap.cellBounds;
                // cellBounds.xMax/yMax are exclusive; subtract one extra for the wall tile itself.
                minX = b.xMin + 1;
                maxX = b.xMax - 2;
                minY = b.yMin + 1;
                maxY = b.yMax - 2;
            }
            else
            {
                // Fallback: use the destructibles tilemap's own bounds.
                destructiblesTilemap.CompressBounds();
                BoundsInt b = destructiblesTilemap.cellBounds;
                minX = b.xMin;
                maxX = b.xMax - 1;
                minY = b.yMin;
                maxY = b.yMax - 1;
            }

            if (minX > maxX || minY > maxY)
            {
                UnityEngine.Debug.LogWarning("[DestructibleGenerator] Computed bounds are degenerate — nothing to generate.");
                return;
            }

            int centreX = (minX + maxX) / 2;
            int centreY = (minY + maxY) / 2;
            int arm     = safeZoneArmLength;

            // ── 3. Build the safe-zone set ───────────────────────────────────────────
            var safe = new HashSet<Vector3Int>();

            // Helper: L-shape from a corner, arms pointing inward.
            // cornerX / cornerY: the corner cell; dx / dy: +1 or -1 (direction toward map centre).
            void AddCornerL(int cornerX, int cornerY, int dx, int dy)
            {
                // Horizontal arm
                for (int i = 0; i <= arm; i++)
                    safe.Add(new Vector3Int(cornerX + dx * i, cornerY, 0));
                // Vertical arm (skip the corner itself — already added above)
                for (int i = 1; i <= arm; i++)
                    safe.Add(new Vector3Int(cornerX, cornerY + dy * i, 0));
            }

            AddCornerL(minX, minY, +1, +1); // bottom-left  → right + up
            AddCornerL(maxX, minY, -1, +1); // bottom-right → left  + up
            AddCornerL(minX, maxY, +1, -1); // top-left     → right + down
            AddCornerL(maxX, maxY, -1, -1); // top-right    → left  + down

            // Centre square
            for (int x = centreX - centreSafeRadius; x <= centreX + centreSafeRadius; x++)
                for (int y = centreY - centreSafeRadius; y <= centreY + centreSafeRadius; y++)
                    safe.Add(new Vector3Int(x, y, 0));

            // ── 4. Clear and repopulate ───────────────────────────────────────────────
            destructiblesTilemap.ClearAllTiles();

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var cell = new Vector3Int(x, y, 0);

                    if (safe.Contains(cell))
                        continue;

                    // Skip cells that hold a permanent indestructible tile (e.g. pillar grid).
                    if (indestructiblesTilemap != null && indestructiblesTilemap.HasTile(cell))
                        continue;

                    if (Random.value < fillRate)
                        destructiblesTilemap.SetTile(cell, tile);
                }
            }

            UnityEngine.Debug.Log($"[DestructibleGenerator] Generated destructibles in bounds " +
                                  $"X:{minX}..{maxX} Y:{minY}..{maxY}, arm={arm}, fill={fillRate:P0}");
        }
    }
}
