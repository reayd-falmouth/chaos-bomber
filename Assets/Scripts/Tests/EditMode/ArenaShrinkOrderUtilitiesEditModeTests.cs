using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class ArenaShrinkOrderUtilitiesEditModeTests
    {
        [Test]
        public void TryRotateToStart_3x3Snake_InteriorCell_FirstIsRotatedStart()
        {
            var order = ArenaShrinkOrderUtilities.ToOrderedList(
                ArenaShrinkPattern.BoustrophedonSnake,
                0,
                2,
                0,
                2,
                snakeIterateYFromMinToMax: false
            );
            Assert.AreEqual(9, order.Count);
            Assert.AreEqual(new Vector3Int(0, 2, 0), order[0]);

            Assert.IsTrue(
                ArenaShrinkOrderUtilities.TryRotateToStart(order, 1, 1, out var rotated),
                "center cell (1,1) must exist in 3x3 snake order"
            );
            Assert.AreEqual(9, rotated.Count);
            Assert.AreEqual(new Vector3Int(1, 1, 0), rotated[0]);
        }

        [Test]
        public void TryRotateToStart_MissingCell_ReturnsFalse()
        {
            var order = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
            };
            Assert.IsFalse(ArenaShrinkOrderUtilities.TryRotateToStart(order, 99, 99, out var rotated));
            Assert.IsNull(rotated);
        }

        [Test]
        public void ToOrderedList_Spiral3x3_MatchesStreamingEnumeration()
        {
            var streamed = new List<Vector3Int>(
                ArenaShrinkCellOrder.EnumerateCells(ArenaShrinkPattern.SpiralInward, 0, 2, 0, 2)
            );
            var materialized = ArenaShrinkOrderUtilities.ToOrderedList(
                ArenaShrinkPattern.SpiralInward,
                0,
                2,
                0,
                2,
                snakeIterateYFromMinToMax: false
            );
            Assert.AreEqual(streamed.Count, materialized.Count);
            for (int i = 0; i < streamed.Count; i++)
                Assert.AreEqual(streamed[i], materialized[i]);
        }
    }
}
