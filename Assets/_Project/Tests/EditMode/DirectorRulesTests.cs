using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class DirectorRulesTests
    {
        private static readonly int[] Weights = { 6, 3, 1 };

        [Test]
        public void BudgetPerSecond_ScalesWithExtraPlayers()
        {
            Assert.That(DirectorRules.BudgetPerSecond(1, 0.5f, 0.25f, 1f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(DirectorRules.BudgetPerSecond(4, 0.5f, 0.25f, 1f), Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(DirectorRules.BudgetPerSecond(0, 0.5f, 0.25f, 1f), Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void BudgetPerSecond_ScalesWithIntensity()
        {
            Assert.That(DirectorRules.BudgetPerSecond(1, 0.5f, 0.25f, 2f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Accrue_ClampsAtMax()
        {
            Assert.That(DirectorRules.Accrue(11f, 2f, 1f, 12f), Is.EqualTo(12f).Within(0.001f));
            Assert.That(DirectorRules.Accrue(1f, 2f, 0.5f, 12f), Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void PopulationCap_ScalesWithPlayers()
        {
            Assert.That(DirectorRules.PopulationCap(1, 6, 2), Is.EqualTo(6));
            Assert.That(DirectorRules.PopulationCap(4, 6, 2), Is.EqualTo(12));
        }

        [Test]
        public void PickWeighted_LowRollPicksFirst()
        {
            Assert.That(DirectorRules.PickWeighted(Weights, 0.0), Is.EqualTo(0));
            Assert.That(DirectorRules.PickWeighted(Weights, 0.59), Is.EqualTo(0));
        }

        [Test]
        public void PickWeighted_MidRollPicksSecond()
        {
            Assert.That(DirectorRules.PickWeighted(Weights, 0.6), Is.EqualTo(1));
            Assert.That(DirectorRules.PickWeighted(Weights, 0.89), Is.EqualTo(1));
        }

        [Test]
        public void PickWeighted_HighRollPicksLast()
        {
            Assert.That(DirectorRules.PickWeighted(Weights, 0.9), Is.EqualTo(2));
            Assert.That(DirectorRules.PickWeighted(Weights, 0.999), Is.EqualTo(2));
        }

        [Test]
        public void PickWeighted_SkipsZeroWeights()
        {
            Assert.That(DirectorRules.PickWeighted(new[] { 0, 5, 0 }, 0.5), Is.EqualTo(1));
        }

        [Test]
        public void PickWeighted_AllZero_ReturnsMinusOne()
        {
            Assert.That(DirectorRules.PickWeighted(new[] { 0, 0 }, 0.5), Is.EqualTo(-1));
        }

        [Test]
        public void ChooseSpawnPoint_SkipsPointsNearPlayers()
        {
            Vector3[] points = { new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, 20f) };
            Vector3[] players = { Vector3.zero };

            Assert.That(DirectorRules.ChooseSpawnPoint(points, players, 10f, 0), Is.EqualTo(1));
        }

        [Test]
        public void ChooseSpawnPoint_RoundRobinsFromCursor()
        {
            Vector3[] points = { new Vector3(0f, 0f, 20f), new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, -20f) };
            Vector3[] players = { Vector3.zero };

            Assert.That(DirectorRules.ChooseSpawnPoint(points, players, 10f, 1), Is.EqualTo(1));
            Assert.That(DirectorRules.ChooseSpawnPoint(points, players, 10f, 3), Is.EqualTo(0));
        }

        [Test]
        public void ChooseSpawnPoint_AllTooClose_PicksFarthest()
        {
            Vector3[] points = { new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 6f), new Vector3(0f, 0f, 4f) };
            Vector3[] players = { Vector3.zero };

            Assert.That(DirectorRules.ChooseSpawnPoint(points, players, 10f, 0), Is.EqualTo(1));
        }

        [Test]
        public void ChooseSpawnPoint_NoPlayers_TakesCursor()
        {
            Vector3[] points = { Vector3.zero, Vector3.one };

            Assert.That(DirectorRules.ChooseSpawnPoint(points, new Vector3[0], 10f, 1), Is.EqualTo(1));
        }

        [Test]
        public void ChooseSpawnPoint_NoPoints_ReturnsMinusOne()
        {
            Assert.That(DirectorRules.ChooseSpawnPoint(new Vector3[0], new Vector3[0], 10f, 0), Is.EqualTo(-1));
        }
    }
}
