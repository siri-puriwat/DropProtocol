using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class HudFormatTests
    {
        private static readonly ProtocolDirection[] Sequence =
            { ProtocolDirection.Up, ProtocolDirection.Down, ProtocolDirection.Right };

        [Test]
        public void Clock_WithLimit_ShowsRemaining()
        {
            Assert.That(HudFormat.Clock(15f, 200f), Is.EqualTo("3:05"));
        }

        [Test]
        public void Clock_WithoutLimit_ShowsElapsed()
        {
            Assert.That(HudFormat.Clock(65f, 0f), Is.EqualTo("1:05"));
        }

        [Test]
        public void Clock_NeverGoesNegative()
        {
            Assert.That(HudFormat.Clock(500f, 200f), Is.EqualTo("0:00"));
        }

        [Test]
        public void CooldownLabel_ReadyAtZero_CeilsOtherwise()
        {
            Assert.That(HudFormat.CooldownLabel(0f), Is.EqualTo("READY"));
            Assert.That(HudFormat.CooldownLabel(2.1f), Is.EqualTo("3 s"));
        }

        [Test]
        public void HealthFraction_ClampsAndHandlesZeroMax()
        {
            Assert.That(HudFormat.HealthFraction(50, 100), Is.EqualTo(0.5f));
            Assert.That(HudFormat.HealthFraction(-5, 100), Is.EqualTo(0f));
            Assert.That(HudFormat.HealthFraction(10, 0), Is.EqualTo(0f));
        }

        [Test]
        public void AmmoLabel_SwapsToReloading()
        {
            Assert.That(HudFormat.AmmoLabel(12, 30, false), Is.EqualTo("12 / 30"));
            Assert.That(HudFormat.AmmoLabel(0, 30, true), Is.EqualTo("RELOADING"));
        }

        [Test]
        public void ResultTitle_DistinguishesOutcomes()
        {
            Assert.That(HudFormat.ResultTitle(MissionPhase.Complete, MissionOutcome.Extracted), Is.EqualTo("MISSION COMPLETE"));
            Assert.That(HudFormat.ResultTitle(MissionPhase.Failed, MissionOutcome.TimedOut), Does.Contain("Time expired"));
            Assert.That(HudFormat.ResultTitle(MissionPhase.Failed, MissionOutcome.SquadWiped), Does.Contain("Squad wiped"));
            Assert.That(HudFormat.ResultTitle(MissionPhase.Active, MissionOutcome.None), Is.Empty);
        }

        [Test]
        public void SquadName_LabelsSlotAndBot()
        {
            Assert.That(HudFormat.SquadName(0, false), Is.EqualTo("P1"));
            Assert.That(HudFormat.SquadName(2, true), Is.EqualTo("P3 BOT"));
            Assert.That(HudFormat.SquadName(-1, false), Is.EqualTo("P?"));
        }

        [Test]
        public void SequenceGlyphs_NothingEntered_IsPlain()
        {
            string glyphs = HudFormat.SequenceGlyphs(Sequence, new ProtocolDirection[4], 0, "#FFF");

            Assert.That(glyphs, Is.EqualTo("↑↓→"));
        }

        [Test]
        public void SequenceGlyphs_MatchedPrefix_IsHighlighted()
        {
            var entered = new[] { ProtocolDirection.Up, ProtocolDirection.Down, ProtocolDirection.Up, ProtocolDirection.Up };

            string glyphs = HudFormat.SequenceGlyphs(Sequence, entered, 2, "#FFF");

            Assert.That(glyphs, Is.EqualTo("<color=#FFF>↑↓</color>→"));
        }

        [Test]
        public void MatchedPrefix_MismatchOrOverflow_IsZero()
        {
            var wrong = new[] { ProtocolDirection.Down, ProtocolDirection.Down, ProtocolDirection.Up, ProtocolDirection.Up };
            var right = new[] { ProtocolDirection.Up, ProtocolDirection.Down, ProtocolDirection.Right, ProtocolDirection.Up };

            Assert.That(HudFormat.MatchedPrefix(Sequence, wrong, 1), Is.Zero);
            Assert.That(HudFormat.MatchedPrefix(Sequence, right, 4), Is.Zero);
            Assert.That(HudFormat.MatchedPrefix(Sequence, right, 3), Is.EqualTo(3));
        }
    }
}
