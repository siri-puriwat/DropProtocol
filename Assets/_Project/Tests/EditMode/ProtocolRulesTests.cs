using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class ProtocolRulesTests
    {
        private static readonly ProtocolDirection[] Supply =
            { ProtocolDirection.Down, ProtocolDirection.Down, ProtocolDirection.Up, ProtocolDirection.Right };

        private static readonly ProtocolDirection[] Sentry =
            { ProtocolDirection.Up, ProtocolDirection.Right, ProtocolDirection.Down, ProtocolDirection.Up };

        private static readonly ProtocolDirection[] Strike =
        {
            ProtocolDirection.Up, ProtocolDirection.Down, ProtocolDirection.Right, ProtocolDirection.Left,
            ProtocolDirection.Up
        };

        private static readonly ProtocolDirection[][] Loadout = { Supply, Sentry, Strike };

        [Test]
        public void TryPressedDirection_RisingEdge_ReturnsDirection()
        {
            Assert.That(ProtocolRules.TryPressedDirection(Vector2.zero, Vector2.up, out var direction), Is.True);
            Assert.That(direction, Is.EqualTo(ProtocolDirection.Up));

            Assert.That(ProtocolRules.TryPressedDirection(Vector2.zero, Vector2.down, out direction), Is.True);
            Assert.That(direction, Is.EqualTo(ProtocolDirection.Down));

            Assert.That(ProtocolRules.TryPressedDirection(Vector2.zero, Vector2.left, out direction), Is.True);
            Assert.That(direction, Is.EqualTo(ProtocolDirection.Left));

            Assert.That(ProtocolRules.TryPressedDirection(Vector2.zero, Vector2.right, out direction), Is.True);
            Assert.That(direction, Is.EqualTo(ProtocolDirection.Right));
        }

        [Test]
        public void TryPressedDirection_Held_DoesNotRepeat()
        {
            Assert.That(ProtocolRules.TryPressedDirection(Vector2.up, Vector2.up, out _), Is.False);
        }

        [Test]
        public void TryPressedDirection_Released_False()
        {
            Assert.That(ProtocolRules.TryPressedDirection(Vector2.up, Vector2.zero, out _), Is.False);
        }

        [Test]
        public void TryPressedDirection_BelowThreshold_False()
        {
            Assert.That(ProtocolRules.TryPressedDirection(Vector2.zero, new Vector2(0f, 0.4f), out _), Is.False);
        }

        [Test]
        public void TryPressedDirection_Diagonal_VerticalWins()
        {
            Assert.That(ProtocolRules.TryPressedDirection(Vector2.zero, new Vector2(1f, -1f), out var direction),
                Is.True);
            Assert.That(direction, Is.EqualTo(ProtocolDirection.Down));
        }

        [Test]
        public void Match_Empty_Pending()
        {
            Assert.That(ProtocolRules.Match(Loadout, new List<ProtocolDirection>(), out _),
                Is.EqualTo(ProtocolMatch.Pending));
        }

        [Test]
        public void Match_SharedPrefix_Pending()
        {
            var entered = new List<ProtocolDirection> { ProtocolDirection.Up };
            Assert.That(ProtocolRules.Match(Loadout, entered, out _), Is.EqualTo(ProtocolMatch.Pending));
        }

        [Test]
        public void Match_CompleteSequence_MatchedWithSlot()
        {
            Assert.That(ProtocolRules.Match(Loadout, Sentry, out int slot), Is.EqualTo(ProtocolMatch.Matched));
            Assert.That(slot, Is.EqualTo(1));

            Assert.That(ProtocolRules.Match(Loadout, Strike, out slot), Is.EqualTo(ProtocolMatch.Matched));
            Assert.That(slot, Is.EqualTo(2));
        }

        [Test]
        public void Match_NoSequenceStartsWithEntered_Rejected()
        {
            var entered = new List<ProtocolDirection> { ProtocolDirection.Left };
            Assert.That(ProtocolRules.Match(Loadout, entered, out int slot), Is.EqualTo(ProtocolMatch.Rejected));
            Assert.That(slot, Is.EqualTo(-1));
        }

        [Test]
        public void Match_LongerThanEverySequence_Rejected()
        {
            var entered = new List<ProtocolDirection>(Strike) { ProtocolDirection.Up };
            Assert.That(ProtocolRules.Match(Loadout, entered, out _), Is.EqualTo(ProtocolMatch.Rejected));
        }

        [Test]
        public void IsPrefixFree_ShippedLoadout_True()
        {
            Assert.That(ProtocolRules.IsPrefixFree(Loadout), Is.True);
        }

        [Test]
        public void IsPrefixFree_PrefixOrDuplicate_False()
        {
            var prefix = new[] { ProtocolDirection.Up, ProtocolDirection.Right };
            Assert.That(ProtocolRules.IsPrefixFree(new[] { Sentry, prefix }), Is.False);
            Assert.That(ProtocolRules.IsPrefixFree(new[] { Sentry, Sentry }), Is.False);
        }

        [Test]
        public void PackUnpack_RoundTrips()
        {
            var buffer = new ProtocolDirection[ProtocolRules.MaxSequenceLength];

            Assert.That(ProtocolRules.Unpack(ProtocolRules.Pack(new List<ProtocolDirection>()), buffer), Is.Zero);

            var one = new List<ProtocolDirection> { ProtocolDirection.Left };
            Assert.That(ProtocolRules.Unpack(ProtocolRules.Pack(one), buffer), Is.EqualTo(1));
            Assert.That(buffer[0], Is.EqualTo(ProtocolDirection.Left));

            var full = new List<ProtocolDirection>
            {
                ProtocolDirection.Right, ProtocolDirection.Left, ProtocolDirection.Down, ProtocolDirection.Up,
                ProtocolDirection.Right, ProtocolDirection.Right, ProtocolDirection.Left, ProtocolDirection.Down
            };
            Assert.That(ProtocolRules.Unpack(ProtocolRules.Pack(full), buffer), Is.EqualTo(8));
            for (int i = 0; i < full.Count; i++)
            {
                Assert.That(buffer[i], Is.EqualTo(full[i]));
            }
        }

        [Test]
        public void Unpack_CapsToBufferLength()
        {
            var packed = ProtocolRules.Pack(Strike);
            var small = new ProtocolDirection[2];
            Assert.That(ProtocolRules.Unpack(packed, small), Is.EqualTo(2));
            Assert.That(small[1], Is.EqualTo(ProtocolDirection.Down));
        }

        [Test]
        public void TargetPoint_FlattensForward()
        {
            var point = ProtocolRules.TargetPoint(new Vector3(1f, 0.1f, 1f), new Vector3(0f, 0.7f, 0.7f), 5f);
            Assert.That(point.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(point.z, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void TargetPoint_ZeroForward_FallsBackToWorldForward()
        {
            var point = ProtocolRules.TargetPoint(Vector3.zero, Vector3.up, 3f);
            Assert.That(point, Is.EqualTo(new Vector3(0f, 0f, 3f)));
        }

        [TestCase(false, 0f, true, true)]
        [TestCase(true, 0f, true, false)]
        [TestCase(false, 0.5f, true, false)]
        [TestCase(false, 0f, false, false)]
        public void CanCall_RequiresStandingReadyAndOpen(bool downed, float cooldown, bool open, bool expected)
        {
            Assert.That(ProtocolRules.CanCall(downed, cooldown, open), Is.EqualTo(expected));
        }

        [Test]
        public void CooldownStep_ClampsAtZero()
        {
            Assert.That(ProtocolRules.CooldownStep(1f, 0.25f), Is.EqualTo(0.75f));
            Assert.That(ProtocolRules.CooldownStep(0.1f, 0.25f), Is.Zero);
        }

        [Test]
        public void HasTimedOut_StrictlyAfterTimeout()
        {
            Assert.That(ProtocolRules.HasTimedOut(10.0, 11.5, 1.5f), Is.False);
            Assert.That(ProtocolRules.HasTimedOut(10.0, 11.6, 1.5f), Is.True);
        }
    }
}
