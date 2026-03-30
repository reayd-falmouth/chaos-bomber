using HybridGame.MasterBlaster.Scripts.Player;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb
{
    public class Explosion : MonoBehaviour
    {
        public AnimatedSpriteRenderer spriteRenderer;

        [Tooltip("MMF_Player on this prefab — add an MMF_MMSoundManagerSound feedback and assign your explode clip there.")]
        [SerializeField] private MMF_Player explosionFeedbacks;

        private GameManager _gameManager;

        private void Awake()
        {
            var root = transform.root != transform ? transform.root : null;
            _gameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                           ?? GameManager.Instance;
            _gameManager?.RegisterExplosion(this);
        }

        private void OnDestroy()
        {
            _gameManager?.UnregisterExplosion(this);
        }

        /// <summary>Plays the explosion feedbacks (call only on the central explosion).</summary>
        public void PlayExplosionSound()
        {
            explosionFeedbacks?.PlayFeedbacks(transform.position);
        }

        /// <summary>Rotates the explosion sprite to face the given 2D direction.</summary>
        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-8f)
                return;
            var d = direction.normalized;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void DestroyAfter(float seconds)
        {
            float soundDuration = explosionFeedbacks != null ? explosionFeedbacks.TotalDuration : 0f;
            Destroy(gameObject, Mathf.Max(seconds, soundDuration));
        }
    }
}

