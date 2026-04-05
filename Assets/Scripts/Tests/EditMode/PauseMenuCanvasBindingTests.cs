using HybridGame.MasterBlaster.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace fps.Tests.EditMode
{
    [Category("PauseMenu")]
    public class PauseMenuCanvasBindingTests
    {
        [Test]
        public void TryBindMenuPanelWorldCamera_ScreenSpaceCamera_AssignsWorldCamera()
        {
            var go = new GameObject("MenuPanel");
            var canvas = go.AddComponent<Canvas>();
            var camA = new GameObject("CamA").AddComponent<Camera>();
            var camB = new GameObject("CamB").AddComponent<Camera>();
            canvas.worldCamera = camA;
            canvas.renderMode = global::UnityEngine.RenderMode.ScreenSpaceCamera;

            Assert.IsTrue(GlobalPauseMenuController.TryBindMenuPanelWorldCamera(go, camB));
            Assert.AreSame(camB, canvas.worldCamera);
        }

        [Test]
        public void TryBindMenuPanelWorldCamera_OverlayMode_DoesNotChange()
        {
            var go = new GameObject("MenuPanel");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = global::UnityEngine.RenderMode.ScreenSpaceOverlay;
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();

            Assert.IsFalse(GlobalPauseMenuController.TryBindMenuPanelWorldCamera(go, cam));
            Assert.IsNull(canvas.worldCamera);
        }

        [Test]
        public void TryBindMenuPanelWorldCamera_NullArgs_ReturnsFalse()
        {
            var go = new GameObject("MenuPanel");
            go.AddComponent<Canvas>().renderMode = global::UnityEngine.RenderMode.ScreenSpaceCamera;
            var cam = new GameObject("Cam").AddComponent<Camera>();

            Assert.IsFalse(GlobalPauseMenuController.TryBindMenuPanelWorldCamera(null, cam));
            Assert.IsFalse(GlobalPauseMenuController.TryBindMenuPanelWorldCamera(go, null));
        }
    }
}
