using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class PlayerCommandTests
    {
        [Test]
        public void None_HasNoMoveAimOrButtons()
        {
            PlayerCommand command = PlayerCommand.None;

            Assert.That(command.HasMove, Is.False);
            Assert.That(command.HasAim, Is.False);
            Assert.That(command.Fire, Is.False);
            Assert.That(command.Reload, Is.False);
            Assert.That(command.Interact, Is.False);
        }

        [Test]
        public void HasMoveAndHasAim_ReflectTheirVectors()
        {
            var command = new PlayerCommand { Move = new Vector2(0f, 1f), Aim = Vector2.zero };

            Assert.That(command.HasMove, Is.True);
            Assert.That(command.HasAim, Is.False);
        }
    }
}
