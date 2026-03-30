using NUnit.Framework;
using Unity.FPS.Game;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void Heal_ClampsToMaxHealthAndInvokesOnHealedWithTrueAmount()
        {
            var go = new GameObject("Health_HealTest");
            var health = go.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 9f;

            float healedAmount = -1f;
            int healEventCount = 0;
            health.OnHealed += amount =>
            {
                healedAmount = amount;
                healEventCount++;
            };

            health.Heal(5f);

            Assert.AreEqual(10f, health.CurrentHealth, 0.0001f);
            Assert.AreEqual(1, healEventCount);
            Assert.AreEqual(1f, healedAmount, 0.0001f); // (10 - 9)

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_WhenInvincible_DoesNotChangeHealthOrInvokeEvents()
        {
            var go = new GameObject("Health_TakeDamageInvincibleTest");
            var health = go.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 10f;
            health.Invincible = true;

            float damagedAmount = -1f;
            int damagedEventCount = 0;
            int dieEventCount = 0;

            health.OnDamaged += (amount, _) =>
            {
                damagedAmount = amount;
                damagedEventCount++;
            };
            health.OnDie += () => dieEventCount++;

            health.TakeDamage(3f, null);

            Assert.AreEqual(10f, health.CurrentHealth, 0.0001f);
            Assert.AreEqual(0, damagedEventCount);
            Assert.AreEqual(0, dieEventCount);
            Assert.IsFalse(health.IsDead);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_ClampsAndInvokesOnDamagedAndOnDieOnce()
        {
            var go = new GameObject("Health_TakeDamageDeathTest");
            var health = go.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 2f;
            health.Invincible = false;

            float damagedAmount = -1f;
            int damagedEventCount = 0;
            int dieEventCount = 0;

            health.OnDamaged += (amount, _) =>
            {
                damagedAmount = amount;
                damagedEventCount++;
            };
            health.OnDie += () => dieEventCount++;

            health.TakeDamage(5f, null); // overkill

            Assert.AreEqual(0f, health.CurrentHealth, 0.0001f);
            Assert.AreEqual(1, damagedEventCount);
            Assert.AreEqual(2f, damagedAmount, 0.0001f); // (2 - 0)
            Assert.AreEqual(1, dieEventCount);
            Assert.IsTrue(health.IsDead);

            // Extra damage after death: should not invoke OnDie again.
            health.TakeDamage(1f, null);
            Assert.AreEqual(1, dieEventCount);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Kill_InvokesOnDamagedWithMaxHealthAndOnDieOnlyOnce()
        {
            var go = new GameObject("Health_KillTest");
            var health = go.AddComponent<Health>();
            health.MaxHealth = 10f;
            health.CurrentHealth = 5f;
            health.Invincible = false;

            float damagedAmount = -1f;
            int damagedEventCount = 0;
            int dieEventCount = 0;

            health.OnDamaged += (amount, damageSource) =>
            {
                damagedAmount = amount;
                damagedEventCount++;
                Assert.IsNull(damageSource);
            };
            health.OnDie += () => dieEventCount++;

            health.Kill();

            Assert.AreEqual(0f, health.CurrentHealth, 0.0001f);
            Assert.AreEqual(1, damagedEventCount);
            Assert.AreEqual(10f, damagedAmount, 0.0001f); // uses MaxHealth
            Assert.AreEqual(1, dieEventCount);
            Assert.IsTrue(health.IsDead);

            health.Kill();

            // HandleDeath short-circuits once dead; Kill still invokes OnDamaged.
            Assert.AreEqual(1, dieEventCount);

            Object.DestroyImmediate(go);
        }
    }
}

