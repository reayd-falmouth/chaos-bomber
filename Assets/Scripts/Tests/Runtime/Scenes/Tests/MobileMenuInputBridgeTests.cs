using HybridGame.MasterBlaster.Scripts.Mobile;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests
{
    public class MobileMenuInputBridgeTests
    {
        [Test]
        public void MergeBombermanGridMoveCore_WhenNoMerge_UsesMoveActionBeforeIp()
        {
            Vector2 r = MobileMenuInputBridge.MergeBombermanGridMoveCore(
                mergeOverlayUi: false,
                overlayDigital: Vector2.up,
                moveActionValue: new Vector2(0.6f, 0f),
                ipMoveDirection: Vector2.up
            );
            Assert.That(r.x, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(r.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void MergeBombermanGridMoveCore_WhenMergeAndDigitalNonZero_IgnoresNoisyMoveAction()
        {
            Vector2 r = MobileMenuInputBridge.MergeBombermanGridMoveCore(
                mergeOverlayUi: true,
                overlayDigital: Vector2.right,
                moveActionValue: new Vector2(0.4f, 0.15f),
                ipMoveDirection: Vector2.zero
            );
            Assert.That(r, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void MergeBombermanGridMoveCore_WhenMergeAndDigitalZero_FallsBackToMoveThenIp()
        {
            Vector2 r = MobileMenuInputBridge.MergeBombermanGridMoveCore(
                mergeOverlayUi: true,
                overlayDigital: Vector2.zero,
                moveActionValue: new Vector2(0.08f, 0f),
                ipMoveDirection: Vector2.up
            );
            Assert.That(r, Is.EqualTo(Vector2.up));
        }
    }
}
