using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>Selects snake vs spiral enumeration for arena shrink.</summary>
    public static class ArenaShrinkCellOrder
    {
        /// <param name="snakeIterateYFromMinToMax">Snake only; ignored for spiral.</param>
        public static IEnumerable<Vector3Int> EnumerateCells(
            ArenaShrinkPattern pattern,
            int minX,
            int maxX,
            int minY,
            int maxY,
            bool snakeIterateYFromMinToMax = false
        )
        {
            return pattern == ArenaShrinkPattern.SpiralInward
                ? ArenaShrinkSpiralOrder.EnumerateCells(minX, maxX, minY, maxY)
                : ArenaShrinkSnakeOrder.EnumerateCells(minX, maxX, minY, maxY, snakeIterateYFromMinToMax);
        }
    }
}
