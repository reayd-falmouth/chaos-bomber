using HybridGame.MasterBlaster.Scripts.Arena;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class WallBlock3DItemDropSpawnTests
    {
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
}

