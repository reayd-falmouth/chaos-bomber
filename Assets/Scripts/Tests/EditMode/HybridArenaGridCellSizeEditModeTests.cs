using HybridGame.MasterBlaster.Scripts.Arena;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class HybridArenaGridCellSizeEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            ArenaGrid3D.CellSize = 1f;
            ArenaGrid3D.GridOrigin = Vector3.zero;
        }

        [Test]
        public void RepublishGridOrigin_UnscaledParent_KeepsUnitCellSize()
        {
            var go = new GameObject("GridRoot");
            var grid = go.AddComponent<HybridArenaGrid>();
            grid.destructibleWallsParent = go.transform;
            grid.gridOriginLocal = Vector3.zero;
            grid.RepublishGridOrigin();
            Assert.AreEqual(1f, ArenaGrid3D.CellSize, 1e-4f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RepublishGridOrigin_ScaledParent_UpdatesCellSizeToWorldStep()
        {
            var go = new GameObject("GridRoot");
            go.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            var grid = go.AddComponent<HybridArenaGrid>();
            grid.destructibleWallsParent = go.transform;
            grid.gridOriginLocal = Vector3.zero;
            grid.RepublishGridOrigin();
            Assert.AreEqual(2.5f, ArenaGrid3D.CellSize, 1e-3f);
            Object.DestroyImmediate(go);
        }
    }
}
