using HybridGame.MasterBlaster.Scripts.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HybridGame.MasterBlaster.Tests
{
    public class HybridArenaGridMultiSlotBaselineTests
    {
        [UnityTest]
        public System.Collections.IEnumerator RecaptureAfterSwitchingDestructibleParent_UsesSlotChildren_WhenLayoutPrefabNull()
        {
            var arenaGo = new GameObject("HybridArenaGrid_MultiSlot_Test");
            var slot0 = new GameObject("Slot0_D").transform;
            var slot1 = new GameObject("Slot1_D").transform;
            var iSlot = new GameObject("Indestructible").transform;
            slot0.SetParent(arenaGo.transform, false);
            slot1.SetParent(arenaGo.transform, false);
            iSlot.SetParent(arenaGo.transform, false);

            for (int i = 0; i < 2; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = $"S0_{i}";
                c.transform.SetParent(slot0, false);
            }

            for (int i = 0; i < 4; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = $"S1_{i}";
                c.transform.SetParent(slot1, false);
            }

            var grid = arenaGo.AddComponent<HybridArenaGrid>();
            grid.thinSceneDestructibles = false;
            grid.destructibleWallsParent = slot0;
            grid.indestructibleWallsParent = iSlot;
            grid.destructibleWallsLayoutPrefab = null;

            yield return null;

            Assert.That(slot0.childCount, Is.EqualTo(2));

            grid.destructibleWallsParent = slot1;
            grid.RecaptureBaselineAndRestoreLayout();

            Assert.That(slot1.childCount, Is.EqualTo(4));

            Object.DestroyImmediate(arenaGo);
        }
    }
}
