using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Player;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class BillboardSpriteOrientationEditModeTests
    {
        [Test]
        public void UseFixedTopDownStyle_Bomberman_AlwaysTrue()
        {
            Assert.IsTrue(BillboardSpriteOrientationMath.UseFixedTopDownStyle(
                GameModeManager.GameMode.Bomberman, cameraOrthographic: true));
            Assert.IsTrue(BillboardSpriteOrientationMath.UseFixedTopDownStyle(
                GameModeManager.GameMode.Bomberman, cameraOrthographic: false));
        }

        [Test]
        public void UseFixedTopDownStyle_ArenaPerspective_OrthographicTrue()
        {
            Assert.IsTrue(BillboardSpriteOrientationMath.UseFixedTopDownStyle(
                GameModeManager.GameMode.ArenaPerspective, cameraOrthographic: true));
        }

        [Test]
        public void UseFixedTopDownStyle_ArenaPerspective_PerspectiveFalse()
        {
            Assert.IsFalse(BillboardSpriteOrientationMath.UseFixedTopDownStyle(
                GameModeManager.GameMode.ArenaPerspective, cameraOrthographic: false));
        }

        [Test]
        public void NormalizeEulerX_60_Stays60()
        {
            Assert.AreEqual(60f, BillboardSpriteOrientationMath.NormalizeEulerX(60f), 0.001f);
        }

        [Test]
        public void NormalizeEulerX_270_BecomesNeg90()
        {
            Assert.AreEqual(-90f, BillboardSpriteOrientationMath.NormalizeEulerX(270f), 0.001f);
        }

        [Test]
        public void ComputePerspectiveGridEuler_BaseZero_Cam60_YieldsNeg60OnX()
        {
            var baseEuler = new Vector3(0f, 12f, 3f);
            var result = BillboardSpriteOrientationMath.ComputePerspectiveGridEuler(baseEuler, 60f);
            Assert.AreEqual(new Vector3(-60f, 12f, 3f), result);
        }

        [Test]
        public void ComputePerspectiveGridEuler_Base90_Cam60_Yields30OnX()
        {
            var baseEuler = new Vector3(90f, 0f, 0f);
            var result = BillboardSpriteOrientationMath.ComputePerspectiveGridEuler(baseEuler, 60f);
            Assert.AreEqual(new Vector3(30f, 0f, 0f), result);
        }

        [Test]
        public void TryComputeFpsCylindricalBillboardRotation_FixedPitchNeg90_ZeroRoll_CameraEast()
        {
            var sprite = Vector3.zero;
            var cam = new Vector3(5f, 10f, 0f);
            Assert.IsTrue(BillboardSpriteOrientationMath.TryComputeFpsCylindricalBillboardRotation(
                sprite, cam, out var q));
            var e = q.eulerAngles;
            Assert.Less(
                Mathf.Abs(BillboardSpriteOrientationMath.NormalizeEulerX(e.x) - -90f),
                1f,
                "X pitch should be -90° (vertical sprite plane).");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(e.z, 0f)), 1f, "Roll Z should be 0.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(e.y, 90f)), 1f, "Yaw should face +X (camera east).");
        }

        [Test]
        public void TryComputeFpsCylindricalBillboardRotation_TooCloseHorizontal_ReturnsFalse()
        {
            var p = Vector3.zero;
            Assert.IsFalse(BillboardSpriteOrientationMath.TryComputeFpsCylindricalBillboardRotation(
                p, p + new Vector3(0.01f, 0f, 0f), out _));
        }

        [Test]
        public void TryComputeFpsCylindricalBillboardRotation_CameraDirectlyAbove_ReturnsFalse()
        {
            Assert.IsFalse(BillboardSpriteOrientationMath.TryComputeFpsCylindricalBillboardRotation(
                Vector3.zero, new Vector3(0f, 5f, 0f), out _));
        }

        [Test]
        public void ComputeFpsBillboardRotation_NullCamera_ReturnsIdentity()
        {
            Assert.AreEqual(Quaternion.identity, BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(
                Vector3.zero, null));
        }

        [Test]
        public void ComputeFpsBillboardRotation_CameraEast_MatchesCylindricalYaw()
        {
            var camGo = new GameObject("CamTest");
            try
            {
                camGo.transform.position = new Vector3(5f, 10f, 0f);
                camGo.transform.rotation = Quaternion.identity;
                var q = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(Vector3.zero, camGo.transform);
                var e = q.eulerAngles;
                Assert.Less(
                    Mathf.Abs(BillboardSpriteOrientationMath.NormalizeEulerX(e.x) - -90f),
                    1f);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(e.z, 0f)), 1f);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(e.y, 90f)), 1f);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void ComputeFpsBillboardRotation_CameraDirectlyAbove_UsesCameraYaw()
        {
            var camGo = new GameObject("CamTest");
            try
            {
                camGo.transform.position = new Vector3(0f, 5f, 0f);
                camGo.transform.rotation = Quaternion.Euler(0f, 33f, 0f);
                var q = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(Vector3.zero, camGo.transform);
                var e = q.eulerAngles;
                Assert.Less(
                    Mathf.Abs(BillboardSpriteOrientationMath.NormalizeEulerX(e.x) - -90f),
                    1f);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(e.z, 0f)), 1f);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(e.y, 33f)), 1f);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void ComputeFpsBillboardRotation_TooCloseHorizontal_UsesCameraYaw()
        {
            var camGo = new GameObject("CamTest");
            try
            {
                camGo.transform.position = new Vector3(0.01f, 2f, 0f);
                camGo.transform.rotation = Quaternion.Euler(0f, -120f, 0f);
                var q = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(Vector3.zero, camGo.transform);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(q.eulerAngles.y, -120f)), 1f);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void ComputeFpsBillboardLookAtRotation_AlignsForwardTowardCamera()
        {
            var camGo = new GameObject("CamLookAtTest");
            try
            {
                camGo.transform.position = new Vector3(3f, 2f, 0f);
                var q = BillboardSpriteOrientationMath.ComputeFpsBillboardLookAtRotation(Vector3.zero, camGo.transform);
                Vector3 forward = q * Vector3.forward;
                Vector3 toCam = (camGo.transform.position - Vector3.zero).normalized;
                Assert.Greater(Vector3.Dot(forward, toCam), 0.99f);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }
    }
}
