using HybridGame.MasterBlaster.Scripts.Scenes.WheelOFortune;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace fps.Tests.EditMode
{
    public class WheelControllerEditModeTests
    {
        [Test]
        public void ApplyPointerTexts_SetsInactiveSpaceAndActiveChevron()
        {
            const string inactive = " ";
            const string active = ">";

            var texts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject($"Pointer_{i}");
                texts[i] = go.AddComponent<Text>();
            }

            WheelController.ApplyPointerTexts(texts, 1, inactive, active);

            Assert.AreEqual(inactive, texts[0].text);
            Assert.AreEqual(active, texts[1].text);
            Assert.AreEqual(inactive, texts[2].text);

            for (int i = 0; i < 3; i++)
                Object.DestroyImmediate(texts[i].gameObject);
        }

        [Test]
        public void ApplyPointerTexts_ClampsActiveIndex_WhenOutOfRange()
        {
            var go = new GameObject("Pointer_single");
            var t = go.AddComponent<Text>();
            var arr = new[] { t };

            WheelController.ApplyPointerTexts(arr, 99, " ", ">");

            Assert.AreEqual(">", t.text);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyPointerTexts_NoOp_WhenNullOrEmpty()
        {
            WheelController.ApplyPointerTexts(null, 0, " ", ">");
            WheelController.ApplyPointerTexts(System.Array.Empty<Text>(), 0, " ", ">");
        }
    }
}
