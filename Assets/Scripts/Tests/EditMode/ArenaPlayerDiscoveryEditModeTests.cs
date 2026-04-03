using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class ArenaPlayerDiscoveryEditModeTests
    {
        [Test]
        public void SlotResolver_Count2_ReturnsTopLeftThenBottomRight()
        {
            var tl = new GameObject("TL");
            var tr = new GameObject("TR");
            var bl = new GameObject("BL");
            var br = new GameObject("BR");
            var mid = new GameObject("Mid");
            var result = new GameObject[2];

            try
            {
                Assert.That(
                    ArenaPlayerSlotResolver.TryResolve(2, tl, tr, bl, br, mid, result),
                    Is.True
                );
                Assert.That(result[0], Is.SameAs(tl));
                Assert.That(result[1], Is.SameAs(br));
            }
            finally
            {
                Object.DestroyImmediate(tl);
                Object.DestroyImmediate(tr);
                Object.DestroyImmediate(bl);
                Object.DestroyImmediate(br);
                Object.DestroyImmediate(mid);
            }
        }

        [Test]
        public void SlotResolver_Count5_IncludesMiddleLast()
        {
            var tl = new GameObject("TL");
            var tr = new GameObject("TR");
            var bl = new GameObject("BL");
            var br = new GameObject("BR");
            var mid = new GameObject("Mid");
            var result = new GameObject[5];

            try
            {
                Assert.That(
                    ArenaPlayerSlotResolver.TryResolve(5, tl, tr, bl, br, mid, result),
                    Is.True
                );
                Assert.That(result[0], Is.SameAs(tl));
                Assert.That(result[1], Is.SameAs(tr));
                Assert.That(result[2], Is.SameAs(bl));
                Assert.That(result[3], Is.SameAs(br));
                Assert.That(result[4], Is.SameAs(mid));
            }
            finally
            {
                Object.DestroyImmediate(tl);
                Object.DestroyImmediate(tr);
                Object.DestroyImmediate(bl);
                Object.DestroyImmediate(br);
                Object.DestroyImmediate(mid);
            }
        }

        [Test]
        public void SlotResolver_NullRequiredSlot_ReturnsFalse()
        {
            var tl = new GameObject("TL");
            var result = new GameObject[2];

            try
            {
                Assert.That(
                    ArenaPlayerSlotResolver.TryResolve(2, tl, null, null, null, null, result),
                    Is.False
                );
            }
            finally
            {
                Object.DestroyImmediate(tl);
            }
        }

        /// <summary>
        /// Flat floor: identical local Y; corners differ in Z. Old Y-only row logic left bottomRow empty.
        /// </summary>
        [Test]
        public void SpatialFallback_SameYDifferentZ_FourCorners_YieldsFourDistinctPlayers()
        {
            var goTl = new GameObject("P_TL");
            var goTr = new GameObject("P_TR");
            var goBl = new GameObject("P_BL");
            var goBr = new GameObject("P_BR");
            goTl.transform.localPosition = new Vector3(-8f, 0f, 6f);
            goTr.transform.localPosition = new Vector3(8f, 0f, 6f);
            goBl.transform.localPosition = new Vector3(-8f, 0f, -4f);
            goBr.transform.localPosition = new Vector3(8f, 0f, -4f);

            var candidates = new List<ArenaPlayerSpatialFallback.Candidate>
            {
                new ArenaPlayerSpatialFallback.Candidate { go = goTl, localPos = goTl.transform.localPosition },
                new ArenaPlayerSpatialFallback.Candidate { go = goTr, localPos = goTr.transform.localPosition },
                new ArenaPlayerSpatialFallback.Candidate { go = goBl, localPos = goBl.transform.localPosition },
                new ArenaPlayerSpatialFallback.Candidate { go = goBr, localPos = goBr.transform.localPosition }
            };

            var result = new GameObject[4];
            try
            {
                ArenaPlayerSpatialFallback.FillResult(candidates, 4, result);
                Assert.That(result[0], Is.SameAs(goTl));
                Assert.That(result[1], Is.SameAs(goTr));
                Assert.That(result[2], Is.SameAs(goBl));
                Assert.That(result[3], Is.SameAs(goBr));
            }
            finally
            {
                Object.DestroyImmediate(goTl);
                Object.DestroyImmediate(goTr);
                Object.DestroyImmediate(goBl);
                Object.DestroyImmediate(goBr);
            }
        }
    }
}
