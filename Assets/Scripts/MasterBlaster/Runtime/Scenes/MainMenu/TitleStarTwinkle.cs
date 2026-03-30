using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.MainMenu
{
    /// <summary>
    /// Twinkle for Title screen starfield / star images by modulating <see cref="Graphic.color"/> alpha.
    /// Attach to an <see cref="Image"/>; optionally twinkle all child graphics with different phases.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class TitleStarTwinkle : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("If true, finds all Graphics under this transform and twinkles each with a random phase.")]
        [SerializeField] private bool twinkleChildren;

        [Header("Alpha (multipliers of each graphic's alpha on enable)")]
        [SerializeField] [Range(0f, 1f)] private float minAlpha = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float maxAlpha = 1f;

        [Header("Motion")]
        [Tooltip("Primary twinkle speed (cycles per second).")]
        [SerializeField] private float primaryFrequency = 1.1f;
        [Tooltip("Secondary shimmer (mixed in for a less regular feel).")]
        [SerializeField] private float secondaryFrequency = 2.4f;
        [SerializeField] [Range(0f, 1f)] private float secondaryMix = 0.3f;

        private Graphic[] _targets;
        private float[] _baseAlphas;
        private float[] _phaseOffsets;

        private void OnEnable()
        {
            CacheTargets();
        }

        private void CacheTargets()
        {
            if (twinkleChildren)
                _targets = GetComponentsInChildren<Graphic>(true);
            else
            {
                var g = GetComponent<Graphic>();
                _targets = g != null ? new[] { g } : System.Array.Empty<Graphic>();
            }

            _baseAlphas = new float[_targets.Length];
            _phaseOffsets = new float[_targets.Length];
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null)
                    _baseAlphas[i] = _targets[i].color.a;
                _phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            if (_targets == null || _targets.Length == 0)
                return;

            float t = Time.unscaledTime;
            float minA = Mathf.Min(minAlpha, maxAlpha);
            float maxA = Mathf.Max(minAlpha, maxAlpha);

            for (int i = 0; i < _targets.Length; i++)
            {
                var graphic = _targets[i];
                if (graphic == null) continue;

                float ph = _phaseOffsets[i];
                float w1 = Mathf.Sin(t * (Mathf.PI * 2f) * primaryFrequency + ph);
                float w2 = Mathf.Sin(t * (Mathf.PI * 2f) * secondaryFrequency + ph * 1.7f);
                float blend = Mathf.Clamp01((w1 + secondaryMix * w2) * 0.5f + 0.5f);
                float mul = Mathf.Lerp(minA, maxA, blend);

                Color c = graphic.color;
                c.a = _baseAlphas[i] * mul;
                graphic.color = c;
            }
        }

        private void OnDisable()
        {
            if (_targets == null || _baseAlphas == null) return;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null && i < _baseAlphas.Length)
                {
                    Color c = _targets[i].color;
                    c.a = _baseAlphas[i];
                    _targets[i].color = c;
                }
            }
        }
    }
}
