using HybridGame.MasterBlaster.Scripts.Arena;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class MultiFaceDestroySpriteAnimationTests
    {
        private static Sprite MakeSprite(Color c)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, c);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        [Test]
        public void Play_SetsFirstFrameOnAllFaces()
        {
            var a = MakeSprite(Color.red);
            var b = MakeSprite(Color.green);
            var c = MakeSprite(Color.blue);

            var root = new GameObject("root");
            try
            {
                var sr0 = new GameObject("f0").AddComponent<SpriteRenderer>();
                var sr1 = new GameObject("f1").AddComponent<SpriteRenderer>();
                sr0.transform.SetParent(root.transform, false);
                sr1.transform.SetParent(root.transform, false);

                var vfx = root.AddComponent<MultiFaceDestroySpriteAnimation>();
                vfx.animationSprites = new[] { a, b, c };
                vfx.animationTime = 0.1f;
                vfx.loop = false;
                vfx.faceRenderers = new[] { sr0, sr1 };

                vfx.Play();
                vfx.CancelInvoke(nameof(MultiFaceDestroySpriteAnimation.NextFrame));

                Assert.AreSame(a, sr0.sprite);
                Assert.AreSame(a, sr1.sprite);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NextFrame_UpdatesAllFacesInLockstep_AndHoldsLastWhenNotLooping()
        {
            var a = MakeSprite(Color.red);
            var b = MakeSprite(Color.green);
            var c = MakeSprite(Color.blue);

            var root = new GameObject("root");
            try
            {
                var sr0 = new GameObject("f0").AddComponent<SpriteRenderer>();
                var sr1 = new GameObject("f1").AddComponent<SpriteRenderer>();
                sr0.transform.SetParent(root.transform, false);
                sr1.transform.SetParent(root.transform, false);

                var vfx = root.AddComponent<MultiFaceDestroySpriteAnimation>();
                vfx.animationSprites = new[] { a, b, c };
                vfx.animationTime = 0.1f;
                vfx.loop = false;
                vfx.faceRenderers = new[] { sr0, sr1 };

                vfx.Play();
                vfx.CancelInvoke(nameof(MultiFaceDestroySpriteAnimation.NextFrame));

                vfx.NextFrame();
                Assert.AreSame(b, sr0.sprite);
                Assert.AreSame(b, sr1.sprite);

                vfx.NextFrame();
                Assert.AreSame(c, sr0.sprite);
                Assert.AreSame(c, sr1.sprite);

                vfx.NextFrame();
                Assert.AreSame(c, sr0.sprite);
                Assert.AreSame(c, sr1.sprite);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
