using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Deterministic visit order for filling the playable rectangle with indestructible blocks:
    /// boustrophedon rows. Default: iterate from <see cref="maxY"/> down to <see cref="minY"/> (typical when tilemap Y+ is up).
    /// Use <paramref name="iterateYFromMinToMax"/> when cell Y+ points down so the outer "top" row (smaller Y) is filled first.
    /// </summary>
    public static class ArenaShrinkSnakeOrder
    {
        /// <summary>
        /// Inclusive bounds; x alternates left-to-right and right-to-left per row.
        /// </summary>
        /// <param name="iterateYFromMinToMax">
        /// False (default): rows from maxY → minY. True: minY → maxY (e.g. tilemap / grid Y increases downward).
        /// </param>
        public static IEnumerable<Vector3Int> EnumerateCells(
            int minX,
            int maxX,
            int minY,
            int maxY,
            bool iterateYFromMinToMax = false
        )
        {
            if (maxX < minX || maxY < minY)
                yield break;

            bool leftToRight = true;

            if (!iterateYFromMinToMax)
            {
                for (int y = maxY; y >= minY; y--)
                {
                    foreach (var c in RowCells(minX, maxX, y, leftToRight))
                        yield return c;
                    leftToRight = !leftToRight;
                }
            }
            else
            {
                for (int y = minY; y <= maxY; y++)
                {
                    foreach (var c in RowCells(minX, maxX, y, leftToRight))
                        yield return c;
                    leftToRight = !leftToRight;
                }
            }
        }

        private static IEnumerable<Vector3Int> RowCells(int minX, int maxX, int y, bool leftToRight)
        {
            if (leftToRight)
            {
                for (int x = minX; x <= maxX; x++)
                    yield return new Vector3Int(x, y, 0);
            }
            else
            {
                for (int x = maxX; x >= minX; x--)
                    yield return new Vector3Int(x, y, 0);
            }
        }
    }
}
