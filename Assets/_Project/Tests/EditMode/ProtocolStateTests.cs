using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class ProtocolStateTests
    {
        private const float Timeout = 1.5f;

        private static readonly ProtocolDirection[] Supply =
            { ProtocolDirection.Down, ProtocolDirection.Down, ProtocolDirection.Up, ProtocolDirection.Right };

        private static readonly ProtocolDirection[] Sentry =
            { ProtocolDirection.Up, ProtocolDirection.Right, ProtocolDirection.Down, ProtocolDirection.Up };

        private ProtocolState m_state;

        [SetUp]
        public void SetUp()
        {
            var tuning = ProtocolTuning.Default;
            tuning.InputTimeoutSeconds = Timeout;
            m_state = new ProtocolState(new[] { Supply, Sentry }, tuning);
        }

        [Test]
        public void Push_FullSequence_MatchesSlotAndClears()
        {
            double now = 0.0;
            ProtocolMatch result = ProtocolMatch.Rejected;
            int slot = -1;
            foreach (var direction in Sentry)
            {
                result = m_state.Push(direction, now, out slot);
                now += 0.2;
            }

            Assert.That(result, Is.EqualTo(ProtocolMatch.Matched));
            Assert.That(slot, Is.EqualTo(1));
            Assert.That(m_state.Entered.Count, Is.Zero);
        }

        [Test]
        public void Push_Prefix_KeepsEntered()
        {
            Assert.That(m_state.Push(ProtocolDirection.Down, 0.0, out _), Is.EqualTo(ProtocolMatch.Pending));
            Assert.That(m_state.Push(ProtocolDirection.Down, 0.1, out _), Is.EqualTo(ProtocolMatch.Pending));
            Assert.That(m_state.Entered.Count, Is.EqualTo(2));
        }

        [Test]
        public void Push_WrongDirection_RejectsAndClears()
        {
            m_state.Push(ProtocolDirection.Down, 0.0, out _);
            Assert.That(m_state.Push(ProtocolDirection.Left, 0.1, out _), Is.EqualTo(ProtocolMatch.Rejected));
            Assert.That(m_state.Entered.Count, Is.Zero);
        }

        [Test]
        public void Push_AfterTimeout_StartsFresh()
        {
            m_state.Push(ProtocolDirection.Up, 0.0, out _);

            // Up then Right would be a Sentry prefix; Right alone starts nothing.
            Assert.That(m_state.Push(ProtocolDirection.Right, Timeout + 0.1, out _),
                Is.EqualTo(ProtocolMatch.Rejected));
        }

        [Test]
        public void Push_WithinTimeout_Continues()
        {
            m_state.Push(ProtocolDirection.Up, 0.0, out _);
            Assert.That(m_state.Push(ProtocolDirection.Right, Timeout - 0.1, out _),
                Is.EqualTo(ProtocolMatch.Pending));
        }

        [Test]
        public void Tick_ClearsEnteredAfterTimeout()
        {
            m_state.Push(ProtocolDirection.Up, 0.0, out _);
            m_state.Tick(Timeout - 0.1, 0.1f);
            Assert.That(m_state.Entered.Count, Is.EqualTo(1));

            m_state.Tick(Timeout + 0.1, 0.1f);
            Assert.That(m_state.Entered.Count, Is.Zero);
        }

        [Test]
        public void Cooldown_CountsDownAndClamps()
        {
            m_state.StartCooldown(0, 1f);
            Assert.That(m_state.CooldownRemaining(0), Is.EqualTo(1f));
            Assert.That(m_state.CooldownRemaining(1), Is.Zero);

            m_state.Tick(0.0, 0.4f);
            Assert.That(m_state.CooldownRemaining(0), Is.EqualTo(0.6f).Within(0.0001f));

            m_state.Tick(0.0, 5f);
            Assert.That(m_state.CooldownRemaining(0), Is.Zero);
        }

        [Test]
        public void Reset_ClearsEntered()
        {
            m_state.Push(ProtocolDirection.Up, 0.0, out _);
            m_state.Reset();
            Assert.That(m_state.Entered.Count, Is.Zero);
        }

        [Test]
        public void Push_BeyondMaxLength_StartsOver()
        {
            var nine = new ProtocolDirection[ProtocolRules.MaxSequenceLength + 1];
            var state = new ProtocolState(new[] { nine }, ProtocolTuning.Default);

            for (int i = 0; i < ProtocolRules.MaxSequenceLength; i++)
            {
                Assert.That(state.Push(ProtocolDirection.Up, 0.0, out _), Is.EqualTo(ProtocolMatch.Pending));
            }

            Assert.That(state.Entered.Count, Is.EqualTo(ProtocolRules.MaxSequenceLength));
            state.Push(ProtocolDirection.Up, 0.0, out _);
            Assert.That(state.Entered.Count, Is.EqualTo(1));
        }
    }
}
