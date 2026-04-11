using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Player;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class BillboardSpriteCameraHelperEditModeTests
    {
        private GameObject m_MainCamGo;
        private GameObject m_SecondaryCamGo;

        [TearDown]
        public void TearDown()
        {
            if (m_MainCamGo != null)
                Object.DestroyImmediate(m_MainCamGo);
            if (m_SecondaryCamGo != null)
                Object.DestroyImmediate(m_SecondaryCamGo);
            m_MainCamGo = null;
            m_SecondaryCamGo = null;
        }

        [Test]
        public void GetFpsBillboardCameraTransform_WhenMainExists_UsesMainTransform()
        {
            m_MainCamGo = new GameObject("MainCamForBillboardTest");
            m_MainCamGo.tag = "MainCamera";
            m_MainCamGo.AddComponent<Camera>();

            m_SecondaryCamGo = new GameObject("SecondaryCamForBillboardTest");
            var secondary = m_SecondaryCamGo.AddComponent<Camera>();

            Transform t = BillboardSpriteCameraHelper.GetFpsBillboardCameraTransform(secondary);
            Assert.IsNotNull(t);
            Assert.AreSame(Camera.main.transform, t);
        }

        [Test]
        public void TryResolveBillboardCamera_WithTaggedMainCamera_ReturnsNonNullCamera()
        {
            m_MainCamGo = new GameObject("MainCamForResolveTest");
            m_MainCamGo.tag = "MainCamera";
            m_MainCamGo.AddComponent<Camera>();

            bool ok = BillboardSpriteCameraHelper.TryResolveBillboardCamera(
                GameModeManager.GameMode.FPS, out UnityEngine.Camera cam);

            Assert.IsTrue(ok);
            Assert.IsNotNull(cam);
        }

        [Test]
        public void UseTopDownGridBillboardRotation_FpsMode_IsFalse()
        {
            Assert.IsFalse(BillboardSpriteCameraHelper.UseTopDownGridBillboardRotation(GameModeManager.GameMode.FPS));
        }
    }
}
