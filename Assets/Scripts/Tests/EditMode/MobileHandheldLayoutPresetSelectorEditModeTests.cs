using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Mobile.Layout;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class MobileHandheldLayoutPresetSelectorEditModeTests
    {
        [Test]
        public void TrySelectExact_FindsMatchingPixels()
        {
            var a = new MobileHandheldLayoutPresetEntry { screenWidth = 1920, screenHeight = 1080, label = "a" };
            var b = new MobileHandheldLayoutPresetEntry { screenWidth = 1280, screenHeight = 720, label = "b" };
            var list = new List<MobileHandheldLayoutPresetEntry> { a, b };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TrySelectExact(list, 1280, 720, out var hit, out var idx));
            Assert.AreSame(b, hit);
            Assert.AreEqual(1, idx);
        }

        [Test]
        public void TrySelectNearestAspect_PicksClosestRatio()
        {
            var sixteenNine = new MobileHandheldLayoutPresetEntry { screenWidth = 1920, screenHeight = 1080, label = "16:9" };
            var fourThree = new MobileHandheldLayoutPresetEntry { screenWidth = 1024, screenHeight = 768, label = "4:3" };
            var list = new List<MobileHandheldLayoutPresetEntry> { sixteenNine, fourThree };

            Assert.IsTrue(
                MobileHandheldLayoutPresetSelector.TrySelectNearestAspect(list, 800, 450, out var hit, out _));
            Assert.AreSame(sixteenNine, hit);
        }

        [Test]
        public void TryBuildInterpolatedEntry_BracketsAspect()
        {
            var low = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 1000,
                screenHeight = 1000,
                label = "1:1",
                letterboxDesignWidth = 400,
                letterboxDesignHeight = 400,
            };
            var high = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 2000,
                screenHeight = 1000,
                label = "2:1",
                letterboxDesignWidth = 800,
                letterboxDesignHeight = 400,
            };
            var list = new List<MobileHandheldLayoutPresetEntry> { low, high };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TryBuildInterpolatedEntry(list, 1500, 1000, out var mid));
            Assert.NotNull(mid);
            Assert.Greater(mid.letterboxDesignWidth, low.letterboxDesignWidth);
            Assert.Less(mid.letterboxDesignWidth, high.letterboxDesignWidth);
        }
    }
}
