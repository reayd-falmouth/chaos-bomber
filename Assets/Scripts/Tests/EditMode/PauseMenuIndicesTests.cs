using NUnit.Framework;
using HybridGame.MasterBlaster.Scripts.Core;

namespace fps.Tests.EditMode
{
    /// <summary>
    /// <see cref="GlobalPauseMenuController"/> uses <c>options.Length == 6</c> for the full pause/settings menu:
    /// Master, SFX, Music, Scanlines, Show controls, Exit (value rows are indices 0–3).
    /// </summary>
    [Category("PauseMenu")]
    public class PauseMenuIndicesTests
    {
        [Test]
        public void ComputeMenuIndices_MapsLastTwoRowsToShowControlsAndExit()
        {
            var (showControlsIndex, exitIndex, valueRowCount) = GlobalPauseMenuController.ComputeMenuIndices(6);

            Assert.AreEqual(4, showControlsIndex);
            Assert.AreEqual(5, exitIndex);
            Assert.AreEqual(4, valueRowCount);
        }

        [Test]
        public void ComputeMenuIndices_WithMinimalRows_DoesNotThrow()
        {
            var (showControlsIndex, exitIndex, valueRowCount) = GlobalPauseMenuController.ComputeMenuIndices(2);

            Assert.AreEqual(0, showControlsIndex);
            Assert.AreEqual(1, exitIndex);
            Assert.AreEqual(0, valueRowCount);
        }
    }
}

