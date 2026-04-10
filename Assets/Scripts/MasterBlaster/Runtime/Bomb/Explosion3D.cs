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

        [Header("Center charge burst (optional)")]
        [Tooltip("Spawned only when PlayExplosionSound runs (bomb center). Independent lifetime; use ParticleEffects layer in Project Settings.")]
        [SerializeField]
        private GameObject centerChargeParticlePrefab;

        [SerializeField]
        private string chargeVfxLayerName = "ParticleEffects";

        [Tooltip("Destroy time for the spawned instance if particle durations cannot be estimated.")]
        [SerializeField]
        private float chargeVfxFallbackLifetimeSeconds = 2f;

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

        /// <summary>Plays the explosion feedbacks (call only on the central explosion).</summary>
        public void PlayExplosionSound()
        {
            SpawnCenterChargeBurst();
            explosionFeedbacks?.PlayFeedbacks(transform.position);
        }

        private void SpawnCenterChargeBurst()
        {
            if (centerChargeParticlePrefab == null) return;

            var parent = transform.parent;
            var instance = Instantiate(
                centerChargeParticlePrefab,
                transform.position,
                transform.rotation,
                parent);

            int layer = LayerMask.NameToLayer(chargeVfxLayerName);
            if (layer >= 0)
                SetLayerRecursively(instance, layer);

            float maxLife = 0f;
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == null) continue;
                var col = ps.collision;
                col.enabled = false;
                var trig = ps.trigger;
                trig.enabled = false;
                ps.Play(true);
                maxLife = Mathf.Max(maxLife, EstimateParticleSystemMaxSimTime(ps));
            }

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = false;
            }

            float destroyAfter = maxLife > 0.05f ? maxLife : chargeVfxFallbackLifetimeSeconds;
            Destroy(instance, destroyAfter);
        }

        private static float EstimateParticleSystemMaxSimTime(ParticleSystem ps)
        {
            var main = ps.main;
            float dur = main.duration;
            var life = main.startLifetime;
            float maxLife = Mathf.Max(life.constantMin, life.constantMax);
            return dur + maxLife;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        public void DestroyAfter(float seconds)
        {
            Destroy(gameObject, seconds);
        }
    }
}
