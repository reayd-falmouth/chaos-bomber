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

        [Header("Charge burst (center detonation only)")]
        [Tooltip("Child with ParticleSystems (e.g. ChargeExplosion prefab). Played from PlayExplosionSound; hidden for propagated rays.")]
        [SerializeField]
        private Transform chargeBurstRoot;

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
            if (chargeBurstRoot != null) chargeBurstRoot.gameObject.SetActive(false);

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

        /// <summary>Plays the explosion feedbacks (call only on the central explosion).</summary>
        public void PlayExplosionSound()
        {
            PlayChargeBurst();
            explosionFeedbacks?.PlayFeedbacks(transform.position);
        }

        private void PlayChargeBurst()
        {
            if (chargeBurstRoot == null) return;
            foreach (var ps in chargeBurstRoot.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == null) continue;
                DisableParticlePhysicsInteractions(ps);
                ps.Play(true);
            }
        }

        /// <summary>
        /// ChargeExplosion assets may enable Collision (world/planes) with a broad layer mask,
        /// which can hit pickups and other gameplay colliders. VFX should not participate in physics.
        /// </summary>
        private static void DisableParticlePhysicsInteractions(ParticleSystem ps)
        {
            var collision = ps.collision;
            collision.enabled = false;
            var trigger = ps.trigger;
            trigger.enabled = false;
        }

        public void DestroyAfter(float seconds)
        {
            // Do not extend lifetime from charge-burst particle duration — that kept billboard sprites
            // visible long after the explosion clip should end. VFX cuts with the explosion timing.
            float destroyAfter = seconds;
            if (explosionFeedbacks != null)
                destroyAfter = Mathf.Max(destroyAfter, explosionFeedbacks.TotalDuration);

            Destroy(gameObject, destroyAfter);
        }
    }
}
