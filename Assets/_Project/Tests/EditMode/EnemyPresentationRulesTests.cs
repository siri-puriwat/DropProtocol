using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class EnemyPresentationRulesTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void AttackSpeed_LandsHitFrameAtWindupEnd()
        {
            float speed = EnemyPresentationRules.AttackSpeed(0.5f, 0.6f, 0.25f);

            Assert.That(0.5f * 0.6f / speed, Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void AttackSpeed_IsClampedToPlayableRange()
        {
            Assert.That(EnemyPresentationRules.AttackSpeed(0.2f, 0.5f, 5f), Is.EqualTo(EnemyPresentationRules.MinAttackSpeed));
            Assert.That(EnemyPresentationRules.AttackSpeed(2f, 1f, 0.05f), Is.EqualTo(EnemyPresentationRules.MaxAttackSpeed));
        }

        [Test]
        public void AttackSpeed_WithoutClipOrWindup_IsOne()
        {
            Assert.That(EnemyPresentationRules.AttackSpeed(0f, 0.6f, 0.25f), Is.EqualTo(1f));
            Assert.That(EnemyPresentationRules.AttackSpeed(0.5f, 0.6f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void IsTelegraphing_OnlyDuringAttack()
        {
            Assert.That(EnemyPresentationRules.IsTelegraphing(EnemyState.Attack), Is.True);
            Assert.That(EnemyPresentationRules.IsTelegraphing(EnemyState.Chase), Is.False);
            Assert.That(EnemyPresentationRules.IsTelegraphing(EnemyState.Dead), Is.False);
        }
    }
}
