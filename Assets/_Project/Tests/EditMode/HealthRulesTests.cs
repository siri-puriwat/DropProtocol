using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class HealthRulesTests
    {
        [Test]
        public void AfterDamage_SubtractsAmount()
        {
            Assert.That(HealthRules.AfterDamage(100, 25), Is.EqualTo(75));
        }

        [Test]
        public void AfterDamage_ClampsAtZero()
        {
            Assert.That(HealthRules.AfterDamage(10, 25), Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void AfterDamage_IgnoresNonPositiveAmount(int amount)
        {
            Assert.That(HealthRules.AfterDamage(40, amount), Is.EqualTo(40));
        }

        [Test]
        public void AfterHeal_AddsAmountUpToMax()
        {
            Assert.That(HealthRules.AfterHeal(50, 30, 100), Is.EqualTo(80));
            Assert.That(HealthRules.AfterHeal(90, 30, 100), Is.EqualTo(100));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void AfterHeal_IgnoresNonPositiveAmount(int amount)
        {
            Assert.That(HealthRules.AfterHeal(40, amount, 100), Is.EqualTo(40));
        }

        [Test]
        public void Clamp_KeepsValueWithinRange()
        {
            Assert.That(HealthRules.Clamp(150, 100), Is.EqualTo(100));
            Assert.That(HealthRules.Clamp(-1, 100), Is.Zero);
            Assert.That(HealthRules.Clamp(42, 100), Is.EqualTo(42));
        }

        [Test]
        public void ReviveHitPoints_UsesFractionOfMax()
        {
            Assert.That(HealthRules.ReviveHitPoints(100, 0.5f), Is.EqualTo(50));
        }

        [Test]
        public void ReviveHitPoints_NeverBelowOne()
        {
            Assert.That(HealthRules.ReviveHitPoints(1, 0.05f), Is.EqualTo(1));
        }
    }
}
