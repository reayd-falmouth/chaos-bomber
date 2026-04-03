using System.Collections;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.AvatarSelect;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Tests
{
    public class AvatarSelectMenuControllerPlayModeTests : InputTestFixture
    {
        sealed class TestMenu : AvatarSelectMenuController
        {
            public FlowState? LastRequestedState { get; private set; }

            protected override void RequestFlowNavigation(FlowState state)
            {
                LastRequestedState = state;
            }
        }

        static InputActionAsset CreateMenuInputAsset()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Player");
            var move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            map.AddAction("PlaceBomb", InputActionType.Button, "<Keyboard>/space");
            asset.AddActionMap(map);
            return asset;
        }

        [UnityTest]
        public IEnumerator SubmitOnSelectRow_PersistsAvatarAndRequestsLevelSelect()
        {
            PlayerPrefs.DeleteKey(AvatarSelectController.SelectedAvatarPrefsKey);

            var root = new GameObject("AvatarRoot");
            var image = root.AddComponent<Image>();
            var descGo = new GameObject("Desc");
            var descText = descGo.AddComponent<Text>();
            descGo.transform.SetParent(root.transform, false);

            root.SetActive(false);
            var avatar = root.AddComponent<AvatarController>();
            avatar.displayImage = image;
            avatar.descriptionText = descText;
            avatar.characters = new[]
            {
                new CharacterData { characterName = "A", characterDescription = "a", characterSprite = CreateSprite(Color.red) },
                new CharacterData { characterName = "B", characterDescription = "b", characterSprite = CreateSprite(Color.blue) }
            };
            var inputAsset = CreateMenuInputAsset();
            avatar.inputActions = inputAsset;
            root.SetActive(true);

            var menuGo = new GameObject("SelectMenu");
            menuGo.transform.SetParent(root.transform, false);
            menuGo.SetActive(false);
            var selPtr = new GameObject("SelPtr").AddComponent<Text>();
            selPtr.transform.SetParent(menuGo.transform, false);
            var backPtr = new GameObject("BackPtr").AddComponent<Text>();
            backPtr.transform.SetParent(menuGo.transform, false);

            var menu = menuGo.AddComponent<TestMenu>();
            menu.BindAvatarController(avatar);
            menu.ConfigureForTests(inputAsset, selPtr, backPtr, null, null);
            menuGo.SetActive(true);

            yield return null;

            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.rightArrowKey);
            yield return null;
            Release(keyboard.rightArrowKey);
            yield return null;
            Assert.That(avatar.CurrentIndex, Is.EqualTo(1));

            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return null;

            Assert.That(menu.LastRequestedState, Is.EqualTo(FlowState.LevelSelect));
            Assert.That(PlayerPrefs.GetInt(AvatarSelectController.SelectedAvatarPrefsKey), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SubmitOnBackRow_RequestsTitle()
        {
            var root = new GameObject("AvatarRoot");
            var image = root.AddComponent<Image>();
            var descGo = new GameObject("Desc");
            var descText = descGo.AddComponent<Text>();
            descGo.transform.SetParent(root.transform, false);

            root.SetActive(false);
            var avatar = root.AddComponent<AvatarController>();
            avatar.displayImage = image;
            avatar.descriptionText = descText;
            avatar.characters = new[]
            {
                new CharacterData { characterName = "A", characterDescription = "a", characterSprite = CreateSprite(Color.red) }
            };
            var inputAsset = CreateMenuInputAsset();
            avatar.inputActions = inputAsset;
            root.SetActive(true);

            var menuGo = new GameObject("SelectMenu");
            menuGo.transform.SetParent(root.transform, false);
            menuGo.SetActive(false);
            var selPtr = new GameObject("SelPtr").AddComponent<Text>();
            selPtr.transform.SetParent(menuGo.transform, false);
            var backPtr = new GameObject("BackPtr").AddComponent<Text>();
            backPtr.transform.SetParent(menuGo.transform, false);

            var menu = menuGo.AddComponent<TestMenu>();
            menu.BindAvatarController(avatar);
            menu.ConfigureForTests(inputAsset, selPtr, backPtr, null, null);
            menuGo.SetActive(true);

            yield return null;

            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.downArrowKey);
            yield return null;
            Release(keyboard.downArrowKey);
            yield return null;

            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return null;

            Assert.That(menu.LastRequestedState, Is.EqualTo(FlowState.Title));
        }

        [UnityTest]
        public IEnumerator SelectButtonClick_RequestsLevelSelect()
        {
            PlayerPrefs.DeleteKey(AvatarSelectController.SelectedAvatarPrefsKey);

            var root = new GameObject("AvatarRoot");
            var image = root.AddComponent<Image>();
            var descGo = new GameObject("Desc");
            var descText = descGo.AddComponent<Text>();
            descGo.transform.SetParent(root.transform, false);

            root.SetActive(false);
            var avatar = root.AddComponent<AvatarController>();
            avatar.displayImage = image;
            avatar.descriptionText = descText;
            avatar.characters = new[]
            {
                new CharacterData { characterName = "A", characterDescription = "a", characterSprite = CreateSprite(Color.red) }
            };
            var inputAsset = CreateMenuInputAsset();
            avatar.inputActions = inputAsset;
            root.SetActive(true);

            var menuGo = new GameObject("SelectMenu");
            menuGo.transform.SetParent(root.transform, false);
            menuGo.SetActive(false);
            var selPtr = new GameObject("SelPtr").AddComponent<Text>();
            selPtr.transform.SetParent(menuGo.transform, false);
            var backPtr = new GameObject("BackPtr").AddComponent<Text>();
            backPtr.transform.SetParent(menuGo.transform, false);
            var selectBtnGo = new GameObject("SelBtn");
            selectBtnGo.transform.SetParent(menuGo.transform, false);
            var selectImg = selectBtnGo.AddComponent<Image>();
            selectImg.color = new Color(0, 0, 0, 0.01f);
            var selectBtn = selectBtnGo.AddComponent<Button>();
            selectBtn.targetGraphic = selectImg;

            var menu = menuGo.AddComponent<TestMenu>();
            menu.BindAvatarController(avatar);
            menu.ConfigureForTests(inputAsset, selPtr, backPtr, selectBtn, null);
            menuGo.SetActive(true);

            yield return null;

            selectBtn.onClick.Invoke();

            Assert.That(menu.LastRequestedState, Is.EqualTo(FlowState.LevelSelect));
            Assert.That(PlayerPrefs.GetInt(AvatarSelectController.SelectedAvatarPrefsKey), Is.EqualTo(0));
        }

        static Sprite CreateSprite(Color c)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            tex.SetPixels(new[] { c, c, c, c });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
