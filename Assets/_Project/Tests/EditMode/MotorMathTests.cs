using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class MotorMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void PlanarVelocity_MapsInputYToWorldZ()
        {
            Vector3 velocity = MotorMath.PlanarVelocity(new Vector2(0f, 1f), 6f);

            Assert.That(velocity, Is.EqualTo(new Vector3(0f, 0f, 6f)));
        }

        [Test]
        public void PlanarVelocity_DiagonalInput_NeverExceedsSpeed()
        {
            Vector3 velocity = MotorMath.PlanarVelocity(new Vector2(1f, 1f), 6f);

            Assert.That(velocity.magnitude, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void YawFromDirection_FollowsUnityYawConvention()
        {
            Assert.That(MotorMath.YawFromDirection(new Vector2(0f, 1f)), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(MotorMath.YawFromDirection(new Vector2(1f, 0f)), Is.EqualTo(90f).Within(Tolerance));
            Assert.That(MotorMath.YawFromDirection(new Vector2(0f, -1f)), Is.EqualTo(180f).Within(Tolerance));
        }

        [Test]
        public void ResolveTargetYaw_AimWinsOverMove()
        {
            float yaw = MotorMath.ResolveTargetYaw(new Vector2(1f, 0f), new Vector2(0f, 1f), 45f);

            Assert.That(yaw, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void ResolveTargetYaw_WithoutAim_FacesMoveDirection()
        {
            float yaw = MotorMath.ResolveTargetYaw(Vector2.zero, new Vector2(-1f, 0f), 45f);

            Assert.That(yaw, Is.EqualTo(-90f).Within(Tolerance));
        }

        [Test]
        public void ResolveTargetYaw_WithNeither_KeepsCurrentYaw()
        {
            float yaw = MotorMath.ResolveTargetYaw(Vector2.zero, Vector2.zero, 45f);

            Assert.That(yaw, Is.EqualTo(45f));
        }

        [Test]
        public void StepYaw_IsLimitedByTurnSpeed()
        {
            float yaw = MotorMath.StepYaw(0f, 90f, 100f, 0.5f);

            Assert.That(yaw, Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void StepYaw_TakesTheShortWayAcrossTheWrap()
        {
            float yaw = MotorMath.StepYaw(350f, 10f, 100f, 0.1f);

            Assert.That(Mathf.DeltaAngle(yaw, 0f), Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
