using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb
{
    /// <summary>
    /// Attached to bomb instances so AI (and others) can read explosion radius. Set by BombController when spawning.
    /// Self-registers with the arena-local GameManager so agents can read ArenaBombs without FindObjectsByType.
    /// </summary>
    public class BombInfo : MonoBehaviour
    {
        public int explosionRadius = 1;
        public float timeRemainingFraction = 1f; // 1 = just placed, 0 = about to explode

        private GameManager _gameManager;

        private void OnEnable()
        {
            var root = transform.root != transform ? transform.root : null;
            _gameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                           ?? GameManager.Instance;
            _gameManager?.RegisterBomb(this);
        }

        private void OnDestroy()
        {
            _gameManager?.UnregisterBomb(this);
        }
    }
}
