using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Deterministic visit order for filling the playable rectangle with indestructible blocks:
    /// boustrophedon rows from top to bottom. First cell is <see cref="Vector3Int"/>(minX, maxY) — top-left
    /// of the inner bounds when Y increases upward.
    /// </summary>
    public static class ArenaShrinkSnakeOrder
    {
        /// <summary>
        /// Inclusive bounds: iterates y from maxY down to minY; x alternates left-to-right and right-to-left per row.
        /// </summary>
        public static IEnumerable<Vector3Int> EnumerateCells(int minX, int maxX, int minY, int maxY)
        {
            if (maxX < minX || maxY < minY)
                yield break;

            bool leftToRight = true;
            for (int y = maxY; y >= minY; y--)
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

                leftToRight = !leftToRight;
            }
        }
    }
}
