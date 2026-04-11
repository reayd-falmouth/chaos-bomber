using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Materialize shrink visit order and optionally rotate so a manual grid cell is visited first.
    /// </summary>
    public static class ArenaShrinkOrderUtilities
    {
        public static List<Vector3Int> ToOrderedList(
            ArenaShrinkPattern pattern,
            int minX,
            int maxX,
            int minY,
            int maxY,
            bool snakeIterateYFromMinToMax
        )
        {
            var list = new List<Vector3Int>();
            foreach (
                var c in ArenaShrinkCellOrder.EnumerateCells(
                    pattern,
                    minX,
                    maxX,
                    minY,
                    maxY,
                    snakeIterateYFromMinToMax
                )
            )
            {
                list.Add(c);
            }

            return list;
        }

        /// <summary>
        /// If <paramref name="startX"/> / <paramref name="startY"/> appear in <paramref name="order"/>,
        /// returns a new list rotated so that cell is first; otherwise returns false.
        /// </summary>
        public static bool TryRotateToStart(
            IReadOnlyList<Vector3Int> order,
            int startX,
            int startY,
            out List<Vector3Int> rotated
        )
        {
            rotated = null;
            int idx = -1;
            for (int i = 0; i < order.Count; i++)
            {
                var c = order[i];
                if (c.x == startX && c.y == startY)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
                return false;

            rotated = new List<Vector3Int>(order.Count);
            for (int i = idx; i < order.Count; i++)
                rotated.Add(order[i]);
            for (int i = 0; i < idx; i++)
                rotated.Add(order[i]);
            return true;
        }
    }
}
