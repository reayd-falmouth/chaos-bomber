using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class AvatarControllerSpriteBlockEditModeTests
    {
        [Test]
        public void GetCurrentSpriteSheetBlockIndex_UsesExplicitBlockWhenNonNegative()
        {
            var go = new GameObject("AvatarTest");
            var ac = go.AddComponent<AvatarController>();
            ac.characters = new[]
            {
                new CharacterData { characterName = "X", spriteSheetBlockIndex = 3 }
            };

            Assert.AreEqual(3, ac.GetCurrentSpriteSheetBlockIndex());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetCurrentSpriteSheetBlockIndex_FallsBackToListIndexWhenUnset()
        {
            var go = new GameObject("AvatarTest");
            var ac = go.AddComponent<AvatarController>();
            ac.characters = new[]
            {
                new CharacterData { characterName = "A", spriteSheetBlockIndex = -1 },
                new CharacterData { characterName = "B", spriteSheetBlockIndex = -1 }
            };
            ac.NextCharacter();

            Assert.AreEqual(1, ac.GetCurrentSpriteSheetBlockIndex());

            Object.DestroyImmediate(go);
        }
    }
}
