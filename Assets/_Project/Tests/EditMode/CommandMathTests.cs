using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class CommandMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void InputToWorldXZ_WithZeroCameraYaw_IsIdentity()
        {
            Vector2 result = CommandMath.InputToWorldXZ(new Vector2(0.5f, -0.25f), 0f);

            Assert.That(result.x, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(result.y, Is.EqualTo(-0.25f).Within(Tolerance));
        }

        [Test]
        public void InputToWorldXZ_WithCameraYaw90_TurnsForwardIntoWorldX()
        {
            Vector2 result = CommandMath.InputToWorldXZ(Vector2.up, 90f);

            Assert.That(result.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(result.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void InputToWorldXZ_ClampsToUnitLength()
        {
            Vector2 result = CommandMath.InputToWorldXZ(new Vector2(1f, 1f), 0f);

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void AimDirection_InsideDeadzone_IsZero()
        {
            Vector2 result = CommandMath.AimDirection(Vector3.zero, new Vector3(0.1f, 5f, 0.1f), 0.3f);

            Assert.That(result, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AimDirection_OutsideDeadzone_IsNormalizedXZ()
        {
            Vector2 result = CommandMath.AimDirection(new Vector3(1f, 0f, 1f), new Vector3(4f, 7f, 5f), 0.3f);

            Assert.That(result.x, Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(result.y, Is.EqualTo(0.8f).Within(Tolerance));
        }

        [Test]
        public void TryProjectToPlane_HitsThePlaneAtTheRequestedHeight()
        {
            var ray = new Ray(new Vector3(0f, 10f, -10f), new Vector3(0f, -1f, 1f).normalized);

            bool hit = CommandMath.TryProjectToPlane(ray, 0f, out Vector3 point);

            Assert.That(hit, Is.True);
            Assert.That(point.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(point.z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TryProjectToPlane_RayPointingAway_Misses()
        {
            var ray = new Ray(new Vector3(0f, 10f, 0f), Vector3.up);

            bool hit = CommandMath.TryProjectToPlane(ray, 0f, out _);

            Assert.That(hit, Is.False);
        }
    }
}
