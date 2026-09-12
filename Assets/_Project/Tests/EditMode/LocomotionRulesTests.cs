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

        [Test]
        public void Stride_StepsOncePerStrideAtWalkingSpeed()
        {
            float accumulated = 0f;
            int steps = 0;
            for (int i = 0; i < 100; i++)
            {
                if (LocomotionRules.Stride(ref accumulated, Vector2.up, 5f, 0.02f, 1.6f))
                    steps++;
            }

            // 100 frames at 5 m/s and 20 ms = 10 m travelled = 6 full strides of 1.6 m.
            Assert.That(steps, Is.EqualTo(6));
        }

        [Test]
        public void Stride_HitchFrame_LandsAtMostOneStep()
        {
            float accumulated = 0f;
            Assert.That(LocomotionRules.Stride(ref accumulated, Vector2.up, 5f, 5f, 1.6f), Is.True);
            Assert.That(accumulated, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(LocomotionRules.Stride(ref accumulated, Vector2.up, 5f, 0.02f, 1.6f), Is.False);
        }

        [Test]
        public void Stride_IdleOrInvalid_ResetsAndNeverSteps()
        {
            float accumulated = 1.5f;
            Assert.That(LocomotionRules.Stride(ref accumulated, Vector2.zero, 5f, 0.02f, 1.6f), Is.False);
            Assert.That(accumulated, Is.EqualTo(0f));
            accumulated = 1.5f;
            Assert.That(LocomotionRules.Stride(ref accumulated, Vector2.up, 5f, 0.02f, 0f), Is.False);
            Assert.That(accumulated, Is.EqualTo(0f));
            accumulated = 1.5f;
            Assert.That(LocomotionRules.Stride(ref accumulated, Vector2.up, 0f, 0.02f, 1.6f), Is.False);
            Assert.That(accumulated, Is.EqualTo(0f));
        }
    }
}
