using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class EnemyBrainTests
    {
        private const float AttackRange = 2f;
        private const float PreferredRange = 2f;
        private const float MinRange = 0f;
        private const double Cooldown = 1.0;
        private const double Windup = 0.25;

        private EnemyBrain m_brain;

        [SetUp]
        public void SetUp()
        {
            m_brain = new EnemyBrain(AttackRange, PreferredRange, MinRange, Cooldown, Windup);
        }

        [Test]
        public void NewBrain_IsIdle()
        {
            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Idle));
        }

        [Test]
        public void Update_WithFarTarget_ChasesAndApproaches()
        {
            m_brain.Update(true, 10f, 0.0);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Chase));
            Assert.That(m_brain.Move, Is.EqualTo(EnemyMove.Approach));
        }

        [Test]
        public void Update_WithoutTarget_ReturnsToIdle()
        {
            m_brain.Update(true, 10f, 0.0);
            m_brain.Update(false, 0f, 0.1);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Idle));
        }

        [Test]
        public void Update_InRangeAndReady_EntersAttack()
        {
            m_brain.Update(true, 1f, 0.0);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Attack));
            Assert.That(m_brain.AttackTriggered, Is.False);
        }

        [Test]
        public void Update_BeforeWindupEnds_DoesNotTrigger()
        {
            m_brain.Update(true, 1f, 0.0);
            m_brain.Update(true, 1f, Windup * 0.5);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Attack));
            Assert.That(m_brain.AttackTriggered, Is.False);
        }

        [Test]
        public void Update_AtWindupEnd_TriggersOnceThenChases()
        {
            m_brain.Update(true, 1f, 0.0);
            m_brain.Update(true, 1f, Windup);

            Assert.That(m_brain.AttackTriggered, Is.True);
            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Chase));

            m_brain.Update(true, 1f, Windup + 0.01);
            Assert.That(m_brain.AttackTriggered, Is.False);
        }

        [Test]
        public void Update_InRangeDuringCooldown_StaysChasing()
        {
            m_brain.Update(true, 1f, 0.0);
            m_brain.Update(true, 1f, Windup);
            m_brain.Update(true, 1f, Windup + Cooldown * 0.5);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Chase));
            Assert.That(m_brain.Move, Is.EqualTo(EnemyMove.Hold));
        }

        [Test]
        public void Update_AfterCooldown_AttacksAgain()
        {
            m_brain.Update(true, 1f, 0.0);
            m_brain.Update(true, 1f, Windup);
            m_brain.Update(true, 1f, Windup + Cooldown);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Attack));
        }

        [Test]
        public void Update_TargetLostDuringWindup_Idles()
        {
            m_brain.Update(true, 1f, 0.0);
            m_brain.Update(false, 0f, 0.1);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(m_brain.AttackTriggered, Is.False);
        }

        [Test]
        public void Kill_IsSticky()
        {
            m_brain.Kill();
            m_brain.Update(true, 1f, 5.0);

            Assert.That(m_brain.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(m_brain.AttackTriggered, Is.False);
        }
    }
}
