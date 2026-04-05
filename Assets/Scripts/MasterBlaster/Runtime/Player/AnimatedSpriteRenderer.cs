using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Frame-based sprite animation on a SpriteRenderer.
    /// Copied verbatim from MasterBlaster (Core.AnimatedSpriteRenderer), namespace changed.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimatedSpriteRenderer : MonoBehaviour
    {
        public const float MinMovementSpeedEpsilon = 0.01f;

        /// <summary>
        /// Per-frame delay at <paramref name="referenceSpeed"/> matching a tuned <paramref name="baseInterval"/>;
        /// scales inversely with <paramref name="currentSpeed"/> so walk cycles keep similar coverage per grid cell.
        /// </summary>
        public static float ComputeScaledFrameInterval(float baseInterval, float referenceSpeed, float currentSpeed)
        {
            float b = Mathf.Max(baseInterval, 1e-4f);
            float refS = Mathf.Max(referenceSpeed, MinMovementSpeedEpsilon);
            float v = Mathf.Max(currentSpeed, MinMovementSpeedEpsilon);
            return b * (refS / v);
        }

        private SpriteRenderer spriteRenderer;

        public Sprite idleSprite;
        public Sprite[] animationSprites;

        public float animationTime = 0.25f;
        private int animationFrame;

        public bool loop = true;
        // Property so we can immediately show the first walk frame when movement starts,
        // rather than waiting up to animationTime seconds for the next timer tick.
        private bool _idle = true;
        public bool idle
        {
            get => _idle;
            set
            {
                _idle = value;
                if (_idle)
                {
                    // Ensure we immediately display the idle frame when stopping.
                    if (spriteRenderer != null)
                        spriteRenderer.sprite = idleSprite;
                }
                else if (animating && animationSprites != null && animationSprites.Length > 0)
                {
                    animationFrame = 0;
                    spriteRenderer.sprite = animationSprites[0];
                }
            }
        }

        public bool playOnStart = true;
        private bool animating = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            spriteRenderer.enabled = true;
            if (playOnStart && !animating)
                StartAnimation();
            else
                spriteRenderer.sprite = idleSprite;
        }

        private void OnDisable()
        {
            StopAnimation();
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
        }

        private void Start()
        {
            if (playOnStart)
                StartAnimation();
            else
                spriteRenderer.sprite = idleSprite;
        }

        public void StartAnimation()
        {
            if (animating) return;
            animating = true;
            InvokeRepeating(nameof(NextFrame), animationTime, animationTime);
        }

        /// <summary>
        /// Updates <see cref="animationTime"/> and, if already animating, restarts <see cref="InvokeRepeating"/>
        /// (may slightly reset phase).
        /// </summary>
        public void SetFrameInterval(float seconds)
        {
            animationTime = Mathf.Max(seconds, 1e-4f);
            if (!animating) return;
            CancelInvoke(nameof(NextFrame));
            InvokeRepeating(nameof(NextFrame), animationTime, animationTime);
        }

        public void StopAnimation()
        {
            animating = false;
            CancelInvoke(nameof(NextFrame));
            if (spriteRenderer != null)
                spriteRenderer.sprite = idleSprite;
        }

        public void NextFrame()
        {
            if (!animating) return;

            animationFrame++;

            if (loop && animationFrame >= animationSprites.Length)
                animationFrame = 0;

            if (idle)
                spriteRenderer.sprite = idleSprite;
            else if (animationFrame >= 0 && animationFrame < animationSprites.Length)
                spriteRenderer.sprite = animationSprites[animationFrame];
        }
    }
}
