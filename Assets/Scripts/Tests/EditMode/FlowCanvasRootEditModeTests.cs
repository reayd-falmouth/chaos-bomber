using HybridGame.MasterBlaster.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace fps.Tests.EditMode
{
    public class FlowCanvasRootEditModeTests
    {
        private static Sprite MakeSprite(Color c)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetOverrideMode(FlowCanvasRoot root, UiCanvasBackdropOverrideMode mode)
        {
            SetPrivateField(root, "uiCanvasBackdropOverrideMode", mode);
        }

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
        public void ApplyUiCanvasBackdropOverride_None_RestoresDefaults()
        {
            var rootGo = new GameObject("FlowCanvasRootEditModeTests_NoneRoot");
            var root = rootGo.AddComponent<FlowCanvasRoot>();

            var sharedGo = new GameObject("FlowCanvasRootEditModeTests_NoneBackdrop");
            var sharedImg = sharedGo.AddComponent<Image>();

            var defaultSprite = MakeSprite(Color.red);
            var defaultColor = new Color(1f, 0f, 0f, 1f);

            // Put some non-default values on the shared backdrop so we can prove they get restored.
            sharedImg.sprite = MakeSprite(Color.green);
            sharedImg.color = Color.green;

            SetOverrideMode(root, UiCanvasBackdropOverrideMode.None);
            SetPrivateField(root, "uiCanvasBackdropOverrideSprite", MakeSprite(Color.blue));
            SetPrivateField(root, "uiCanvasBackdropOverrideColor", Color.yellow);

            root.ApplyUiCanvasBackdropOverride(sharedImg, defaultSprite, defaultColor);

            Assert.AreSame(defaultSprite, sharedImg.sprite);
            Assert.AreEqual(defaultColor, sharedImg.color);

            Object.DestroyImmediate(sharedGo);
            Object.DestroyImmediate(rootGo);
        }

        [Test]
        public void ApplyUiCanvasBackdropOverride_ColorOnly_UsesDefaultSprite_OverrideColor()
        {
            var rootGo = new GameObject("FlowCanvasRootEditModeTests_ColorOnlyRoot");
            var root = rootGo.AddComponent<FlowCanvasRoot>();

            var sharedGo = new GameObject("FlowCanvasRootEditModeTests_ColorOnlyBackdrop");
            var sharedImg = sharedGo.AddComponent<Image>();

            var defaultSprite = MakeSprite(Color.red);
            var defaultColor = new Color(1f, 0f, 0f, 1f);
            var overrideColor = new Color(0f, 1f, 0f, 1f);
            var irrelevantOverrideSprite = MakeSprite(Color.blue);

            SetOverrideMode(root, UiCanvasBackdropOverrideMode.ColorOnly);
            SetPrivateField(root, "uiCanvasBackdropOverrideSprite", irrelevantOverrideSprite);
            SetPrivateField(root, "uiCanvasBackdropOverrideColor", overrideColor);

            root.ApplyUiCanvasBackdropOverride(sharedImg, defaultSprite, defaultColor);

            Assert.AreSame(defaultSprite, sharedImg.sprite);
            Assert.AreEqual(overrideColor, sharedImg.color);

            Object.DestroyImmediate(sharedGo);
            Object.DestroyImmediate(rootGo);
        }

        [Test]
        public void ApplyUiCanvasBackdropOverride_SpriteAndColor_UsesOverrideSpriteAndColor()
        {
            var rootGo = new GameObject("FlowCanvasRootEditModeTests_SpriteAndColorRoot");
            var root = rootGo.AddComponent<FlowCanvasRoot>();

            var sharedGo = new GameObject("FlowCanvasRootEditModeTests_SpriteAndColorBackdrop");
            var sharedImg = sharedGo.AddComponent<Image>();

            var defaultSprite = MakeSprite(Color.red);
            var defaultColor = Color.red;
            var overrideSprite = MakeSprite(Color.blue);
            var overrideColor = Color.cyan;

            SetOverrideMode(root, UiCanvasBackdropOverrideMode.SpriteAndColor);
            SetPrivateField(root, "uiCanvasBackdropOverrideSprite", overrideSprite);
            SetPrivateField(root, "uiCanvasBackdropOverrideColor", overrideColor);

            root.ApplyUiCanvasBackdropOverride(sharedImg, defaultSprite, defaultColor);

            Assert.AreSame(overrideSprite, sharedImg.sprite);
            Assert.AreEqual(overrideColor, sharedImg.color);

            Object.DestroyImmediate(sharedGo);
            Object.DestroyImmediate(rootGo);
        }

        [Test]
        public void ApplyUiCanvasBackdropOverride_SpriteAndColor_AllowsIntentionallyNullSprite()
        {
            var rootGo = new GameObject("FlowCanvasRootEditModeTests_SpriteAndColorNullSpriteRoot");
            var root = rootGo.AddComponent<FlowCanvasRoot>();

            var sharedGo = new GameObject("FlowCanvasRootEditModeTests_SpriteAndColorNullSpriteBackdrop");
            var sharedImg = sharedGo.AddComponent<Image>();

            var defaultSprite = MakeSprite(Color.red);
            var defaultColor = Color.red;
            var overrideColor = Color.green;

            SetOverrideMode(root, UiCanvasBackdropOverrideMode.SpriteAndColor);
            SetPrivateField(root, "uiCanvasBackdropOverrideSprite", null);
            SetPrivateField(root, "uiCanvasBackdropOverrideColor", overrideColor);

            root.ApplyUiCanvasBackdropOverride(sharedImg, defaultSprite, defaultColor);

            Assert.IsNull(sharedImg.sprite);
            Assert.AreEqual(overrideColor, sharedImg.color);

            Object.DestroyImmediate(sharedGo);
            Object.DestroyImmediate(rootGo);
        }

        [Test]
        public void ApplyUiCanvasBackdropOverride_CopyFromImage_CopiesSpriteAndColor()
        {
            var rootGo = new GameObject("FlowCanvasRootEditModeTests_CopyFromImageRoot");
            var root = rootGo.AddComponent<FlowCanvasRoot>();

            var sharedGo = new GameObject("FlowCanvasRootEditModeTests_CopyFromImageBackdrop");
            var sharedImg = sharedGo.AddComponent<Image>();

            var defaultSprite = MakeSprite(Color.red);
            var defaultColor = Color.red;

            var sourceGo = new GameObject("FlowCanvasRootEditModeTests_CopyFromImageSource");
            var sourceImg = sourceGo.AddComponent<Image>();
            var sourceSprite = MakeSprite(Color.blue);
            var sourceColor = new Color(1f, 1f, 0f, 1f); // yellow
            sourceImg.sprite = sourceSprite;
            sourceImg.color = sourceColor;

            SetOverrideMode(root, UiCanvasBackdropOverrideMode.CopyFromImage);
            SetPrivateField(root, "uiCanvasBackdropOverrideSourceImage", sourceImg);

            root.ApplyUiCanvasBackdropOverride(sharedImg, defaultSprite, defaultColor);

            Assert.AreSame(sourceSprite, sharedImg.sprite);
            Assert.AreEqual(sourceColor, sharedImg.color);

            Object.DestroyImmediate(sourceGo);
            Object.DestroyImmediate(sharedGo);
            Object.DestroyImmediate(rootGo);
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
            nestedMain.loop = true; // Keep playing so StopParticleSystemsWhenFlowHidden can be observed.
            nestedMain.playOnAwake = false;
            nestedMain.startLifetime = 10f; // Ensure particles still exist after simulation.

            var emission = nestedPs.emission;
            emission.rateOverTime = 50f;

            nestedPs.Play();
            nestedPs.Simulate(0.1f, true, true);

            outer.StopParticleSystemsWhenFlowHidden();

            Assert.Greater(nestedPs.particleCount, 0, "Nested FlowCanvasRoot subtree should manage its own particle systems.");

            Object.DestroyImmediate(root);
        }
    }
}
