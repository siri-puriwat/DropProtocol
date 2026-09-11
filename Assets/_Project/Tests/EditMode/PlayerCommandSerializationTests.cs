using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class PlayerCommandSerializationTests
    {
        private const float Tolerance = 1e-6f;

        [Test]
        public void RoundTrip_PreservesVectorsAndButtons()
        {
            var original = new PlayerCommand
            {
                Move = new Vector2(0.25f, -0.75f),
                Aim = new Vector2(-1f, 0f),
                Fire = true,
                Reload = false,
                Interact = true,
            };

            PlayerCommand restored = RoundTrip(original);

            Assert.That(restored.Move.x, Is.EqualTo(original.Move.x).Within(Tolerance));
            Assert.That(restored.Move.y, Is.EqualTo(original.Move.y).Within(Tolerance));
            Assert.That(restored.Aim.x, Is.EqualTo(original.Aim.x).Within(Tolerance));
            Assert.That(restored.Aim.y, Is.EqualTo(original.Aim.y).Within(Tolerance));
            Assert.That(restored.Fire, Is.True);
            Assert.That(restored.Reload, Is.False);
            Assert.That(restored.Interact, Is.True);
        }

        [Test]
        public void RoundTrip_None_StaysNone()
        {
            PlayerCommand restored = RoundTrip(PlayerCommand.None);

            Assert.That(restored.HasMove, Is.False);
            Assert.That(restored.HasAim, Is.False);
            Assert.That(restored.Fire | restored.Reload | restored.Interact, Is.False);
        }

        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        [TestCase(true, true, true)]
        public void RoundTrip_EachButtonIsIndependent(bool fire, bool reload, bool interact)
        {
            var original = new PlayerCommand { Fire = fire, Reload = reload, Interact = interact };

            PlayerCommand restored = RoundTrip(original);

            Assert.That(restored.Fire, Is.EqualTo(fire));
            Assert.That(restored.Reload, Is.EqualTo(reload));
            Assert.That(restored.Interact, Is.EqualTo(interact));
        }

        [Test]
        public void Serialized_UsesTwoVectorsAndOneByte()
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            var command = new PlayerCommand { Fire = true };
            writer.WriteNetworkSerializable(command);

            Assert.That(writer.Position, Is.EqualTo(2 * sizeof(float) * 2 + 1));
        }

        private static PlayerCommand RoundTrip(PlayerCommand command)
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteNetworkSerializable(command);

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadNetworkSerializable(out PlayerCommand restored);
            return restored;
        }
    }
}
