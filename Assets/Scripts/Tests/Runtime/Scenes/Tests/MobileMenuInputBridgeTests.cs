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

        /// <summary>
        /// MergeBombermanGridMove(arenaPlayerId != 1) zeros overlay before Core; same result as merge with zero overlay.
        /// </summary>
        [Test]
        public void MergeBombermanGridMoveCore_NonPlayer1OverlaySuppressed_MatchesZeroOverlayUnderMerge()
        {
            Vector2 move = new Vector2(0.7f, 0f);
            Vector2 withP1Overlay = MobileMenuInputBridge.MergeBombermanGridMoveCore(
                mergeOverlayUi: true,
                overlayDigital: Vector2.right,
                moveActionValue: move,
                ipMoveDirection: Vector2.zero);
            Assert.That(withP1Overlay, Is.EqualTo(Vector2.right));

            Vector2 nonP1Equivalent = MobileMenuInputBridge.MergeBombermanGridMoveCore(
                mergeOverlayUi: true,
                overlayDigital: Vector2.zero,
                moveActionValue: move,
                ipMoveDirection: Vector2.zero);
            Assert.That(nonP1Equivalent, Is.EqualTo(move));
        }

        [Test]
        public void TryVerticalMenuNavUp_CrossesThreshold_ReturnsTrue()
        {
            Assert.That(
                MobileMenuInputBridge.TryVerticalMenuNavUp(Vector2.zero, new Vector2(0f, 1f), 0.5f),
                Is.True);
        }

        [Test]
        public void TryVerticalMenuNavDown_CrossesThreshold_ReturnsTrue()
        {
            Assert.That(
                MobileMenuInputBridge.TryVerticalMenuNavDown(Vector2.zero, new Vector2(0f, -1f), 0.5f),
                Is.True);
        }

        [Test]
        public void TryVerticalMenuNav_NoEdgeWhileHeld_DoesNotRepeat()
        {
            var held = new Vector2(0f, 1f);
            Assert.That(MobileMenuInputBridge.TryVerticalMenuNavUp(held, held, 0.5f), Is.False);
        }
    }
}
