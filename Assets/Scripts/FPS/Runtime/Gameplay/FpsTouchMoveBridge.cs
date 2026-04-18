using System;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// Optional hook for on-screen D-pad movement (e.g. Master Blaster mobile overlay).
    /// Wired from HybridGame to avoid a circular assembly reference (HybridGame already references fps.Gameplay).
    /// </summary>
    public static class FpsTouchMoveBridge
    {
        /// <summary>Returns world-space move intent (X strafe, Z forward); zero when none.</summary>
        public static Func<Vector3> TryGetDigitalMoveWorld;
    }
}
