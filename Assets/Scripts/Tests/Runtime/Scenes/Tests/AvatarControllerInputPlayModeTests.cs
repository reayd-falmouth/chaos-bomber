using System.Collections;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Tests
{
    public class AvatarControllerInputPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator LeftRightCyclesCharacters_SubmitDoesNotAdvanceFlow()
        {
            // Arrange — defer Awake until inputActions is assigned (Awake reads the asset).
            var go = new GameObject("AvatarController");
            var image = go.AddComponent<Image>();
            var descTextGo = new GameObject("DescText");
            var descText = descTextGo.AddComponent<Text>();
            descTextGo.transform.SetParent(go.transform, false);

            go.SetActive(false);
            var controller = go.AddComponent<AvatarController>();
            controller.displayImage = image;
            controller.descriptionText = descText;

            controller.characters = new[]
            {
                new CharacterData
                {
                    characterName = "A",
                    characterDescription = "A-desc",
                    characterSprite = CreateSprite(Color.red)
                },
                new CharacterData
                {
                    characterName = "B",
                    characterDescription = "B-desc",
                    characterSprite = CreateSprite(Color.green)
                }
            };

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Player");
            var move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            map.AddAction("PlaceBomb", InputActionType.Button, "<Keyboard>/space");
            asset.AddActionMap(map);

            controller.inputActions = asset;
            go.SetActive(true);

            yield return null;

            var keyboard = InputSystem.AddDevice<Keyboard>();

            Assert.That(controller.displayImage.sprite, Is.EqualTo(controller.characters[0].characterSprite));

            Press(keyboard.rightArrowKey);
            yield return null;
            Release(keyboard.rightArrowKey);
            yield return null;

            Assert.That(controller.displayImage.sprite, Is.EqualTo(controller.characters[1].characterSprite));

            Press(keyboard.leftArrowKey);
            yield return null;
            Release(keyboard.leftArrowKey);
            yield return null;
            Assert.That(controller.displayImage.sprite, Is.EqualTo(controller.characters[0].characterSprite));

            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return null;

            Assert.That(controller.CurrentIndex, Is.EqualTo(0));
        }

        private static Sprite CreateSprite(Color c)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            tex.SetPixels(new[] { c, c, c, c });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
