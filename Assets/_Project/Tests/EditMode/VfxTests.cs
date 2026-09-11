using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class VfxTests
    {
        [Test]
        public void Spawn_WithoutPrefab_IsANoOp()
        {
            Assert.That(Vfx.Spawn(null, Vector3.one, Vector3.forward), Is.Null);
        }

        [Test]
        public void Rotation_FacesForward()
        {
            Quaternion rotation = Vfx.Rotation(Vector3.right);

            Assert.That(Quaternion.Angle(rotation, Quaternion.LookRotation(Vector3.right)), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Rotation_ZeroForward_IsIdentity()
        {
            Assert.That(Vfx.Rotation(Vector3.zero), Is.EqualTo(Quaternion.identity));
        }
    }
}
