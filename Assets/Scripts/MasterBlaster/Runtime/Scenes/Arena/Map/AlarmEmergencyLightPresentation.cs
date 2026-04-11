using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Cache and pulse optional spot/point lights under a root when the match alarm is active.
    /// </summary>
    public static class AlarmEmergencyLightPresentation
    {
        public static void TryCache(Transform root, ref Light[] lights, ref float[] baseIntensities, ref bool cached)
        {
            if (root == null || cached)
                return;
            lights = root.GetComponentsInChildren<Light>(true);
            baseIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                baseIntensities[i] = lights[i].intensity;
            cached = true;
        }

        public static void ActivateAlarmRoot(Transform root)
        {
            if (root != null)
                root.gameObject.SetActive(true);
        }

        public static void RestoreAndHideRoot(Transform root, Light[] lights, float[] baseIntensities)
        {
            if (root == null || lights == null || baseIntensities == null)
                return;
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = baseIntensities[i];
            root.gameObject.SetActive(false);
        }

        public static void ApplyIntensityPulse(
            Light[] lights,
            float[] baseIntensities,
            float timeSeconds,
            float pulseSpeed,
            float phaseSpreadPerIndexRadians
        )
        {
            if (lights == null || baseIntensities == null)
                return;
            for (int i = 0; i < lights.Length; i++)
            {
                float t = AlarmPresentationPulse.SinPulse01WithPhase(
                    timeSeconds,
                    pulseSpeed,
                    i * phaseSpreadPerIndexRadians
                );
                lights[i].intensity = baseIntensities[i] * t;
            }
        }
    }
}
