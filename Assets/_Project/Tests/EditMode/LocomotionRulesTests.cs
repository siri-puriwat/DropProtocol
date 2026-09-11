using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class LocomotionRulesTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void WorldVelocity_DividesPlanarDeltaByTime()
        {
            Vector2 velocity = LocomotionRules.WorldVelocity(Vector3.zero, new Vector3(1f, 5f, 2f), 0.5f);

            Assert.That(velocity.x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(velocity.y, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void WorldVelocity_ZeroDeltaTime_IsZero()
        {
            Assert.That(LocomotionRules.WorldVelocity(Vector3.zero, Vector3.one, 0f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ToLocal_FacingWorldForward_IsIdentity()
        {
            Vector2 local = LocomotionRules.ToLocal(new Vector2(1f, 2f), 0f);

            Assert.That(local.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(local.y, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void ToLocal_FacingEast_MapsWorldXToForward()
        {
            Vector2 local = LocomotionRules.ToLocal(new Vector2(1f, 0f), 90f);

            Assert.That(local.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(local.y, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ToLocal_FacingSouth_MovingNorthIsBackward()
        {
            Vector2 local = LocomotionRules.ToLocal(new Vector2(0f, 1f), 180f);

            Assert.That(local.y, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void ToLocal_WrappedYaw_MatchesUnwrapped()
        {
            Vector2 wrapped = LocomotionRules.ToLocal(new Vector2(1f, 1f), 450f);
            Vector2 plain = LocomotionRules.ToLocal(new Vector2(1f, 1f), 90f);

            Assert.That(wrapped.x, Is.EqualTo(plain.x).Within(Tolerance));
            Assert.That(wrapped.y, Is.EqualTo(plain.y).Within(Tolerance));
        }

        [Test]
        public void Normalize_ScalesByMoveSpeedAndClampsToUnit()
        {
            Vector2 half = LocomotionRules.Normalize(new Vector2(0f, 3f), 6f, 0f);
            Vector2 over = LocomotionRules.Normalize(new Vector2(12f, 0f), 6f, 0f);

            Assert.That(half.y, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(over.x, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Normalize_BelowDeadzone_IsZero()
        {
            Assert.That(LocomotionRules.Normalize(new Vector2(0.1f, 0f), 6f, 0.05f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Normalize_ZeroMoveSpeed_IsZero()
        {
            Assert.That(LocomotionRules.Normalize(new Vector2(3f, 0f), 0f, 0f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Damp_ConvergesTowardTarget()
        {
            Vector2 value = Vector2.zero;
            for (int i = 0; i < 60; i++)
                value = LocomotionRules.Damp(value, Vector2.up, 12f, 1f / 60f);

            Assert.That(value.y, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void Damp_NeverOvershoots()
        {
            Vector2 value = LocomotionRules.Damp(Vector2.zero, Vector2.up, 12f, 10f);

            Assert.That(value.y, Is.LessThanOrEqualTo(1f));
        }
    }
}
