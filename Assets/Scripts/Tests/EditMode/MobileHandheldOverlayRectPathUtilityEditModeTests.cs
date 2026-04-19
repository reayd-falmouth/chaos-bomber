using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Mobile.Layout;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class MobileHandheldOverlayRectPathUtilityEditModeTests
    {
        [Test]
        public void TryParseSegment_ParsesIndexAndName()
        {
            Assert.IsTrue(MobileHandheldOverlayRectPathUtility.TryParseSegment("2-My-Button", out var idx, out var name));
            Assert.AreEqual(2, idx);
            Assert.AreEqual("My-Button", name);
        }

        [Test]
        public void BuildIndexedPath_RoundTripsUnderRoot()
        {
            var rootGo = new GameObject("Root", typeof(RectTransform));
            var root = rootGo.GetComponent<RectTransform>();
            var childGo = new GameObject("Child", typeof(RectTransform));
            var child = childGo.GetComponent<RectTransform>();
            child.SetParent(root, false);

            var path = MobileHandheldOverlayRectPathUtility.BuildIndexedPath(root, child);
            Assert.AreEqual("0-Child", path);

            var resolved = MobileHandheldOverlayRectPathUtility.TryResolvePath(root, path);
            Assert.AreSame(child, resolved);

            Object.DestroyImmediate(rootGo);
        }

        [Test]
        public void CaptureDescendants_ExcludesRootAndExcludeRefs()
        {
            var rootGo = new GameObject("Root", typeof(RectTransform));
            var root = rootGo.GetComponent<RectTransform>();
            var a = new GameObject("A", typeof(RectTransform)).GetComponent<RectTransform>();
            var b = new GameObject("B", typeof(RectTransform)).GetComponent<RectTransform>();
            a.SetParent(root, false);
            b.SetParent(root, false);

            var captured = MobileHandheldOverlayRectPathUtility.CaptureDescendants(root, true, new[] { b });
            Assert.AreEqual(1, captured.Length);
            Assert.AreEqual("0-A", captured[0].relativePath);

            Object.DestroyImmediate(rootGo);
        }

        [Test]
        public void InterpolateDescendantRows_LerpsMatchingPaths()
        {
            var lower = new[]
            {
                new MobileHandheldOverlayRectPathEntry
                {
                    relativePath = "0-A",
                    rect = new MobileHandheldRectSnapshot { anchoredPosition = Vector2.zero },
                },
            };
            var upper = new[]
            {
                new MobileHandheldOverlayRectPathEntry
                {
                    relativePath = "0-A",
                    rect = new MobileHandheldRectSnapshot { anchoredPosition = new Vector2(100f, 0f) },
                },
            };

            var mid = MobileHandheldOverlayRectPathUtility.InterpolateDescendantRows(lower, upper, 0.5f);
            Assert.AreEqual(1, mid.Length);
            Assert.AreEqual(50f, mid[0].rect.anchoredPosition.x, 1e-5f);
        }

        [Test]
        public void TryBuildInterpolatedEntry_IncludesDescendantInterpolation()
        {
            var low = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 1000,
                screenHeight = 1000,
                label = "1:1",
                overlayDescendantRects = new[]
                {
                    new MobileHandheldOverlayRectPathEntry
                    {
                        relativePath = "0-X",
                        rect = new MobileHandheldRectSnapshot { anchoredPosition = Vector2.zero },
                    },
                },
            };
            var high = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 2000,
                screenHeight = 1000,
                label = "2:1",
                overlayDescendantRects = new[]
                {
                    new MobileHandheldOverlayRectPathEntry
                    {
                        relativePath = "0-X",
                        rect = new MobileHandheldRectSnapshot { anchoredPosition = new Vector2(20f, 0f) },
                    },
                },
            };
            var list = new List<MobileHandheldLayoutPresetEntry> { low, high };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TryBuildInterpolatedEntry(list, 1500, 1000, out var mid));
            Assert.NotNull(mid);
            Assert.IsNotNull(mid.overlayDescendantRects);
            Assert.AreEqual(1, mid.overlayDescendantRects.Length);
            Assert.AreEqual(10f, mid.overlayDescendantRects[0].rect.anchoredPosition.x, 1e-5f);
        }
    }
}
