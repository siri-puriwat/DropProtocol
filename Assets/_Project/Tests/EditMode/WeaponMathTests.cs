using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class WeaponMathTests
    {
        private const float Tolerance = 1e-3f;

        [Test]
        public void ResolveAim_WithAim_ReturnsNormalizedAim()
        {
            Vector2 aim = WeaponMath.ResolveAim(new Vector2(0f, 3f), 90f);

            Assert.That(aim.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(aim.y, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ResolveAim_WithoutAim_UsesFacing()
        {
            Vector2 aim = WeaponMath.ResolveAim(Vector2.zero, 90f);

            Assert.That(aim.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(aim.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void SpreadDirection_MidRandom_MatchesAim()
        {
            Vector3 direction = WeaponMath.SpreadDirection(new Vector2(0f, 1f), 10f, 0.5f);

            Assert.That(direction.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(direction.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(direction.z, Is.EqualTo(1f).Within(Tolerance));
        }

        [TestCase(0f, -5f)]
        [TestCase(1f, 5f)]
        public void SpreadDirection_ExtremeRandom_DeviatesByHalfSpread(float unitRandom, float expectedDegrees)
        {
            Vector3 direction = WeaponMath.SpreadDirection(new Vector2(0f, 1f), 10f, unitRandom);

            float signedAngle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
            Assert.That(signedAngle, Is.EqualTo(expectedDegrees).Within(Tolerance));
        }

        [Test]
        public void SpreadDirection_IsUnitLengthAndHorizontal()
        {
            Vector3 direction = WeaponMath.SpreadDirection(new Vector2(1f, 1f), 20f, 0.8f);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(direction.y, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
