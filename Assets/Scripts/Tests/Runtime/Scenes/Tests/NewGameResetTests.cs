using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Player;
using NUnit.Framework;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.TestTools;

namespace HybridGame.MasterBlaster.Tests
{
    public class NewGameResetTests
    {
        [UnityTest]
        public System.Collections.IEnumerator ResetPlayersForNewGame_RevivesAndRespawns()
        {
            PlayerPrefs.SetInt("Players", 1);
            PlayerPrefs.SetInt("NewGamePending", 1);
            PlayerPrefs.Save();

            var sessionGo = new GameObject("SessionManager_Test");
            sessionGo.AddComponent<SessionManager>().Initialize(1);

            var player = new GameObject("P1");
            player.SetActive(true);
            var dual = player.AddComponent<PlayerDualModeController>();
            dual.playerId = 1;
            var health = player.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 0f;
            health.Kill();

            var gmGo = new GameObject("GameManager_Test");
            var gm = gmGo.AddComponent<HybridGame.MasterBlaster.Scripts.Scenes.Arena.GameManager>();

            // Ensure player is discoverable
            yield return null;

            // After GameManager.OnEnable, NewGamePending should be consumed and health restored
            Assert.That(player.activeInHierarchy, Is.True);
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));

            Object.DestroyImmediate(gmGo);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(sessionGo);
        }

        [UnityTest]
        public System.Collections.IEnumerator HybridArenaGrid_RestoreFromBaseline_RecreatesWalls()
        {
            var arenaGo = new GameObject("HybridArenaGrid_Test");
            var parent = new GameObject("DestructibleParent").transform;
            parent.SetParent(arenaGo.transform);

            // Create baseline walls in scene (generateOnStart=false path)
            for (int i = 0; i < 3; i++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.name = $"Wall_{i}";
                w.transform.SetParent(parent);
                w.AddComponent<WallBlock3D>();
            }

            var grid = arenaGo.AddComponent<HybridArenaGrid>();
            grid.generateOnStart = false;
            grid.thinSceneDestructibles = false;
            grid.destructibleWallsParent = parent;
            grid.indestructibleWallsParent = null;

            // Let Start() capture baseline
            yield return null;

            // Simulate destruction: clear all
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            Assert.That(parent.childCount, Is.EqualTo(0));

            grid.RestoreDestructiblesFromBaselineThenRethinAndRebuild();

            // Restored
            Assert.That(parent.childCount, Is.EqualTo(3));

            Object.DestroyImmediate(arenaGo);
        }
    }
}

