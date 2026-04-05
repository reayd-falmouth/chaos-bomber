using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace fps.Tests.EditMode
{
    public class AvatarPerkIconUiEditModeTests
    {
        [Test]
        public void ApplyPerkIcon_NullImage_DoesNotThrow()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            Assert.DoesNotThrow(() => AvatarPerkIconUi.ApplyPerkIcon(null, sprite));
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void ApplyPerkIcon_NullSprite_DisablesAndClearsReference()
        {
            var go = new GameObject("Img");
            var image = go.AddComponent<Image>();
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            image.sprite = sprite;
            image.enabled = true;

            AvatarPerkIconUi.ApplyPerkIcon(image, null);

            Assert.IsNull(image.sprite);
            Assert.IsFalse(image.enabled);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void ApplyPerkIcon_NonNullSprite_AssignsAndEnables()
        {
            var go = new GameObject("Img");
            var image = go.AddComponent<Image>();
            image.enabled = false;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

            AvatarPerkIconUi.ApplyPerkIcon(image, sprite);

            Assert.AreSame(sprite, image.sprite);
            Assert.IsTrue(image.enabled);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(sprite);
        }
    }
}
