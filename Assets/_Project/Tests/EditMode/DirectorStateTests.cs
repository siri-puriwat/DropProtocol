using System.Collections.Generic;
using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class DirectorStateTests
    {
        private static readonly int[] Costs = { 1, 3, 6 };
        private static readonly int[] Weights = { 6, 3, 1 };

        private static DirectorTuning Tuning => new DirectorTuning
        {
            BaseBudgetPerSecond = 1f,
            BudgetPerExtraPlayer = 1f,
            MaxBudget = 12f,
            BasePopulationCap = 3,
            PopulationPerExtraPlayer = 1,
            ThinkSeconds = 1f,
            MinSpawnDistance = 10f,
            Seed = 1,
        };

        private static DirectorState Create(params double[] rolls)
        {
            var queue = new Queue<double>(rolls);
            return new DirectorState(Costs, Weights, Tuning, () => queue.Count > 0 ? queue.Dequeue() : 0.0);
        }

        [Test]
        public void Step_WithoutBudget_SpawnsNothing()
        {
            DirectorState state = Create(0.0);

            Assert.That(state.Step(1, 0, 0.5f), Is.EqualTo(-1));
            Assert.That(state.Budget, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Step_AffordableGrunt_SpawnsAndDeducts()
        {
            DirectorState state = Create(0.0);

            Assert.That(state.Step(1, 0, 1.5f), Is.EqualTo(0));
            Assert.That(state.Budget, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(state.DesiredIndex, Is.EqualTo(-1));
        }

        [Test]
        public void Step_SavesUpForBrute()
        {
            DirectorState state = Create(0.95);

            Assert.That(state.Step(1, 0, 1f), Is.EqualTo(-1));
            Assert.That(state.DesiredIndex, Is.EqualTo(2));
            Assert.That(state.Step(1, 0, 4f), Is.EqualTo(-1));
            Assert.That(state.Step(1, 0, 1f), Is.EqualTo(2));
            Assert.That(state.Budget, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Step_AtPopulationCap_SpawnsNothingButKeepsAccruing()
        {
            DirectorState state = Create(0.0);

            Assert.That(state.Step(1, 3, 5f), Is.EqualTo(-1));
            Assert.That(state.Budget, Is.EqualTo(5f).Within(0.001f));
            Assert.That(state.Step(1, 2, 0f), Is.EqualTo(0));
        }

        [Test]
        public void Step_MorePlayers_AccruesFaster()
        {
            DirectorState solo = Create(0.0);
            DirectorState squad = Create(0.0);

            Assert.That(solo.Step(1, 0, 0.5f), Is.EqualTo(-1));
            Assert.That(squad.Step(2, 0, 0.5f), Is.EqualTo(0));
        }

        [Test]
        public void Step_IntensityScalesIncome()
        {
            DirectorState state = Create(0.0);
            state.Intensity = 2f;

            Assert.That(state.Step(1, 0, 0.5f), Is.EqualTo(0));
        }

        [Test]
        public void Step_SameSeed_SameSequence()
        {
            var a = new DirectorState(Costs, Weights, Tuning, new System.Random(7).NextDouble);
            var b = new DirectorState(Costs, Weights, Tuning, new System.Random(7).NextDouble);

            for (int i = 0; i < 40; i++)
                Assert.That(a.Step(2, 0, 1f), Is.EqualTo(b.Step(2, 0, 1f)));
        }
    }
}
