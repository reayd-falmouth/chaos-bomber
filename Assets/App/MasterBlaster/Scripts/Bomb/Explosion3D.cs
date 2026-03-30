using HybridGame.MasterBlaster.Scripts.Player;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Bomb
{
    /// <summary>
    /// Visual-only explosion segment in 3D space.
    /// Billboarded sprite plays the explosion animation and self-destructs.
    /// Port of MasterBlaster's Explosion.cs — no Collider2D, no Physics2D.
    /// Damage is handled by BombController3D's overlap checks, not this component.
    /// </summary>
    public class Explosion3D : MonoBehaviour
    {
        [Header("Visuals")]
        public AnimatedSpriteRenderer spriteCenter;
        public AnimatedSpriteRenderer spriteHorizontal;
        public AnimatedSpriteRenderer spriteVertical;

        [Header("Audio")]
        [SerializeField] private MMF_Player explosionFeedbacks;

        private void Start()
        {
            if (spriteCenter != null)
            {
                spriteCenter.idle = false;
                spriteCenter.StartAnimation();
            }
        }

        /// <summary>
        /// Show the sprite that matches the explosion direction (XZ plane).
        /// direction should be one of Vector3.forward/back/left/right.
        /// </summary>
        public void SetDirection(Vector3 direction)
        {
            // Center burst is for the bomb origin only — hide it at ray positions
            if (spriteCenter != null) spriteCenter.gameObject.SetActive(false);

            bool horizontal = Mathf.Abs(direction.x) > 0.1f;
            if (spriteHorizontal != null)
            {
                spriteHorizontal.enabled = horizontal;
                if (horizontal) spriteHorizontal.idle = false;
            }
            if (spriteVertical != null)
            {
                spriteVertical.enabled = !horizontal;
                if (!horizontal) spriteVertical.idle = false;
            }
        }

        public void PlayExplosionSound()
        {
            explosionFeedbacks?.PlayFeedbacks(transform.position);
        }

        public void DestroyAfter(float seconds)
        {
            Destroy(gameObject, seconds);
        }
    }
}
