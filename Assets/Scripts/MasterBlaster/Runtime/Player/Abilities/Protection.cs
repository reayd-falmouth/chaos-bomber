using System.Collections;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player.Abilities
{
    /// <summary>
    /// Protection ability — absorbs one explosion hit then flickers and deactivates.
    /// Matches MasterBlaster 2D semantics via <see cref="Unity.FPS.Game.IExplosionDamageShield"/> (pre-damage),
    /// not <see cref="Unity.FPS.Game.Health.OnDamaged"/> (too late).
    /// </summary>
    [DisallowMultipleComponent]
    public class Protection : MonoBehaviour, Unity.FPS.Game.IExplosionDamageShield
    {
        [Header("Visuals")]
        [SerializeField] private Material whiteProtectionMaterial;
        [SerializeField] private float flickerDuration = 2f;
        [SerializeField] private float flickerInterval = 0.2f;

        private Unity.FPS.Game.Health _health;
        private SpriteRenderer[] _spriteRenderers;
        private Material[] _originalMaterials;
        private bool _active;
        private Coroutine _flickerCo;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_health == null)
                _health = GetComponentInParent<Unity.FPS.Game.Health>();

            if (_spriteRenderers == null)
            {
                // MasterBlaster 2D applies the shield visuals across the whole player.
                // In the FPS hybrid setup, some billboards (e.g. `Billbox`) are siblings of the `Protection` GameObject,
                // so we must scope renderer discovery off the shared player root (Health), not just `this` transform.
                _spriteRenderers = _health != null
                    ? _health.GetComponentsInChildren<SpriteRenderer>(true)
                    : GetComponentsInChildren<SpriteRenderer>(true);
                _originalMaterials = new Material[_spriteRenderers.Length];
                for (int i = 0; i < _spriteRenderers.Length; i++)
                    _originalMaterials[i] = _spriteRenderers[i].sharedMaterial;
            }
        }

        public void Activate()
        {
            gameObject.SetActive(true);
            EnsureInitialized();
            _active = true;
            SetProtectionVisual(true);
        }

        /// <inheritdoc />
        public bool TryConsumeExplosionHit()
        {
            if (!_active)
                return false;
            EnsureInitialized();
            BeginRemoveProtection();
            return true;
        }

        public void TakeDamage()
        {
            if (!_active) return;
            BeginRemoveProtection();
        }

        private void BeginRemoveProtection()
        {
            if (_flickerCo != null) StopCoroutine(_flickerCo);
            _flickerCo = StartCoroutine(RemoveProtection());
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
            _active = false;
            SetProtectionVisual(false);
        }

        private void OnDisable()
        {
            _active = false;
        }

        private void SetProtectionVisual(bool on)
        {
            if (_spriteRenderers == null || _originalMaterials == null) return;
            if (whiteProtectionMaterial == null) return;
            for (int i = 0; i < _spriteRenderers.Length; i++)
                _spriteRenderers[i].material = on ? whiteProtectionMaterial : _originalMaterials[i];
        }

        public bool IsActive => _active;
    }
}
