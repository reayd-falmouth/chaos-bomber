using HybridGame.MasterBlaster.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class UiCanvasCameraBinderEditModeTests
    {
        [Test]
        public void ApplyUiCameraToSubtree_NullRoot_DoesNotThrow()
        {
            var camGo = new GameObject("UiCanvasCameraBinderTests_Cam");
            var cam = camGo.AddComponent<Camera>();
            Assert.DoesNotThrow(() => UiCanvasCameraBinder.ApplyUiCameraToSubtree(null, cam));
            Object.DestroyImmediate(camGo);
        }

        [Test]
        public void ApplyUiCameraToSubtree_NullCamera_DoesNotThrow()
        {
            var go = new GameObject("UiCanvasCanvasBinderTests_Canvas", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Assert.DoesNotThrow(() => UiCanvasCameraBinder.ApplyUiCameraToSubtree(canvas, null));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyUiCameraToSubtree_SkipsOverlayChildren()
        {
            var rootGo = new GameObject("UiCanvasCameraBinderTests_Root", typeof(RectTransform));
            var camGo = new GameObject("UiCam");
            camGo.transform.SetParent(rootGo.transform, false);
            var uiCam = camGo.AddComponent<Camera>();

            var rootCanvas = rootGo.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var overlayChildGo = new GameObject("OverlayChild", typeof(RectTransform));
            overlayChildGo.transform.SetParent(rootGo.transform, false);
            var overlayChild = overlayChildGo.AddComponent<Canvas>();
            overlayChild.renderMode = RenderMode.ScreenSpaceOverlay;

            Assert.DoesNotThrow(() => UiCanvasCameraBinder.ApplyUiCameraToSubtree(rootCanvas, uiCam));

            Object.DestroyImmediate(rootGo);
        }
    }
}
