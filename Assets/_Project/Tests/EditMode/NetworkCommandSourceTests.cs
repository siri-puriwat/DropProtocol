using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class NetworkCommandSourceTests
    {
        private double m_now;
        private NetworkCommandSource m_source;

        [SetUp]
        public void SetUp()
        {
            m_now = 100.0;
            m_source = new NetworkCommandSource(() => m_now, staleSeconds: 0.5);
        }

        [Test]
        public void BeforeAnyCommand_ReturnsNone()
        {
            PlayerCommand command = m_source.GetCommand();

            Assert.That(command.HasMove, Is.False);
            Assert.That(command.HasAim, Is.False);
        }

        [Test]
        public void Push_LatestCommandWins()
        {
            m_source.Push(new PlayerCommand { Move = new Vector2(1f, 0f) });
            m_source.Push(new PlayerCommand { Move = new Vector2(0f, 1f), Fire = true });

            PlayerCommand command = m_source.GetCommand();

            Assert.That(command.Move, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(command.Fire, Is.True);
        }

        [Test]
        public void GetCommand_WithinStaleWindow_ReturnsLatest()
        {
            m_source.Push(new PlayerCommand { Move = new Vector2(1f, 0f) });
            m_now += 0.49;

            Assert.That(m_source.GetCommand().HasMove, Is.True);
        }

        [Test]
        public void GetCommand_PastStaleWindow_ReturnsNone()
        {
            m_source.Push(new PlayerCommand { Move = new Vector2(1f, 0f), Fire = true });
            m_now += 0.51;

            PlayerCommand command = m_source.GetCommand();

            Assert.That(command.HasMove, Is.False);
            Assert.That(command.Fire, Is.False);
        }

        [Test]
        public void Push_AfterGoingStale_Revives()
        {
            m_source.Push(new PlayerCommand { Move = new Vector2(1f, 0f) });
            m_now += 5.0;
            m_source.Push(new PlayerCommand { Move = new Vector2(-1f, 0f) });

            Assert.That(m_source.GetCommand().Move, Is.EqualTo(new Vector2(-1f, 0f)));
        }
    }
}
