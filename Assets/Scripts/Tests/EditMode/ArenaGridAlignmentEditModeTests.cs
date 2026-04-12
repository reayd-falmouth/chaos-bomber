using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Arena;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class ArenaGridAlignmentEditModeTests
    {
        [Test]
        public void AnalyzeWorldSamples_MinCellZero_SuggestedDeltaZero()
        {
            var origin = new Vector3(-8f, 0f, -6f);
            // Integer XZ locals so WorldToCell matches wall snaps on the 1×1 grid (see HybridArenaGrid.BuildGrid).
            var samples = new List<Vector3>
            {
                new Vector3(-8f, 0f, -6f),
                new Vector3(10f, 0f, 8f),
            };
            var report = ArenaGridAlignment.AnalyzeWorldSamples(samples, origin, 1f, 19, 15);
            Assert.AreEqual(2, report.SampleCount);
            Assert.AreEqual(Vector2Int.zero, report.MinCell);
            Assert.AreEqual(new Vector2Int(18, 14), report.MaxCell);
            Assert.AreEqual(0, report.OutOfBoundsCount);
            Assert.That(report.SuggestedGridOriginWorldDelta.sqrMagnitude, Is.LessThan(1e-6f));
        }

        [Test]
        public void AnalyzeWorldSamples_ShiftedOneCell_SuggestedDeltaMatchesCellSize()
        {
            var origin = new Vector3(0f, 0f, 0f);
            var samples = new List<Vector3>
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(2f, 0f, 1f),
            };
            var report = ArenaGridAlignment.AnalyzeWorldSamples(samples, origin, 1f, 19, 15);
            Assert.AreEqual(new Vector2Int(1, 0), report.MinCell);
            Assert.AreEqual(
                new Vector3(1f, 0f, 0f),
                report.SuggestedGridOriginWorldDelta);
        }

        [Test]
        public void ComputeOriginWorldDeltaToAnchorMinCellAtZero_MatchesMultiply()
        {
            var d = ArenaGridAlignment.ComputeOriginWorldDeltaToAnchorMinCellAtZero(new Vector2Int(3, -2), 2f);
            Assert.AreEqual(new Vector3(6f, 0f, -4f), d);
        }

        [Test]
        public void WorldDeltaToGridOriginLocalDelta_IdentityParent_ReturnsXZ()
        {
            var go = new GameObject("parent");
            try
            {
                var t = go.transform;
                t.position = new Vector3(5f, 0f, 0f);
                var w = new Vector3(1f, 0f, -2f);
                var local = ArenaGridAlignment.WorldDeltaToGridOriginLocalDelta(t, w);
                Assert.That(Vector3.Distance(local, new Vector3(1f, 0f, -2f)), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
