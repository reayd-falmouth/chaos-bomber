using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Charge burst VFX is assigned to ParticleEffects; layer must exist in TagManager.
    /// </summary>
    public class ExplosionChargeParticleLayerEditModeTests
    {
        [Test]
        public void ParticleEffects_layer_exists()
        {
            int layer = LayerMask.NameToLayer("ParticleEffects");
            Assert.That(layer, Is.GreaterThanOrEqualTo(0), "ParticleEffects layer must exist for charge VFX.");
        }
    }
}
