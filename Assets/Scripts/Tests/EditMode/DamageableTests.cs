using System.Reflection;
using NUnit.Framework;
using Unity.FPS.Game;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class DamageableTests
    {
        private sealed class ExplosionShieldStub : MonoBehaviour, IExplosionDamageShield
        {
            public bool ConsumeOnNextHit = true;
            public bool IsConsumed { get; private set; }

            public bool TryConsumeExplosionHit()
            {
                if (!ConsumeOnNextHit)
                    return false;

                ConsumeOnNextHit = false;
                IsConsumed = true;
                return true;
            }
        }

        static void InvokePrivateAwake(Damageable instance)
        {
            // In some edit-mode test contexts Unity doesn't reliably run Awake().
            // These unit tests need deterministic initialization.
            var method = typeof(Damageable).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(instance, null);
        }

        [Test]
        public void InflictDamage_NonExplosion_AppliesDamageMultiplierAndSelfSensibility()
        {
            var healthGo = new GameObject("Damageable_NonExplosion_Test");
            var health = healthGo.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 10f;

            var damageable = healthGo.AddComponent<Damageable>();
            damageable.DamageMultiplier = 2f;
            damageable.SensibilityToSelfdamage = 0.5f;

            InvokePrivateAwake(damageable);
            Assert.NotNull(damageable.Health);

            var attackerGo = new GameObject("Attacker");

            // Non-explosion: multiplier applies when not explosion damage.
            damageable.InflictDamage(3f, isExplosionDamage: false, damageSource: attackerGo);
            Assert.AreEqual(4f, health.CurrentHealth, 0.0001f); // 10 - (3*2)

            // Reset and test self sensibility.
            health.CurrentHealth = 10f;
            damageable.InflictDamage(3f, isExplosionDamage: false, damageSource: healthGo);
            Assert.AreEqual(7f, health.CurrentHealth, 0.0001f); // 10 - (3*2*0.5)

            Object.DestroyImmediate(attackerGo);
            Object.DestroyImmediate(healthGo);
        }

        [Test]
        public void InflictDamage_ExplosionDamage_SkipsDamageMultiplierButAppliesSelfSensibility()
        {
            var healthGo = new GameObject("Damageable_Explosion_NoShield_Test");
            var health = healthGo.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 10f;

            var damageable = healthGo.AddComponent<Damageable>();
            damageable.DamageMultiplier = 2f;
            damageable.SensibilityToSelfdamage = 0.5f;

            var attackerGo = new GameObject("Attacker");
            InvokePrivateAwake(damageable);

            // Explosion: damage multiplier should NOT apply.
            damageable.InflictDamage(3f, isExplosionDamage: true, damageSource: attackerGo);
            Assert.AreEqual(7f, health.CurrentHealth, 0.0001f); // 10 - 3

            // Explosion: self sensibility still applies.
            health.CurrentHealth = 10f;
            damageable.InflictDamage(3f, isExplosionDamage: true, damageSource: healthGo);
            Assert.AreEqual(8.5f, health.CurrentHealth, 0.0001f); // 10 - (3*0.5)

            Object.DestroyImmediate(attackerGo);
            Object.DestroyImmediate(healthGo);
        }

        [Test]
        public void InflictDamage_ExplosionDamage_IsAbsorbedByShield_ThenConsumesOnce()
        {
            var healthGo = new GameObject("Damageable_Explosion_ShieldAbsorb_Test");
            var health = healthGo.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 10f;

            var damageable = healthGo.AddComponent<Damageable>();
            damageable.DamageMultiplier = 2f; // should be skipped for explosion anyway
            damageable.SensibilityToSelfdamage = 0.5f;

            var attackerGo = new GameObject("Attacker");

            // Add an explosion shield under the health object.
            var shieldGo = new GameObject("ExplosionShieldStub");
            shieldGo.transform.SetParent(healthGo.transform);
            var shield = shieldGo.AddComponent<ExplosionShieldStub>();

            InvokePrivateAwake(damageable);

            damageable.InflictDamage(3f, isExplosionDamage: true, damageSource: attackerGo);

            Assert.AreEqual(10f, health.CurrentHealth, 0.0001f); // absorbed: no health change
            Assert.IsTrue(shield.IsConsumed);

            // Second explosion: shield should no longer consume.
            damageable.InflictDamage(3f, isExplosionDamage: true, damageSource: attackerGo);
            Assert.AreEqual(7f, health.CurrentHealth, 0.0001f); // 10 - 3

            Object.DestroyImmediate(shieldGo);
            Object.DestroyImmediate(attackerGo);
            Object.DestroyImmediate(healthGo);
        }
    }
}

