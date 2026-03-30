using NUnit.Framework;
using HybridGame.MasterBlaster.Scripts.Player.Abilities;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests
{
    public class GhostAbilityTests
    {
        [Test]
        public void Activate_WhenGhostGameObjectIsInactive_StillExcludesDestructibleWallsAndBombs()
        {
            var player = new GameObject("Player");
            try
            {
                var cc = player.AddComponent<CharacterController>();
                player.AddComponent<HybridGame.MasterBlaster.Scripts.Player.PlayerDualModeController>();

                var ghostGo = new GameObject("Ghost");
                ghostGo.transform.SetParent(player.transform);
                ghostGo.SetActive(false); // Ensure Ghost.Awake() does not run before we call Activate().

                var ghost = ghostGo.AddComponent<Ghost>();

                ghost.Activate(duration: 0.1f);

                int expectedMask = LayerMask.GetMask("DestructibleWall", "Bomb3D");
                Assert.That((cc.excludeLayers & expectedMask) == expectedMask);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}

