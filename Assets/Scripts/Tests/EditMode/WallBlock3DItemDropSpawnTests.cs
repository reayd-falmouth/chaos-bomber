using HybridGame.MasterBlaster.Scripts.Arena;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class WallBlock3DItemDropSpawnTests
    {
        [Test]
        public void CellToWorldShrinkBlock_MatchesCellToWorldXZ_AndHalfCubeOnY()
        {
            var prevOrigin = ArenaGrid3D.GridOrigin;
            var prevCellSize = ArenaGrid3D.CellSize;
            try
            {
                ArenaGrid3D.GridOrigin = new Vector3(1f, 5f, -2f);
                ArenaGrid3D.CellSize = 1f;
                var cell = new Vector2Int(2, 3);
                Vector3 xz = ArenaGrid3D.CellToWorld(cell);
                Vector3 expected = new Vector3(xz.x, ArenaGrid3D.GridOrigin.y + 0.5f * ArenaGrid3D.CellSize, xz.z);
                Vector3 actual = ArenaGrid3D.CellToWorldShrinkBlock(cell);
                Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f));
            }
            finally
            {
                ArenaGrid3D.GridOrigin = prevOrigin;
                ArenaGrid3D.CellSize = prevCellSize;
            }
        }

        [Test]
        public void GetItemDropSpawnY_UsesHalfCellHeightAboveGridOrigin()
        {
            var prevOrigin = ArenaGrid3D.GridOrigin;
            var prevCellSize = ArenaGrid3D.CellSize;
            try
            {
                ArenaGrid3D.GridOrigin = new UnityEngine.Vector3(0f, 10f, 0f);
                ArenaGrid3D.CellSize = 2f;

                Assert.AreEqual(11f, WallBlock3D.GetItemDropSpawnY(), 0.0001f); // 10 + 0.5*2
                Assert.AreEqual(11.25f, WallBlock3D.GetItemDropSpawnY(0.25f), 0.0001f);
            }
            finally
            {
                ArenaGrid3D.GridOrigin = prevOrigin;
                ArenaGrid3D.CellSize = prevCellSize;
            }
        }
    }

    public class HybridArenaGridRebindEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            var grids = Object.FindObjectsByType<HybridArenaGrid>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < grids.Length; i++)
            {
                if (grids[i] != null)
                    Object.DestroyImmediate(grids[i].gameObject);
            }
            ArenaGrid3D.GridOrigin = Vector3.zero;
        }

        [Test]
        public void RepublishGridOrigin_UsesCurrentDestructibleParentTransform()
        {
            var gridGo = new GameObject("HybridArenaGrid_RebindTest");
            var grid = gridGo.AddComponent<HybridArenaGrid>();
            var p1 = new GameObject("DestP1").transform;
            p1.SetParent(gridGo.transform, false);
            p1.localPosition = new Vector3(10f, 0f, 0f);
            var p2 = new GameObject("DestP2").transform;
            p2.SetParent(gridGo.transform, false);
            p2.localPosition = new Vector3(-5f, 0f, 3f);

            grid.destructibleWallsParent = p1;
            grid.gridOriginLocal = new Vector3(1f, 0f, 2f);
            grid.RepublishGridOrigin();
            Vector3 g1 = ArenaGrid3D.GridOrigin;

            grid.destructibleWallsParent = p2;
            grid.RepublishGridOrigin();
            Vector3 g2 = ArenaGrid3D.GridOrigin;

            Assert.That(g2.x, Is.Not.EqualTo(g1.x).Within(0.001f));
        }

        [Test]
        public void RecaptureBaselineAndRestoreLayout_EmptyDestructibleParent_DoesNotThrow()
        {
            var gridGo = new GameObject("HybridArenaGrid_RecaptureTest");
            var grid = gridGo.AddComponent<HybridArenaGrid>();
            var dest = new GameObject("EmptyDest").transform;
            dest.SetParent(gridGo.transform, false);
            grid.destructibleWallsParent = dest;
            grid.indestructibleWallsParent = null;
            grid.destructibleWallsLayoutPrefab = null;

            Assert.DoesNotThrow(() => grid.RecaptureBaselineAndRestoreLayout());
        }
    }
}

