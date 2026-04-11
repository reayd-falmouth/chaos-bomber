using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// Plays the same sprite sequence on multiple <see cref="SpriteRenderer"/>s in lockstep (e.g. six faces of a unit cube).
    /// </summary>
    public class MultiFaceDestroySpriteAnimation : MonoBehaviour
    {
        [Header("Animation")]
        public Sprite[] animationSprites;
        public float animationTime = 0.1f;
        public bool loop;

        [Header("Faces")]
        public SpriteRenderer[] faceRenderers;

        private bool _animating;
        private int _animationFrame;

        /// <summary>Shows the destroy VFX and starts the synchronized frame sequence.</summary>
        public void Play()
        {
            gameObject.SetActive(true);
            enabled = true;
            _animating = true;
            _animationFrame = 0;
            CancelInvoke(nameof(NextFrame));
            ApplyFrameToAllFaces();
            if (animationSprites != null && animationSprites.Length > 0)
                InvokeRepeating(nameof(NextFrame), animationTime, animationTime);
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void StopAnimation()
        {
            _animating = false;
            CancelInvoke(nameof(NextFrame));
        }

        /// <summary>Advances one frame; same timing contract as <c>AnimatedSpriteRenderer.NextFrame</c>.</summary>
        public void NextFrame()
        {
            if (!_animating) return;
            if (animationSprites == null || animationSprites.Length == 0) return;

            _animationFrame++;

            if (loop && _animationFrame >= animationSprites.Length)
                _animationFrame = 0;

            if (!loop && _animationFrame >= animationSprites.Length)
            {
                CancelInvoke(nameof(NextFrame));
                return;
            }

            ApplyFrameToAllFaces();
        }

        private void ApplyFrameToAllFaces()
        {
            if (animationSprites == null || animationSprites.Length == 0 || faceRenderers == null)
                return;

            if (_animationFrame < 0 || _animationFrame >= animationSprites.Length)
                return;

            Sprite s = animationSprites[_animationFrame];

            for (int i = 0; i < faceRenderers.Length; i++)
            {
                if (faceRenderers[i] != null)
                    faceRenderers[i].sprite = s;
            }
        }
    }
}
