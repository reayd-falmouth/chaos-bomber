using HybridGame.MasterBlaster.Scripts.Mobile;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace fps.Tests.EditMode
{
    public class MobileHoldButtonEditModeTests
    {
        [Test]
        public void MobileHoldButton_RefreshRaycastTarget_FullyTransparentImage_DisablesCullTransparentMesh()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            var go = new GameObject("MobileHoldButtonTest");
            go.transform.SetParent(canvasGo.transform, false);
            try
            {
                var image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;

                var canvasRenderer = go.GetComponent<CanvasRenderer>();
                Assert.IsNotNull(canvasRenderer, "Image should add CanvasRenderer.");
                canvasRenderer.cullTransparentMesh = true;

                var hold = go.AddComponent<MobileHoldButton>();
                // Edit Mode tests may not run Awake/OnEnable on ad-hoc GameObjects; runtime always does.
                hold.RefreshRaycastTarget();

                Assert.IsFalse(
                    canvasRenderer.cullTransparentMesh,
                    "Fully transparent hit targets must not be culled or raycasts can miss this graphic.");
                Assert.Greater(
                    image.color.a,
                    0f,
                    "Alpha 0 images should get a minimal alpha so the graphic receives raycasts.");
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void MobileHoldButton_RefreshRaycastTarget_DisablesDecorativeDPadRootRaycast()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            var dpadRoot = new GameObject("DPadRoot");
            dpadRoot.transform.SetParent(canvasGo.transform, false);
            dpadRoot.AddComponent<RectTransform>();
            var rootImage = dpadRoot.AddComponent<Image>();
            rootImage.raycastTarget = true;

            var go = new GameObject("Up");
            go.transform.SetParent(dpadRoot.transform, false);
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            var hold = go.AddComponent<MobileHoldButton>();
            hold.RefreshRaycastTarget();

            Assert.IsFalse(
                rootImage.raycastTarget,
                "Opaque DPadRoot decoration must not steal raycasts from child MobileHoldButton tiles.");
        }
    }
}
