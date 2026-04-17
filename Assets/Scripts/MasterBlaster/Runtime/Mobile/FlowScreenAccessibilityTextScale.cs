using UnityEngine;
using UnityEngine.Accessibility;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Handheld flow screens (Quote, Prologue): combine author-tuned mobile boost with system font scale.
    /// </summary>
    public static class FlowScreenAccessibilityTextScale
    {
        public static bool IsHandheldMobile() =>
            Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer;

        /// <summary>
        /// Pure scale math for tests and callers that inject <paramref name="systemFontScale"/>.
        /// </summary>
        public static float ComputeCombinedTextScale(
            bool isMobile,
            float systemFontScale,
            float mobileBoost,
            float minScale,
            float maxScale)
        {
            if (!isMobile)
                return 1f;

            float s = Mathf.Max(0.01f, systemFontScale);
            float b = Mathf.Max(0.01f, mobileBoost);
            return Mathf.Clamp(b * s, minScale, maxScale);
        }

        public static float GetCombinedTextScale(float mobileBoost, float minScale, float maxScale)
        {
            if (!IsHandheldMobile())
                return 1f;
            return ComputeCombinedTextScale(true, AccessibilitySettings.fontScale, mobileBoost, minScale, maxScale);
        }
    }
}
