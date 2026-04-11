using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class ArenaShrinkSnakeOrderEditModeTests
    {
        [Test]
        public void EnumerateCells_2x2_YFromMin_FillsOuterTopRowFirstInIndexSpace()
        {
            var list = new List<Vector3Int>(ArenaShrinkSnakeOrder.EnumerateCells(0, 1, 0, 1, iterateYFromMinToMax: true));
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(new Vector3Int(0, 0, 0), list[0]);
            Assert.AreEqual(new Vector3Int(1, 0, 0), list[1]);
            Assert.AreEqual(new Vector3Int(1, 1, 0), list[2]);
            Assert.AreEqual(new Vector3Int(0, 1, 0), list[3]);
        }

        [Test]
        public void EnumerateCells_2x2_TopLeftFirst_Boustrophedon()
        {
            var list = new List<Vector3Int>(ArenaShrinkSnakeOrder.EnumerateCells(0, 1, 0, 1));
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(new Vector3Int(0, 1, 0), list[0]);
            Assert.AreEqual(new Vector3Int(1, 1, 0), list[1]);
            Assert.AreEqual(new Vector3Int(1, 0, 0), list[2]);
            Assert.AreEqual(new Vector3Int(0, 0, 0), list[3]);
        }

        [Test]
        public void EnumerateCells_3x3_StartsAtMinXMaxY()
        {
            var list = new List<Vector3Int>(ArenaShrinkSnakeOrder.EnumerateCells(1, 3, 1, 3));
            Assert.AreEqual(9, list.Count);
            Assert.AreEqual(new Vector3Int(1, 3, 0), list[0]);
            Assert.AreEqual(new Vector3Int(2, 3, 0), list[1]);
            Assert.AreEqual(new Vector3Int(3, 3, 0), list[2]);
            Assert.AreEqual(new Vector3Int(3, 2, 0), list[3]);
            Assert.AreEqual(new Vector3Int(2, 2, 0), list[4]);
            Assert.AreEqual(new Vector3Int(1, 2, 0), list[5]);
        }

        [Test]
        public void EnumerateCells_InvalidBounds_YieldsNothing()
        {
            int n = 0;
            foreach (var _ in ArenaShrinkSnakeOrder.EnumerateCells(2, 1, 0, 1))
                n++;
            Assert.AreEqual(0, n);
        }

        [Test]
        public void SpiralOrder_2x2_PerimeterFirst()
        {
            var list = new List<Vector3Int>(ArenaShrinkSpiralOrder.EnumerateCells(0, 1, 0, 1));
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(new Vector3Int(0, 1, 0), list[0]);
            Assert.AreEqual(new Vector3Int(1, 1, 0), list[1]);
            Assert.AreEqual(new Vector3Int(1, 0, 0), list[2]);
            Assert.AreEqual(new Vector3Int(0, 0, 0), list[3]);
        }

        [Test]
        public void CellOrder_SpiralDiffersFromSnake_On3x3()
        {
            var spiral = new List<Vector3Int>(ArenaShrinkCellOrder.EnumerateCells(ArenaShrinkPattern.SpiralInward, 0, 2, 0, 2));
            var snake = new List<Vector3Int>(ArenaShrinkCellOrder.EnumerateCells(ArenaShrinkPattern.BoustrophedonSnake, 0, 2, 0, 2));
            Assert.AreEqual(9, spiral.Count);
            Assert.AreEqual(9, snake.Count);
            // Same outer ring; divergence starts when spiral finishes the right/bottom edges before the interior row.
            Assert.AreNotEqual(spiral[4], snake[4]);
        }

        [Test]
        public void SpiralOrder_3x3_FirstIsTopLeftOfRing()
        {
            var list = new List<Vector3Int>(ArenaShrinkSpiralOrder.EnumerateCells(0, 2, 0, 2));
            Assert.AreEqual(9, list.Count);
            Assert.AreEqual(new Vector3Int(0, 2, 0), list[0]);
            Assert.AreEqual(new Vector3Int(2, 2, 0), list[2]);
            Assert.AreEqual(new Vector3Int(1, 1, 0), list[8]);
        }
    }
}
