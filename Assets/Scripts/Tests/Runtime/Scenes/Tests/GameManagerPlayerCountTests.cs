using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using HybridGame.MasterBlaster.Scripts.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HybridGame.MasterBlaster.Tests
{
    public class GameManagerPlayerCountTests
    {
        [UnityTest]
        public System.Collections.IEnumerator OnEnable_WhenPlayersPrefIs2_DisablesExtraPlayerSlots()
        {
            PlayerPrefs.SetInt("Players", 2);
            PlayerPrefs.Save();

            var gmGo = new GameObject("GameManager_Test");
            var playersRoot = new GameObject("PlayersRoot");

            try
            {
                // Create 5 player candidates that would otherwise remain active from a prior run.
                for (int i = 1; i <= 5; i++)
                {
                    var p = new GameObject($"P{i}");
                    p.transform.SetParent(playersRoot.transform);
                    p.SetActive(true);
                    var dual = p.AddComponent<PlayerDualModeController>();
                    dual.playerId = i;
                }

                gmGo.AddComponent<GameManager>(); // OnEnable runs immediately

                // Wait one frame for OnEnable to finish any coroutines it starts.
                yield return null;

                int activeCount = 0;
                var all = Object.FindObjectsByType<PlayerDualModeController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
                foreach (var d in all)
                {
                    if (d != null && d.gameObject.activeInHierarchy)
                        activeCount++;
                }

                Assert.That(activeCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(gmGo);
                Object.DestroyImmediate(playersRoot);
            }
        }
    }
}

