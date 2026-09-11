using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class ProjectileMathTests
    {
        [Test]
        public void LaunchDirection_IsFlatAndUnitLength()
        {
            Vector3 direction = ProjectileMath.LaunchDirection(new Vector3(0f, 0.9f, 0f), new Vector3(3f, 5f, 4f));

            Assert.That(direction.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(direction.x, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(direction.z, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void LaunchDirection_Degenerate_FallsBackToForward()
        {
            Vector3 origin = new Vector3(1f, 0f, 1f);

            Assert.That(ProjectileMath.LaunchDirection(origin, origin + Vector3.up), Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void StepLength_ScalesWithDeltaTime()
        {
            Assert.That(ProjectileMath.StepLength(12f, 0.5f), Is.EqualTo(6f).Within(0.001f));
            Assert.That(ProjectileMath.StepLength(12f, -1f), Is.Zero);
        }

        [Test]
        public void HasExpired_AtLifetime()
        {
            Assert.That(ProjectileMath.HasExpired(10.0, 1.5f, 11.4), Is.False);
            Assert.That(ProjectileMath.HasExpired(10.0, 1.5f, 11.5), Is.True);
        }
    }
}
