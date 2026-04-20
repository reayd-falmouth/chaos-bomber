using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Mobile;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace fps.Tests.EditMode
{
    /// <summary>
    /// Guards Bomberman drive gate for handheld touch (see <see cref="PlayerDualModeController.CanDriveBombermanLocally"/>).
    /// </summary>
    public class PlayerDualModeControllerBombermanDriveEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            MobileOverlayBootstrap.SetPreviewOverlayState(false, false);
        }

        [Test]
        public void CanDriveBombermanLocally_BadKeyboardFlags_WithMobilePlayerInputAndHandheldPreview_CanDrive()
        {
            var go = new GameObject(
                "PlayerDriveTest",
                typeof(CharacterController),
                typeof(PlayerDualModeController),
                typeof(MobilePlayerInput));
            try
            {
                var dual = go.GetComponent<PlayerDualModeController>();
                dual.playerId = 1;
                ApplyBombermanKeyboardSerialized(dual, receiveShared: false, ownerId: 2);

                MobileOverlayBootstrap.SetPreviewOverlayState(true, false);
                Assert.IsTrue(
                    MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput(),
                    "Handheld preview should enable the same merge gate as Android/iOS.");

                Assert.IsTrue(
                    dual.CanDriveBombermanLocally(),
                    "Player 1 with MobilePlayerInput should drive Bomberman when merge UI is active, even if prefab keyboard flags are wrong.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CanDriveBombermanLocally_BadKeyboardFlags_WithMobilePlayerInput_NoPreview_CannotDriveWithoutSessionDevice()
        {
            MobileOverlayBootstrap.SetPreviewOverlayState(false, false);
            var go = new GameObject(
                "PlayerDriveTest",
                typeof(CharacterController),
                typeof(PlayerDualModeController),
                typeof(MobilePlayerInput));
            try
            {
                var dual = go.GetComponent<PlayerDualModeController>();
                dual.playerId = 1;
                ApplyBombermanKeyboardSerialized(dual, receiveShared: false, ownerId: 2);

                if (SessionManager.Instance != null
                    && SessionManager.Instance.GetAssignedDevice(1).HasValue)
                {
                    Assert.Ignore("SessionManager assigned a device to player 1; HasSessionAssignedDevice would make CanDrive true unrelated to touch path.");
                }

                Assert.IsFalse(
                    dual.CanDriveBombermanLocally(),
                    "Without merge UI and without session device, bad keyboard flags should not allow local drive.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CanDriveBombermanLocally_DefaultKeyboardFlags_Player1_CanDriveWithoutPreview()
        {
            MobileOverlayBootstrap.SetPreviewOverlayState(false, false);
            var go = new GameObject("PlayerDriveTest", typeof(CharacterController), typeof(PlayerDualModeController));
            try
            {
                var dual = go.GetComponent<PlayerDualModeController>();
                dual.playerId = 1;
                ApplyBombermanKeyboardSerialized(dual, receiveShared: true, ownerId: 1);

                Assert.IsTrue(
                    dual.CanDriveBombermanLocally(),
                    "Single-player keyboard owner (player 1) should drive without handheld preview.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void ApplyBombermanKeyboardSerialized(PlayerDualModeController dual, bool receiveShared, int ownerId)
        {
            var so = new SerializedObject(dual);
            so.FindProperty("receiveSharedKeyboardInput").boolValue = receiveShared;
            so.FindProperty("bombermanKeyboardOwnerPlayerId").intValue = ownerId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
