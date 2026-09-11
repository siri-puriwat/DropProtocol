using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    public class MissionRulesTests
    {
        [Test]
        public void IsSquadWiped_NoPlayers_False()
        {
            Assert.That(MissionRules.IsSquadWiped(0, 0), Is.False);
        }

        [Test]
        public void IsSquadWiped_AllDowned_True()
        {
            Assert.That(MissionRules.IsSquadWiped(3, 0), Is.True);
        }

        [Test]
        public void IsSquadWiped_OneAlive_False()
        {
            Assert.That(MissionRules.IsSquadWiped(3, 1), Is.False);
        }

        [Test]
        public void IsTimedOut_ZeroLimit_False()
        {
            Assert.That(MissionRules.IsTimedOut(1000f, 0f), Is.False);
        }

        [Test]
        public void IsTimedOut_AtLimit_True()
        {
            Assert.That(MissionRules.IsTimedOut(60f, 60f), Is.True);
            Assert.That(MissionRules.IsTimedOut(59.9f, 60f), Is.False);
        }

        [Test]
        public void AllRelaysActivated_ZeroRelays_True()
        {
            Assert.That(MissionRules.AllRelaysActivated(0, 0), Is.True);
            Assert.That(MissionRules.AllRelaysActivated(2, 3), Is.False);
            Assert.That(MissionRules.AllRelaysActivated(3, 3), Is.True);
        }

        [Test]
        public void RelayStep_Held_Advances()
        {
            Assert.That(MissionRules.RelayStep(0f, true, 1f, 4f), Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void RelayStep_Released_Persists()
        {
            Assert.That(MissionRules.RelayStep(0.6f, false, 5f, 4f), Is.EqualTo(0.6f));
        }

        [Test]
        public void RelayStep_ZeroSeconds_Completes()
        {
            Assert.That(MissionRules.RelayStep(0f, true, 0.01f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void RelayStep_ClampsToOne()
        {
            Assert.That(MissionRules.RelayStep(0.9f, true, 10f, 4f), Is.EqualTo(1f));
            Assert.That(MissionRules.IsRelayActivated(1f), Is.True);
            Assert.That(MissionRules.IsRelayActivated(0.99f), Is.False);
        }

        [Test]
        public void ExtractionStep_NobodyInside_Pauses()
        {
            Assert.That(MissionRules.ExtractionStep(5f, false, 3f), Is.EqualTo(5f));
        }

        [Test]
        public void ExtractionStep_Inside_CountsDownAndClamps()
        {
            Assert.That(MissionRules.ExtractionStep(5f, true, 3f), Is.EqualTo(2f));
            Assert.That(MissionRules.ExtractionStep(1f, true, 3f), Is.EqualTo(0f));
            Assert.That(MissionRules.IsExtractionComplete(0f), Is.True);
        }

        [Test]
        public void DirectorShouldRun_ActiveAndExtractionOnly()
        {
            Assert.That(MissionRules.DirectorShouldRun(MissionPhase.Deployment), Is.False);
            Assert.That(MissionRules.DirectorShouldRun(MissionPhase.Active), Is.True);
            Assert.That(MissionRules.DirectorShouldRun(MissionPhase.Extraction), Is.True);
            Assert.That(MissionRules.DirectorShouldRun(MissionPhase.Complete), Is.False);
            Assert.That(MissionRules.DirectorShouldRun(MissionPhase.Failed), Is.False);
        }

        [Test]
        public void Intensity_Active_ScalesPerRelay()
        {
            Assert.That(MissionRules.Intensity(MissionPhase.Active, 2, 0.5f, 3f), Is.EqualTo(2f));
            Assert.That(MissionRules.Intensity(MissionPhase.Deployment, 2, 0.5f, 3f), Is.EqualTo(1f));
        }

        [Test]
        public void Intensity_Extraction_UsesExtractionIntensity()
        {
            Assert.That(MissionRules.Intensity(MissionPhase.Extraction, 3, 0.5f, 3f), Is.EqualTo(3f));
        }

        [Test]
        public void IsInside_FlatDistanceIgnoresHeight()
        {
            Assert.That(MissionRules.IsInside(new Vector3(2f, 10f, 0f), Vector3.zero, 2f), Is.True);
            Assert.That(MissionRules.IsInside(new Vector3(2.1f, 0f, 0f), Vector3.zero, 2f), Is.False);
        }
    }
}
