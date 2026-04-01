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

        [Test]
        public void StopParticleSystemsWhenFlowHidden_StopsParticleSystemsUnderThisRoot()
        {
            var root = new GameObject("FlowCanvasRootEditModeTests_Root");
            var flow = root.AddComponent<FlowCanvasRoot>();
            var child = new GameObject("PS");
            child.transform.SetParent(root.transform, false);
            var ps = child.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            ps.Play();
            ps.Simulate(0.1f, true, true);

            flow.StopParticleSystemsWhenFlowHidden();

            Assert.IsFalse(ps.isPlaying, "ParticleSystem under hidden flow root should be stopped.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void StopParticleSystemsWhenFlowHidden_SkipsParticleSystemsUnderNestedFlowCanvasRoot()
        {
            var root = new GameObject("FlowCanvasRootEditModeTests_Outer");
            var outer = root.AddComponent<FlowCanvasRoot>();
            var nestedGo = new GameObject("NestedRoot");
            nestedGo.transform.SetParent(root.transform, false);
            nestedGo.AddComponent<FlowCanvasRoot>();
            var leaf = new GameObject("NestedPS");
            leaf.transform.SetParent(nestedGo.transform, false);
            var nestedPs = leaf.AddComponent<ParticleSystem>();
            var nestedMain = nestedPs.main;
            nestedMain.playOnAwake = false;
            nestedPs.Play();
            nestedPs.Simulate(0.1f, true, true);

            outer.StopParticleSystemsWhenFlowHidden();

            Assert.IsTrue(nestedPs.isPlaying, "Nested FlowCanvasRoot subtree should manage its own particle systems.");

            Object.DestroyImmediate(root);
        }
    }
}
