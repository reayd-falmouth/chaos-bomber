using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// Static utility for grid operations in the XZ plane; vertical position uses
    /// <see cref="GridOrigin"/>.y as the arena floor height (often 0, or set by HybridArenaGrid).
    /// One cell = CellSize world units. Equivalent to MasterBlaster's ArenaPlane but for 3D XZ.
    /// </summary>
    public static class ArenaGrid3D
    {
        public static float CellSize = 1f;

        /// <summary>
        /// World-space XZ position of cell (0,0). Set by HybridArenaGrid.Start() before
        /// BuildGrid() runs. Defaults to Vector3.zero so pure-FPS scenes are unaffected.
        /// </summary>
        public static Vector3 GridOrigin = Vector3.zero;

        /// <summary>Snap a world position to the nearest cell centre (XZ; Y = GridOrigin.y).</summary>
        public static Vector3 SnapToCell(Vector3 world)
        {
            Vector3 local = world - GridOrigin;
            return GridOrigin + new Vector3(
                Mathf.Round(local.x / CellSize) * CellSize,
                0f,
                Mathf.Round(local.z / CellSize) * CellSize);
        }

        /// <summary>Convert a world position to grid indices.</summary>
        public static Vector2Int WorldToCell(Vector3 world)
        {
            Vector3 local = world - GridOrigin;
            return new Vector2Int(
                Mathf.RoundToInt(local.x / CellSize),
                Mathf.RoundToInt(local.z / CellSize));
        }

        /// <summary>
        /// Minimum XZ corner of the cell (same convention as destructible wall placement in the hybrid arena).
        /// Y is <see cref="GridOrigin"/>.y (floor height).
        /// </summary>
        public static Vector3 CellToWorld(Vector2Int cell)
        {
            return GridOrigin + new Vector3(cell.x * CellSize, 0f, cell.y * CellSize);
        }

        /// <summary>
        /// World position for a center-pivoted shrink indestructible cube (1×CellSize tall):
        /// XZ at the geometric centre of the cell, Y at <see cref="GridOrigin"/>.y plus half cube height so a unit cube rests on the floor.
        /// </summary>
        public static Vector3 CellToWorldShrinkBlock(Vector2Int cell)
        {
            float s = CellSize;
            Vector3 corner = CellToWorld(cell);
            float half = 0.5f * s;
            return new Vector3(corner.x + half, GridOrigin.y + half, corner.z + half);
        }

        /// <summary>
        /// Movement delta in XZ for one frame.
        /// direction.x maps to world X, direction.y maps to world Z.
        /// </summary>
        public static Vector3 MoveDelta(Vector2 logicalDir, float speed, float dt)
        {
            return new Vector3(logicalDir.x, 0f, logicalDir.y) * speed * dt;
        }

        /// <summary>Explosion propagation directions in XZ (equivalent to MB's up/down/left/right in XY).</summary>
        public static readonly Vector3[] ExplosionDirections =
        {
            Vector3.forward,   // Z+  (was Vector3.up    in MB)
            Vector3.back,      // Z-  (was Vector3.down  in MB)
            Vector3.left,      // X-  (same)
            Vector3.right,     // X+  (same)
        };
    }
}
