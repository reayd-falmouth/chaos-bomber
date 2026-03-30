using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Shop;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.Abilities
{
    [DisallowMultipleComponent]
    public class Protection : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField]
        private Material whiteProtectionMaterial;

        [SerializeField]
        private float flickerDuration = 2f;

        [SerializeField]
        private float flickerInterval = 0.2f;

        private PlayerController pc;
        private SpriteRenderer[] allSpriteRenderers;
        private Material[] originalMaterials;
        private bool active;
        private Coroutine flickerCo;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
            allSpriteRenderers = pc.GetComponentsInChildren<SpriteRenderer>(true);
            originalMaterials = new Material[allSpriteRenderers.Length];
            for (int i = 0; i < allSpriteRenderers.Length; i++)
                originalMaterials[i] = allSpriteRenderers[i].sharedMaterial;
        }

        private void Start()
        {
            SyncFromSession();
        }

        /// <summary>
        /// Removes arena-granted protection and reapplies shop protection (subscription + visuals).
        /// </summary>
        public void SyncFromSession()
        {
            if (pc == null)
                return;

            if (flickerCo != null)
            {
                StopCoroutine(flickerCo);
                flickerCo = null;
            }

            pc.OnExplosionHit -= HandleExplosionHit;
            active = false;
            SetProtectionVisual(false);

            if (SessionManager.Instance == null || pc.playerId <= 0)
                return;

            int playerId = pc.playerId;
            bool shopProtection =
                SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Protection) == 1;
            UnityEngine.Debug.Log($"[PlayerController] Player {playerId} protection session sync (shop={shopProtection}).");

            if (!shopProtection)
                return;

            active = true;
            pc.OnExplosionHit += HandleExplosionHit;
            SetProtectionVisual(true);
        }

        public void Activate()
        {
            active = true;
            pc.OnExplosionHit += HandleExplosionHit;
            SetProtectionVisual(true);
        }

        private bool HandleExplosionHit()
        {
            if (active)
            {
                TakeDamage(); // absorb and flicker
                return true; // block death
            }
            return false; // not blocking
        }

        public void TakeDamage()
        {
            if (!active)
                return;
            if (flickerCo != null)
                StopCoroutine(flickerCo);
            flickerCo = StartCoroutine(RemoveProtection());
        }

        private IEnumerator RemoveProtection()
        {
            float elapsed = 0f;
            bool on = true;

            while (elapsed < flickerDuration)
            {
                SetProtectionVisual(on);
                on = !on;
                yield return new WaitForSeconds(flickerInterval);
                elapsed += flickerInterval;
            }

            active = false;
            SetProtectionVisual(false);
            pc.OnExplosionHit -= HandleExplosionHit;
        }

        private void SetProtectionVisual(bool on)
        {
            if (whiteProtectionMaterial == null)
            {
                UnityEngine.Debug.LogWarning("[Protection] whiteProtectionMaterial not set.");
                return;
            }

            for (int i = 0; i < allSpriteRenderers.Length; i++)
            {
                allSpriteRenderers[i].material = on
                    ? whiteProtectionMaterial
                    : originalMaterials[i];
            }
        }

        public bool IsActive => active;
    }
}
