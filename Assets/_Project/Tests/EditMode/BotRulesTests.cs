using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class BotRulesTests
    {
        private const float Stop = 3f;
        private const float Resume = 4.5f;
        private const float Engage = 25f;
        private const float Disengage = 30f;
        private const int Magazine = 30;

        [Test]
        public void ShouldMoveToLeader_InsideStopRadius_False()
        {
            Assert.That(BotRules.ShouldMoveToLeader(2f, true, Stop, Resume), Is.False);
        }

        [Test]
        public void ShouldMoveToLeader_BeyondResumeRadius_True()
        {
            Assert.That(BotRules.ShouldMoveToLeader(5f, false, Stop, Resume), Is.True);
        }

        [Test]
        public void ShouldMoveToLeader_BetweenRadiiWhileMoving_KeepsMoving()
        {
            Assert.That(BotRules.ShouldMoveToLeader(4f, true, Stop, Resume), Is.True);
        }

        [Test]
        public void ShouldMoveToLeader_BetweenRadiiWhileStopped_StaysStopped()
        {
            Assert.That(BotRules.ShouldMoveToLeader(4f, false, Stop, Resume), Is.False);
        }

        [Test]
        public void ShouldEngage_WithinEngageRange_True()
        {
            Assert.That(BotRules.ShouldEngage(20f, false, Engage, Disengage), Is.True);
        }

        [Test]
        public void ShouldEngage_BetweenRangesWhileEngaged_True()
        {
            Assert.That(BotRules.ShouldEngage(27f, true, Engage, Disengage), Is.True);
        }

        [Test]
        public void ShouldEngage_BetweenRangesWhileIdle_False()
        {
            Assert.That(BotRules.ShouldEngage(27f, false, Engage, Disengage), Is.False);
        }

        [Test]
        public void ShouldEngage_BeyondDisengageRange_False()
        {
            Assert.That(BotRules.ShouldEngage(31f, true, Engage, Disengage), Is.False);
        }

        [Test]
        public void ShouldReload_FullMagazine_False()
        {
            Assert.That(BotRules.ShouldReload(Magazine, Magazine, false, false), Is.False);
        }

        [Test]
        public void ShouldReload_PartialWithoutEnemy_True()
        {
            Assert.That(BotRules.ShouldReload(10, Magazine, false, false), Is.True);
        }

        [Test]
        public void ShouldReload_PartialWithEnemy_False()
        {
            Assert.That(BotRules.ShouldReload(10, Magazine, false, true), Is.False);
        }

        [Test]
        public void ShouldReload_EmptyWithEnemy_True()
        {
            Assert.That(BotRules.ShouldReload(0, Magazine, false, true), Is.True);
        }

        [Test]
        public void ShouldReload_AlreadyReloading_False()
        {
            Assert.That(BotRules.ShouldReload(0, Magazine, true, false), Is.False);
        }

        [Test]
        public void NextCornerIndex_SkipsCornersWithinReach()
        {
            Vector3[] corners = { Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(5f, 0f, 0f) };

            Assert.That(BotRules.NextCornerIndex(corners, corners.Length, 1, new Vector3(1f, 0f, 0.1f), 0.3f), Is.EqualTo(2));
        }

        [Test]
        public void NextCornerIndex_NeverPassesLastCorner()
        {
            Vector3[] corners = { Vector3.zero, new Vector3(1f, 0f, 0f) };

            Assert.That(BotRules.NextCornerIndex(corners, corners.Length, 1, new Vector3(1f, 0f, 0f), 0.3f), Is.EqualTo(1));
        }

        [Test]
        public void NextCornerIndex_RespectsCornerCountNotArrayLength()
        {
            Vector3[] corners = { Vector3.zero, new Vector3(0.1f, 0f, 0f), new Vector3(0.2f, 0f, 0f), new Vector3(9f, 0f, 9f) };

            Assert.That(BotRules.NextCornerIndex(corners, 2, 0, Vector3.zero, 0.3f), Is.EqualTo(1));
        }
    }
}
