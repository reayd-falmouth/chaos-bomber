using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Shop;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.Abilities
{
    [DisallowMultipleComponent]
    public class Ghost : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private float defaultDuration = 15f;

        [SerializeField]
        private AnimatedSpriteRenderer spriteRendererGhost;

        private PlayerController pc;
        private Rigidbody2D rb;
        private LayerMask ghostExcludeLayers;
        private bool active;
        private float timer;
        private Coroutine endCo;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
            rb = GetComponentInParent<Rigidbody2D>();
            ghostExcludeLayers = LayerMask.GetMask("Destructible", "Bomb");
        }

        private void Start()
        {
            SyncFromSession();
        }

        private bool IsShopGhostOwned()
        {
            return SessionManager.Instance != null
                && pc != null
                && pc.playerId > 0
                && SessionManager.Instance.GetUpgradeLevel(pc.playerId, ShopItemType.Ghost) == 1;
        }

        private void Update()
        {
            if (!active)
                return;

            // Shop-purchased ghost stays on; arena pickup uses a finite timer only.
            if (IsShopGhostOwned())
                return;

            timer -= Time.deltaTime;
            if (timer <= 0f && endCo == null)
                endCo = StartCoroutine(EndRoutine());
        }

        private void OnDisable()
        {
            if (!active)
                return;
            ForceDeactivateQuiet();
        }

        /// <summary>Clears ghost movement/visual state without the "stuck in wall" death check.</summary>
        private void ForceDeactivateQuiet()
        {
            if (endCo != null)
            {
                StopCoroutine(endCo);
                endCo = null;
            }

            active = false;
            timer = 0f;

            if (rb != null)
                rb.excludeLayers &= ~ghostExcludeLayers;

            if (pc != null)
            {
                pc.visualOverrideActive = false;
                pc.visualOverrideRenderer = null;
            }

            if (spriteRendererGhost != null)
                spriteRendererGhost.StopAnimation();
        }

        private void ApplyGhostVisualAndCollision()
        {
            if (spriteRendererGhost == null || pc == null || rb == null)
                return;

            spriteRendererGhost.StartAnimation();
            pc.visualOverrideActive = true;
            pc.visualOverrideRenderer = spriteRendererGhost;
            pc.UpdateVisualState();

            rb.excludeLayers |= ghostExcludeLayers;
        }

        /// <summary>
        /// Clears arena-pickup ghost, then reapplies shop ghost if purchased. Call when session upgrades
        /// are reapplied (new round).
        /// </summary>
        public void SyncFromSession()
        {
            if (pc == null)
                return;

            ForceDeactivateQuiet();

            if (SessionManager.Instance == null || pc.playerId <= 0)
                return;

            int playerId = pc.playerId;
            bool shopGhost = SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Ghost) == 1;
            UnityEngine.Debug.Log($"[PlayerController] Player {playerId} ghost session sync (shop={shopGhost}).");

            if (!shopGhost)
                return;

            active = true;
            timer = 0f;
            ApplyGhostVisualAndCollision();
        }

        public void Activate(float duration = -1f)
        {
            active = true;
            timer = duration > 0f ? duration : defaultDuration;
            ApplyGhostVisualAndCollision();
        }

        private IEnumerator EndRoutine()
        {
            active = false;

            // Restore collisions for this player's rigidbody only
            rb.excludeLayers &= ~ghostExcludeLayers;

            // Clear override → back to normal visuals
            pc.visualOverrideActive = false;
            pc.visualOverrideRenderer = null;
            pc.SetVisualState(PlayerController.PlayerVisualState.Normal);
            spriteRendererGhost.StopAnimation();

            // Safety check
            if (Physics2D.OverlapCircleAll(transform.position, 0.1f, LayerMask.GetMask("Destructible", "Bomb")).Length > 0)
                pc.ApplyDeath();

            yield return null;
            endCo = null;
        }
    }
}
