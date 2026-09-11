using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class MissionStateTests
    {
        private static MissionTuning Tuning(float timeLimit = 0f) => new MissionTuning
        {
            DeploySeconds = 2f,
            TimeLimitSeconds = timeLimit,
            ExtractionSeconds = 4f,
            IntensityPerRelay = 0.5f,
            ExtractionIntensity = 3f,
        };

        private static MissionInputs Inputs(int relays = 2, int done = 0, int players = 2, int alive = 2, int inside = 0)
        {
            return new MissionInputs
            {
                RelayCount = relays,
                RelaysActivated = done,
                PlayerCount = players,
                AliveCount = alive,
                AliveInsideZone = inside,
            };
        }

        private static MissionState Active(float timeLimit = 0f)
        {
            var state = new MissionState(Tuning(timeLimit));
            state.Step(Inputs(), 2f);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Active));
            return state;
        }

        private static MissionState Extracting(float timeLimit = 0f)
        {
            MissionState state = Active(timeLimit);
            state.Step(Inputs(done: 2), 0.1f);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Extraction));
            return state;
        }

        [Test]
        public void NewState_StartsInDeployment()
        {
            var state = new MissionState(Tuning());

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Deployment));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.None));
            Assert.That(state.DeployRemaining, Is.EqualTo(2f));
            Assert.That(state.IsOver, Is.False);
        }

        [Test]
        public void Step_DeployCountdownExpires_Active()
        {
            var state = new MissionState(Tuning());

            Assert.That(state.Step(Inputs(), 1f), Is.False);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Deployment));
            Assert.That(state.Step(Inputs(), 1f), Is.True);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Active));
            Assert.That(state.DeployRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void Step_DeploymentWithoutPlayers_StillReachesActive()
        {
            var state = new MissionState(Tuning());

            state.Step(Inputs(players: 0, alive: 0), 3f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Active));
        }

        [Test]
        public void Step_DeploymentSquadWipe_Failed()
        {
            var state = new MissionState(Tuning());

            Assert.That(state.Step(Inputs(alive: 0), 0.1f), Is.True);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Failed));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.SquadWiped));
        }

        [Test]
        public void Step_ActiveAllRelays_Extraction()
        {
            MissionState state = Active();

            Assert.That(state.Step(Inputs(done: 1), 1f), Is.False);
            Assert.That(state.Step(Inputs(done: 2), 1f), Is.True);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Extraction));
            Assert.That(state.ExtractionRemaining, Is.EqualTo(4f));
        }

        [Test]
        public void Step_ActiveZeroRelays_ExtractionImmediately()
        {
            MissionState state = Active();

            state.Step(Inputs(relays: 0), 0.1f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Extraction));
        }

        [Test]
        public void Step_ActiveSquadWipe_FailedSquadWiped()
        {
            MissionState state = Active();

            state.Step(Inputs(alive: 0), 0.1f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Failed));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.SquadWiped));
            Assert.That(state.IsOver, Is.True);
        }

        [Test]
        public void Step_ActiveTimeLimit_FailedTimedOut()
        {
            MissionState state = Active(10f);

            state.Step(Inputs(), 9.5f);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Active));
            state.Step(Inputs(), 0.5f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Failed));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.TimedOut));
        }

        [Test]
        public void Step_ZeroTimeLimit_NeverTimesOut()
        {
            MissionState state = Active();

            state.Step(Inputs(), 100000f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Active));
        }

        [Test]
        public void Step_ElapsedDoesNotAccrueInDeployment()
        {
            var state = new MissionState(Tuning());

            state.Step(Inputs(), 1f);
            Assert.That(state.Elapsed, Is.EqualTo(0f));
            state.Step(Inputs(), 1f);
            state.Step(Inputs(), 0.5f);

            Assert.That(state.Elapsed, Is.EqualTo(0.5f));
        }

        [Test]
        public void Step_ExtractionNobodyInside_Pauses()
        {
            MissionState state = Extracting();

            state.Step(Inputs(done: 2, inside: 0), 10f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Extraction));
            Assert.That(state.ExtractionRemaining, Is.EqualTo(4f));
        }

        [Test]
        public void Step_ExtractionResumesWithoutReset()
        {
            MissionState state = Extracting();

            state.Step(Inputs(done: 2, inside: 1), 1f);
            state.Step(Inputs(done: 2, inside: 0), 5f);
            Assert.That(state.ExtractionRemaining, Is.EqualTo(3f));
            state.Step(Inputs(done: 2, inside: 1), 3f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Complete));
        }

        [Test]
        public void Step_ExtractionCompletes_ExtractedCountIsAliveInside()
        {
            MissionState state = Extracting();

            Assert.That(state.Step(Inputs(done: 2, inside: 2), 4f), Is.True);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Complete));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.Extracted));
            Assert.That(state.ExtractedCount, Is.EqualTo(2));
            Assert.That(state.IsOver, Is.True);
        }

        [Test]
        public void Step_ExtractionSquadWipe_Failed()
        {
            MissionState state = Extracting();

            state.Step(Inputs(done: 2, alive: 0), 0.1f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Failed));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.SquadWiped));
        }

        [Test]
        public void Step_TimeLimitBeatsRelayCompletion()
        {
            MissionState state = Active(1f);

            state.Step(Inputs(done: 2), 1f);

            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Failed));
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.TimedOut));
        }

        [Test]
        public void Step_AfterComplete_IsNoOp()
        {
            MissionState state = Extracting();
            state.Step(Inputs(done: 2, inside: 1), 4f);

            Assert.That(state.Step(Inputs(done: 2, alive: 0), 1f), Is.False);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Complete));
            Assert.That(state.ExtractedCount, Is.EqualTo(1));
        }

        [Test]
        public void Step_AfterFailed_IsNoOp()
        {
            MissionState state = Active();
            state.Step(Inputs(alive: 0), 0.1f);
            float elapsed = state.Elapsed;

            Assert.That(state.Step(Inputs(done: 2, inside: 2), 10f), Is.False);
            Assert.That(state.Phase, Is.EqualTo(MissionPhase.Failed));
            Assert.That(state.Elapsed, Is.EqualTo(elapsed));
        }

        [Test]
        public void Step_ReturnsTrueOnlyOnTransition()
        {
            var state = new MissionState(Tuning());

            Assert.That(state.Step(Inputs(), 0.5f), Is.False);
            Assert.That(state.Step(Inputs(), 2f), Is.True);
            Assert.That(state.Step(Inputs(), 0.5f), Is.False);
        }
    }
}
