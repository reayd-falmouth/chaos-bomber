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
        public void TryComputeFpsBillboardRotation_ForwardPointsAtCamera()
        {
            var sprite = new Vector3(0f, 0f, 0f);
            var cam = new Vector3(3f, 2f, 0f);
            Assert.IsTrue(BillboardSpriteOrientationMath.TryComputeFpsBillboardRotation(
                sprite, cam, Vector3.up, out var q));
            var forward = q * Vector3.forward;
            var want = (cam - sprite).normalized;
            Assert.Less(Vector3.Angle(forward, want), 0.05f);
        }

        [Test]
        public void TryComputeFpsBillboardRotation_TooClose_ReturnsFalse()
        {
            var p = Vector3.zero;
            Assert.IsFalse(BillboardSpriteOrientationMath.TryComputeFpsBillboardRotation(
                p, p + new Vector3(0.01f, 0f, 0f), Vector3.up, out _));
        }
    }
}
