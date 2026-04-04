using UnityEngine;
using UnityEngine.UI;

namespace HybridGame
{
    [ExecuteAlways]
    public class BurnController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image targetImage;

        [Header("Glow Settings")]
        [SerializeField, ColorUsage(true, true)] private Color innerColor = Color.white;
        [SerializeField, ColorUsage(true, true)] private Color outerColor = new Color(0.71f, 0.71f, 0.42f, 1f);
        [SerializeField, Range(0f, 50f)] private float glowWidth = 15f;
        [SerializeField, Range(0.1f, 10f)] private float glowSoftness = 2f;
        [SerializeField, Range(0f, 10f)] private float glowIntensity = 2.5f;

        private const string _InnerColorProp = "_InnerColor";
        private const string _OuterColorProp = "_OuterColor";
        private const string _WidthProp = "_GlowWidth";
        private const string _SoftProp = "_GlowSoftness";
        private const string _IntenProp = "_GlowIntensity";

        private Material runtimeMaterial;

        private void Awake()
        {
            if (!targetImage) targetImage = GetComponent<Image>();
            if (targetImage && Application.isPlaying)
            {
                runtimeMaterial = new Material(targetImage.material);
                targetImage.material = runtimeMaterial;
            }
            ApplyToMaterial();
        }

        private void OnValidate() => ApplyToMaterial();

        public void ApplyToMaterial()
        {
            Material mat = Application.isPlaying ? runtimeMaterial : (targetImage ? targetImage.material : null);
            if (!mat) return;

            mat.SetColor(_InnerColorProp, innerColor);
            mat.SetColor(_OuterColorProp, outerColor);
            mat.SetFloat(_WidthProp, glowWidth);
            mat.SetFloat(_SoftProp, glowSoftness);
            mat.SetFloat(_IntenProp, glowIntensity);
        }

        public void SetGlow(Color color, float width, float softness)
        {
            outerColor = color;
            innerColor = Color.white;
            glowWidth = width;
            glowSoftness = softness;
            ApplyToMaterial();
        }
    }
}