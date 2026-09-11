using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class EnemyRulesTests
    {
        [Test]
        public void ResolveMove_BeyondPreferredRange_Approaches()
        {
            Assert.That(EnemyRules.ResolveMove(10f, 8f, 5f), Is.EqualTo(EnemyMove.Approach));
        }

        [Test]
        public void ResolveMove_InsideMinRange_Retreats()
        {
            Assert.That(EnemyRules.ResolveMove(3f, 8f, 5f), Is.EqualTo(EnemyMove.Retreat));
        }

        [Test]
        public void ResolveMove_BetweenRanges_Holds()
        {
            Assert.That(EnemyRules.ResolveMove(6f, 8f, 5f), Is.EqualTo(EnemyMove.Hold));
        }

        [Test]
        public void ResolveMove_ZeroMinRange_NeverRetreats()
        {
            Assert.That(EnemyRules.ResolveMove(0f, 1.6f, 0f), Is.EqualTo(EnemyMove.Hold));
        }

        [Test]
        public void InAttackRange_AtExactRange_IsTrue()
        {
            Assert.That(EnemyRules.InAttackRange(1.6f, 1.6f), Is.True);
            Assert.That(EnemyRules.InAttackRange(1.61f, 1.6f), Is.False);
        }

        [Test]
        public void FlatDistance_IgnoresHeight()
        {
            float distance = EnemyRules.FlatDistance(new Vector3(0f, 0f, 0f), new Vector3(3f, 10f, 4f));

            Assert.That(distance, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void RetreatPoint_MovesAwayFromThreatOnXZ()
        {
            Vector3 point = EnemyRules.RetreatPoint(new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 0f), 3f);

            Assert.That(point.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(point.z, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void RetreatPoint_OnTopOfThreat_StillMoves()
        {
            Vector3 self = new Vector3(1f, 0f, 1f);
            Vector3 point = EnemyRules.RetreatPoint(self, self, 3f);

            Assert.That((point - self).magnitude, Is.EqualTo(3f).Within(0.001f));
        }
    }
}
