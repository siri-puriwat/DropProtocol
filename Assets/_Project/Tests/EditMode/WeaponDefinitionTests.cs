using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class WeaponDefinitionTests
    {
        [Test]
        public void Create_DefaultsToOnePellet()
        {
            WeaponDefinition rifle = WeaponDefinition.Create(20, 10f, 30, 2f, 3f, 60f);

            Assert.That(rifle.PelletCount, Is.EqualTo(1));
            Object.DestroyImmediate(rifle);
        }

        [Test]
        public void Create_KeepsThePelletCount()
        {
            WeaponDefinition shotgun = WeaponDefinition.Create(9, 1.2f, 6, 2.8f, 14f, 25f, pelletCount: 8);

            Assert.That(shotgun.PelletCount, Is.EqualTo(8));
            Object.DestroyImmediate(shotgun);
        }
    }
}
