using System.Collections;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.AvatarSelect;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Tests
{
    public class LevelSelectMenuControllerPlayModeTests : InputTestFixture
    {
        sealed class TestLevelMenu : LevelSelectMenuController
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
        public IEnumerator SubmitOnBackRow_RequestsAvatarSelect()
        {
            PlayerPrefs.DeleteKey(AvatarSelectController.SelectedAvatarPrefsKey);

            var parent = new GameObject("LevelSelectMenuParent");
            var inputAsset = CreateMenuInputAsset();

            var menuGo = new GameObject("SelectMenu");
            menuGo.transform.SetParent(parent.transform, false);
            menuGo.SetActive(false);
            var selPtr = new GameObject("SelPtr").AddComponent<Text>();
            selPtr.transform.SetParent(menuGo.transform, false);
            var backPtr = new GameObject("BackPtr").AddComponent<Text>();
            backPtr.transform.SetParent(menuGo.transform, false);

            var menu = menuGo.AddComponent<TestLevelMenu>();
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

            Assert.That(menu.LastRequestedState, Is.EqualTo(FlowState.AvatarSelect));
            Assert.That(PlayerPrefs.HasKey(AvatarSelectController.SelectedAvatarPrefsKey), Is.False);
        }

        [UnityTest]
        public IEnumerator SubmitOnSelectRow_RequestsCountdown_WithoutAvatar_NoPlayerPrefsWrite()
        {
            PlayerPrefs.DeleteKey(AvatarSelectController.SelectedAvatarPrefsKey);
            PlayerPrefs.DeleteKey(AvatarSelectionPrefs.PlayerDisplayNameKey);
            PlayerPrefs.DeleteKey(AvatarSelectionPrefs.AvatarStartingPerkKey);
            PlayerPrefs.DeleteKey(AvatarSelectionPrefs.Player1SpriteBlockKey);

            var parent = new GameObject("LevelSelectMenuParent");
            var inputAsset = CreateMenuInputAsset();

            var menuGo = new GameObject("SelectMenu");
            menuGo.transform.SetParent(parent.transform, false);
            menuGo.SetActive(false);
            var selPtr = new GameObject("SelPtr").AddComponent<Text>();
            selPtr.transform.SetParent(menuGo.transform, false);
            var backPtr = new GameObject("BackPtr").AddComponent<Text>();
            backPtr.transform.SetParent(menuGo.transform, false);

            var menu = menuGo.AddComponent<TestLevelMenu>();
            menu.ConfigureForTests(inputAsset, selPtr, backPtr, null, null);
            menuGo.SetActive(true);

            yield return null;

            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return null;

            Assert.That(menu.LastRequestedState, Is.EqualTo(FlowState.Countdown));
            Assert.That(PlayerPrefs.HasKey(AvatarSelectController.SelectedAvatarPrefsKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(AvatarSelectionPrefs.PlayerDisplayNameKey), Is.False);
        }
    }
}
