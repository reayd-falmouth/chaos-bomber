namespace Unity.FPS.Game
{
    /// <summary>
    /// Optional shield that can absorb one explosion-class hit before health is reduced.
    /// Implemented by gameplay abilities (e.g. MasterBlaster Protection).
    /// </summary>
    public interface IExplosionDamageShield
    {
        /// <returns>True if this shield consumed the hit and no health damage should be applied.</returns>
        bool TryConsumeExplosionHit();
    }
}
