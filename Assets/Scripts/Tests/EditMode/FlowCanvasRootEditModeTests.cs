using HybridGame.MasterBlaster.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class FlowCanvasRootEditModeTests
    {
        [Test]
        public void ApplyCanvasGroupVisibility_SetsAlphaAndRaycastGates()
        {
            var go = new GameObject("FlowCanvasRootEditModeTests_CanvasGroup");
            var cg = go.AddComponent<CanvasGroup>();

            FlowCanvasRoot.ApplyCanvasGroupVisibility(cg, true);
            Assert.AreEqual(1f, cg.alpha);
            Assert.IsTrue(cg.interactable);
            Assert.IsTrue(cg.blocksRaycasts);

            FlowCanvasRoot.ApplyCanvasGroupVisibility(cg, false);
            Assert.AreEqual(0f, cg.alpha);
            Assert.IsFalse(cg.interactable);
            Assert.IsFalse(cg.blocksRaycasts);

            Object.DestroyImmediate(go);
        }
    }
}
