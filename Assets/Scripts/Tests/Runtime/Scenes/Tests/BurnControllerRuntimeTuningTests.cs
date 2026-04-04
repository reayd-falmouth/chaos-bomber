using UnityEngine;
using UnityEngine.UI;

namespace HybridGame
{
    [ExecuteAlways]
    public class BurnController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private SpriteRenderer targetSprite;

        [Header("Glow Settings")]
        [SerializeField, ColorUsage(true, true)] private Color innerColor = Color.white;
        [SerializeField, ColorUsage(true, true)] private Color outerColor = new Color(0.71f, 0.71f, 0.42f, 1f);
        [SerializeField, Range(0f, 30f)] private float glowWidth = 10f;
        [SerializeField, Range(0.1f, 10f)] private float glowSoftness = 4f;
        [SerializeField, Range(0f, 10f)] private float glowIntensity = 2.5f;

        // Constants must match the new Shader Property names exactly
        private const string _InnerColorProp = "_InnerColor";
        private const string _OuterColorProp = "_OuterColor";
        private const string _WidthProp = "_GlowWidth";
        private const string _SoftProp = "_GlowSoftness";
        private const string _IntenProp = "_GlowIntensity";

        private Material runtimeMaterial;

        private void Awake()
        {
            if (!targetSprite) targetSprite = GetComponent<SpriteRenderer>();

            if (targetSprite && Application.isPlaying)
            {
                runtimeMaterial = new Material(targetSprite.sharedMaterial);
                targetSprite.material = runtimeMaterial;
            }
            ApplyToMaterial();
        }

        private void OnValidate() => ApplyToMaterial();

        public void ApplyToMaterial()
        {
            Material mat = Application.isPlaying ? runtimeMaterial : (targetSprite ? targetSprite.sharedMaterial : null);
            if (!mat) return;

            mat.SetColor(_InnerColorProp, innerColor);
            mat.SetColor(_OuterColorProp, outerColor);
            mat.SetFloat(_WidthProp, glowWidth);
            mat.SetFloat(_SoftProp, glowSoftness);
            mat.SetFloat(_IntenProp, glowIntensity);
        }

        // FIX: Re-adding this method so the Unit Tests compile and pass
        public void SetGlow(Color color, float width, float softness)
        {
            outerColor = color;
            innerColor = Color.white; // Defaulting inner to white for the test
            glowWidth = width;
            glowSoftness = softness;
            ApplyToMaterial();
        }
    }
}