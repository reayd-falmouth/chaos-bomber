#if UNITY_EDITOR
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class LevelArenaPreviewRendererEditModeTests
    {
        [Test]
        public void SetPreviewIndex_EnablesExactlyOneSlot_AndClampsIndex()
        {
            var host = new GameObject("LevelArenaPreviewRendererEditModeTests_Host");
            var renderer = host.AddComponent<LevelArenaPreviewRenderer>();

            var s0 = new GameObject("S0");
            var s1 = new GameObject("S1");
            var s2 = new GameObject("S2");
            s0.transform.SetParent(host.transform, false);
            s1.transform.SetParent(host.transform, false);
            s2.transform.SetParent(host.transform, false);

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(host.transform, false);
            var cam = camGo.AddComponent<Camera>();

            var rt = new RenderTexture(32, 32, 16);

            var so = new SerializedObject(renderer);
            so.FindProperty("previewCamera").objectReferenceValue = cam;
            so.FindProperty("renderTexture").objectReferenceValue = rt;
            var roots = so.FindProperty("previewSlotRoots");
            roots.arraySize = 3;
            roots.GetArrayElementAtIndex(0).objectReferenceValue = s0;
            roots.GetArrayElementAtIndex(1).objectReferenceValue = s1;
            roots.GetArrayElementAtIndex(2).objectReferenceValue = s2;
            so.ApplyModifiedProperties();

            renderer.SetPreviewIndex(1);
            Assert.IsFalse(s0.activeSelf);
            Assert.IsTrue(s1.activeSelf);
            Assert.IsFalse(s2.activeSelf);
            Assert.AreEqual(1, renderer.LastRenderedIndex);

            renderer.SetPreviewIndex(99);
            Assert.IsFalse(s0.activeSelf);
            Assert.IsFalse(s1.activeSelf);
            Assert.IsTrue(s2.activeSelf);
            Assert.AreEqual(2, renderer.LastRenderedIndex);

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void SetPreviewIndex_NoSlots_DoesNotThrow()
        {
            var host = new GameObject("LevelArenaPreviewRendererEditModeTests_Empty");
            var renderer = host.AddComponent<LevelArenaPreviewRenderer>();
            Assert.DoesNotThrow(() => renderer.SetPreviewIndex(0));
            Object.DestroyImmediate(host);
        }
    }

    public class AvatarPreviewRendererEditModeTests
    {
        [Test]
        public void SetPreviewIndex_EnablesExactlyOneSlot_AndClampsIndex()
        {
            var host = new GameObject("AvatarPreviewRendererEditModeTests_Host");
            var renderer = host.AddComponent<AvatarPreviewRenderer>();

            var s0 = new GameObject("A0");
            var s1 = new GameObject("A1");
            s0.transform.SetParent(host.transform, false);
            s1.transform.SetParent(host.transform, false);

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(host.transform, false);
            var cam = camGo.AddComponent<Camera>();

            var rt = new RenderTexture(32, 32, 16);

            var so = new SerializedObject(renderer);
            so.FindProperty("previewCamera").objectReferenceValue = cam;
            so.FindProperty("renderTexture").objectReferenceValue = rt;
            var roots = so.FindProperty("previewSlotRoots");
            roots.arraySize = 2;
            roots.GetArrayElementAtIndex(0).objectReferenceValue = s0;
            roots.GetArrayElementAtIndex(1).objectReferenceValue = s1;
            so.ApplyModifiedProperties();

            renderer.SetPreviewIndex(0);
            Assert.IsTrue(s0.activeSelf);
            Assert.IsFalse(s1.activeSelf);

            renderer.SetPreviewIndex(50);
            Assert.IsFalse(s0.activeSelf);
            Assert.IsTrue(s1.activeSelf);
            Assert.AreEqual(1, renderer.LastRenderedIndex);

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(host);
        }
    }
}
#endif
