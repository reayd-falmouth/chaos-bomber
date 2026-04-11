using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Shared pulse factor for alarm visuals (camera tint, emergency lights). Pure math for tests.
    /// </summary>
    public static class AlarmPresentationPulse
    {
        public static float SinPulse01(float timeSeconds, float pulseSpeed) =>
            (Mathf.Sin(timeSeconds * pulseSpeed) + 1f) * 0.5f;

        public static float SinPulse01WithPhase(
            float timeSeconds,
            float pulseSpeed,
            float phaseRadians
        ) => (Mathf.Sin(timeSeconds * pulseSpeed + phaseRadians) + 1f) * 0.5f;
    }
}
