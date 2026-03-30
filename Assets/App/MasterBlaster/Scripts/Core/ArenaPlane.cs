using UnityEngine;
using UnityEngine.Tilemaps;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Arena gameplay uses the XY plane with Z as depth. Logical movement uses <see cref="Vector2"/>
    /// mapping directly to world X and Y (standard 2D).
    /// </summary>
    public static class ArenaPlane
    {
        public static Vector2 LogicalXY(Vector3 world) => new Vector2(world.x, world.y);

        public static Vector3 FromLogicalXY(Vector2 logical) =>
            new Vector3(logical.x, logical.y, 0f);

        /// <summary>Snaps a world position to the tilemap cell center (full Vector3).</summary>
        public static Vector3 SnapWorldToTileGrid(Tilemap tm, Vector3 world)
        {
            if (tm == null)
            {
                return new Vector3(
                    Mathf.Round(world.x),
                    Mathf.Round(world.y),
                    world.z);
            }

            var cell = tm.WorldToCell(world);
            return tm.GetCellCenterWorld(cell);
        }

        public static Vector2 MoveDeltaXY(Vector2 logicalDir, float speed, float dt) =>
            logicalDir * speed * dt;

        /// <summary>Rounds logical grid coordinates from world (for AI / danger checks).</summary>
        public static Vector2 RoundToCellXY(Vector3 world) =>
            new Vector2(Mathf.Round(world.x), Mathf.Round(world.y));
    }
}
