using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Outside-in ring order: top edge left→right, right edge top→bottom, bottom right→left, left edge bottom→top, repeat for inner rectangle.
    /// </summary>
    public static class ArenaShrinkSpiralOrder
    {
        /// <summary>Inclusive bounds; same cell convention as <see cref="ArenaShrinkSnakeOrder"/> (Y up).</summary>
        public static IEnumerable<Vector3Int> EnumerateCells(int minX, int maxX, int minY, int maxY)
        {
            if (maxX < minX || maxY < minY)
                yield break;

            int left = minX;
            int right = maxX;
            int bottom = minY;
            int top = maxY;

            while (left <= right && bottom <= top)
            {
                for (int x = left; x <= right; x++)
                    yield return new Vector3Int(x, top, 0);
                top--;
                if (bottom > top)
                    break;

                for (int y = top; y >= bottom; y--)
                    yield return new Vector3Int(right, y, 0);
                right--;
                if (left > right)
                    break;

                for (int x = right; x >= left; x--)
                    yield return new Vector3Int(x, bottom, 0);
                bottom++;
                if (bottom > top)
                    break;

                for (int y = bottom; y <= top; y++)
                    yield return new Vector3Int(left, y, 0);
                left++;
            }
        }
    }
}
