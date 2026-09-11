using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Mission objectives on a single in-process host: relays charge from a held Interact, the mission
    /// moves through deployment, relays, extraction and results, fails on a wipe or the clock, and bots
    /// help with whatever objective the squad is working on.
    /// </summary>
    public class MissionHostTests
    {
        private const ushort TestPort = 7795;
        private const ulong ClientA = 42;
        private const ulong ClientB = 43;
        private const int PlayerHealth = 100;
        private const int GruntHealth = 1000;
        private const float RelayRadius = 3f;
        private const float ZoneRadius = 4f;

        private HostTestHarness m_harness;
        private GameObject m_playerTemplate;
        private GameObject m_gruntTemplate;
        private BotTuning m_botTuning;
        private EnemyDirector m_director;
        private uint m_nextRelayHash;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_harness = new HostTestHarness();
            m_harness.CreateGround(80f);
            m_harness.BuildNavMesh();

            WeaponDefinition rifle = m_harness.TrackAsset(WeaponDefinition.Create(20, 10f, 30, 0.5f, 0f, 60f));
            // A tanky grunt that barely moves and cannot hurt anyone keeps the geometry of each test fixed.
            EnemyDefinition grunt = m_harness.TrackAsset(EnemyDefinition.Create(GruntHealth, 1, 0.1f, 0.4f, 0, 1.6f, 1.6f, 0f, 1f, 0.25f));
            m_playerTemplate = m_harness.CreatePlayerTemplate(rifle, 0xD20B000Eu);
            m_gruntTemplate = m_harness.CreateEnemyTemplate(grunt, null, 0xD20B000Fu);
            m_nextRelayHash = 0xD20B0010u;

            m_botTuning = BotTuning.Default;
            m_botTuning.ThinkSeconds = 0.1f;

            m_harness.StartHost(TestPort);
            yield return null;

            m_director = SpawnDirector();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return m_harness.TearDown();
        }

        [UnityTest]
        public IEnumerator Relay_HeldInteractInsideRadius_Activates()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 1f);
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1f, 0.1f, 0f));

            yield return HostTestHarness.Hold(player, Interact(), 1.5f);

            Assert.That(relay.IsActivated.Value, Is.True);
            Assert.That(relay.Progress.Value, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Relay_HeldInteractOutsideRadius_DoesNotFill()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 1f);
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(5f, 0.1f, 0f));

            yield return HostTestHarness.Hold(player, Interact(), 1f);

            Assert.That(relay.Progress.Value, Is.Zero);
            Assert.That(relay.IsActivated.Value, Is.False);
        }

        [UnityTest]
        public IEnumerator Relay_ReleasedInteract_KeepsProgress()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 2f);
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1f, 0.1f, 0f));

            yield return HostTestHarness.Hold(player, Interact(), 0.5f);
            // An explicit release: the network source keeps repeating the last command for its stale window.
            yield return HostTestHarness.Hold(player, PlayerCommand.None, 0.1f);
            float held = relay.Progress.Value;
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(held, Is.InRange(0.15f, 0.45f));
            Assert.That(relay.Progress.Value, Is.EqualTo(held));
        }

        [UnityTest]
        public IEnumerator Relay_DownedPlayer_DoesNotFill()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 1f);
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1f, 0.1f, 0f));
            yield return null;
            player.Health.ApplyDamage(PlayerHealth);

            yield return HostTestHarness.Hold(player, Interact(), 1f);

            Assert.That(relay.Progress.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Relay_TwoHolders_SameRate()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 2f);
            NetworkPlayer a = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1f, 0.1f, 0f));
            NetworkPlayer b = m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(-1f, 0.1f, 0f));

            float end = Time.time + 0.5f;
            while (Time.time < end)
            {
                a.SubmitCommand(Interact());
                b.SubmitCommand(Interact());
                yield return null;
            }

            Assert.That(relay.Holders, Is.EqualTo(2));
            Assert.That(relay.Progress.Value, Is.InRange(0.15f, 0.4f));
        }

        [UnityTest]
        public IEnumerator Relay_ReviveBeatsRelay()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 100f);
            NetworkPlayer reviver = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0.5f, 0.1f, 0f));
            NetworkPlayer downed = m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(-1f, 0.1f, 0f));
            yield return null;
            downed.Health.ApplyDamage(PlayerHealth);

            yield return HostTestHarness.Hold(reviver, Interact(), 2f);

            Assert.That(downed.Health.IsDowned, Is.True);
            Assert.That(reviver.GetComponent<ReviveController>().Progress.Value, Is.GreaterThan(0f));
            Assert.That(relay.Progress.Value, Is.Zero, "the relay charged while a revive was in progress");

            yield return HostTestHarness.Hold(reviver, Interact(), 1.5f);

            Assert.That(downed.Health.IsDowned, Is.False);
        }

        [UnityTest]
        public IEnumerator Relay_DuringDeployment_DoesNotFill()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 1f);
            SpawnMission(new[] { relay }, null, Tuning(deploy: 5f));
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1f, 0.1f, 0f));

            yield return HostTestHarness.Hold(player, Interact(), 1f);

            Assert.That(MissionDirector.Instance.Phase.Value, Is.EqualTo(MissionPhase.Deployment));
            Assert.That(relay.Progress.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Mission_DeployCountdown_ThenActive()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(20f, 0.1f, 0f));
            MissionDirector mission = SpawnMission(new[] { SpawnRelay(Vector3.zero, 1f) }, null, Tuning());
            yield return null;

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Deployment));
            Assert.That(m_director.Running, Is.False);

            yield return HostTestHarness.Wait(0.6f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Active));
            Assert.That(m_director.Running, Is.True);
            Assert.That(m_director.Intensity, Is.EqualTo(1f));
            Assert.That(mission.RelayCount.Value, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Mission_AllRelaysActivated_Extraction()
        {
            CommRelay first = SpawnRelay(Vector3.zero, 0.5f);
            CommRelay second = SpawnRelay(new Vector3(3f, 0f, 0f), 0.5f);
            MissionDirector mission = SpawnMission(new[] { first, second }, CreateZone(new Vector3(30f, 0f, 30f)), Tuning());
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1.5f, 0.1f, 0f));
            yield return HostTestHarness.Wait(0.5f);

            yield return HostTestHarness.Hold(player, Interact(), 1f);

            Assert.That(mission.RelaysActivated.Value, Is.EqualTo(2));
            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Extraction));
            Assert.That(m_director.Running, Is.True);
            Assert.That(m_director.Intensity, Is.EqualTo(mission.Tuning.ExtractionIntensity));
        }

        [UnityTest]
        public IEnumerator Mission_RelayActivation_RaisesIntensity()
        {
            CommRelay near = SpawnRelay(Vector3.zero, 0.5f);
            CommRelay far = SpawnRelay(new Vector3(20f, 0f, 0f), 0.5f);
            MissionDirector mission = SpawnMission(new[] { near, far }, null, Tuning());
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(1f, 0.1f, 0f));
            yield return HostTestHarness.Wait(0.5f);

            yield return HostTestHarness.Hold(player, Interact(), 1f);

            Assert.That(mission.RelaysActivated.Value, Is.EqualTo(1));
            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Active));
            Assert.That(m_director.Intensity, Is.EqualTo(1f + mission.Tuning.IntensityPerRelay));
        }

        [UnityTest]
        public IEnumerator Mission_AlivePlayerInZone_Completes()
        {
            MissionDirector mission = SpawnMission(new CommRelay[0], CreateZone(Vector3.zero), Tuning());
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));

            yield return HostTestHarness.Wait(1.5f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Complete));
            Assert.That(mission.Outcome.Value, Is.EqualTo(MissionOutcome.Extracted));
            Assert.That(mission.ExtractedCount.Value, Is.EqualTo(1));
            Assert.That(m_director.Running, Is.False);
        }

        [UnityTest]
        public IEnumerator Mission_DownedPlayerInZone_DoesNotCountDown()
        {
            MissionDirector mission = SpawnMission(new CommRelay[0], CreateZone(Vector3.zero), Tuning());
            NetworkPlayer inside = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(20f, 0.1f, 0f));
            yield return null;
            inside.Health.ApplyDamage(PlayerHealth);

            yield return HostTestHarness.Wait(1.5f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Extraction));
            Assert.That(mission.ExtractionRemaining.Value, Is.EqualTo(mission.Tuning.ExtractionSeconds));
        }

        [UnityTest]
        public IEnumerator Mission_ExtractionPausesAndResumes()
        {
            MissionDirector mission = SpawnMission(new CommRelay[0], CreateZone(Vector3.zero), Tuning(extraction: 1.5f));
            NetworkPlayer inside = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(20f, 0.1f, 0f));
            yield return HostTestHarness.Wait(1f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Extraction));
            inside.Health.ApplyDamage(PlayerHealth);
            yield return null;
            float paused = mission.ExtractionRemaining.Value;
            yield return HostTestHarness.Wait(1f);

            Assert.That(paused, Is.InRange(0.3f, 1.4f));
            Assert.That(mission.ExtractionRemaining.Value, Is.EqualTo(paused));

            inside.Health.Revive(PlayerHealth);
            yield return HostTestHarness.Wait(1.5f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Complete));
            Assert.That(mission.ExtractedCount.Value, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Mission_AllPlayersDowned_Failed()
        {
            MissionDirector mission = SpawnMission(new[] { SpawnRelay(Vector3.zero, 1f) }, null, Tuning());
            NetworkPlayer a = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(10f, 0.1f, 0f));
            NetworkPlayer b = m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(12f, 0.1f, 0f));
            yield return HostTestHarness.Wait(0.5f);

            a.Health.ApplyDamage(PlayerHealth);
            yield return null;
            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Active));
            b.Health.ApplyDamage(PlayerHealth);
            yield return null;
            yield return null;

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Failed));
            Assert.That(mission.Outcome.Value, Is.EqualTo(MissionOutcome.SquadWiped));
            Assert.That(m_director.Running, Is.False);
        }

        [UnityTest]
        public IEnumerator Mission_TimeLimit_Failed()
        {
            MissionDirector mission = SpawnMission(new[] { SpawnRelay(Vector3.zero, 1f) }, null, Tuning(timeLimit: 0.5f));
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(10f, 0.1f, 0f));

            yield return HostTestHarness.Wait(1.2f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Failed));
            Assert.That(mission.Outcome.Value, Is.EqualTo(MissionOutcome.TimedOut));
            Assert.That(mission.TimeLimit.Value, Is.EqualTo(0.5f));
        }

        [UnityTest]
        public IEnumerator Mission_EnemyKilled_IncrementsKillCount()
        {
            MissionDirector mission = SpawnMission(new[] { SpawnRelay(Vector3.zero, 1f) }, null, Tuning());
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(10f, 0.1f, 0f));
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(20f, 0f, 20f));
            yield return null;

            grunt.GetComponent<Health>().ApplyDamage(GruntHealth);
            yield return HostTestHarness.Wait(0.2f);

            Assert.That(mission.EnemiesKilled.Value, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Mission_AfterComplete_StaysComplete()
        {
            MissionDirector mission = SpawnMission(new CommRelay[0], CreateZone(Vector3.zero), Tuning());
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return HostTestHarness.Wait(1.5f);
            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Complete));

            player.Health.ApplyDamage(PlayerHealth);
            yield return HostTestHarness.Wait(0.3f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Complete));
            Assert.That(mission.Outcome.Value, Is.EqualTo(MissionOutcome.Extracted));
        }

        [UnityTest]
        public IEnumerator Bot_WalksToRelayNearLeaderAndActivatesIt()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 1f);
            SpawnMission(new[] { relay }, null, Tuning());
            // The leader stands beside the relay, off the straight line the bot walks along.
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 6f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(15f, 0.1f, 0f), m_botTuning);

            yield return HostTestHarness.Wait(7f);

            // The human never holds Interact, so only the bot can have charged it.
            Assert.That(relay.IsActivated.Value, Is.True, "bot did not activate the relay");
            // With the relay done there is no objective left, so the bot falls back to its leader.
            Assert.That(StateOf(bot), Is.EqualTo(BotState.Follow));
        }

        [UnityTest]
        public IEnumerator Bot_IgnoresRelayFarFromLeader()
        {
            CommRelay relay = SpawnRelay(Vector3.zero, 1f);
            SpawnMission(new[] { relay }, null, Tuning());
            NetworkPlayer human = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(30f, 0.1f, 0f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(28f, 0.1f, 0f), m_botTuning);

            yield return HostTestHarness.Wait(3f);

            Assert.That(relay.Progress.Value, Is.Zero);
            Assert.That(StateOf(bot), Is.EqualTo(BotState.Follow));
            Assert.That(EnemyRules.FlatDistance(bot.transform.position, human.transform.position), Is.LessThan(m_botTuning.FollowResumeRadius));
        }

        [UnityTest]
        public IEnumerator Bot_WalksIntoZoneDuringExtraction()
        {
            MissionDirector mission = SpawnMission(new CommRelay[0], CreateZone(Vector3.zero), Tuning(extraction: 1.5f));
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(12f, 0.1f, 0f), m_botTuning);

            yield return HostTestHarness.Wait(6f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Complete));
            Assert.That(mission.ExtractedCount.Value, Is.EqualTo(2), "bot did not reach the extraction zone");
            Assert.That(EnemyRules.FlatDistance(bot.transform.position, Vector3.zero), Is.LessThan(ZoneRadius));
        }

        private static PlayerCommand Interact()
        {
            return new PlayerCommand { Interact = true };
        }

        private static MissionTuning Tuning(float deploy = 0.3f, float timeLimit = 0f, float extraction = 0.5f)
        {
            MissionTuning tuning = MissionTuning.Default;
            tuning.DeploySeconds = deploy;
            tuning.TimeLimitSeconds = timeLimit;
            tuning.ExtractionSeconds = extraction;
            return tuning;
        }

        private static BotState StateOf(NetworkPlayer bot)
        {
            return ((BotCommandSource)bot.Character.CommandSource).State;
        }

        private CommRelay SpawnRelay(Vector3 position, float activateSeconds)
        {
            var relayObject = m_harness.Track(new GameObject("TestRelay"));
            relayObject.SetActive(false);
            relayObject.transform.position = position;
            var networkObject = relayObject.AddComponent<NetworkObject>();
            var relay = relayObject.AddComponent<CommRelay>();
            relay.Configure(RelayRadius, activateSeconds);
            HostTestHarness.AssignGlobalObjectIdHash(networkObject, m_nextRelayHash++);
            relayObject.SetActive(true);
            networkObject.Spawn();
            return relay;
        }

        private ExtractionZone CreateZone(Vector3 position)
        {
            var zoneObject = m_harness.Track(new GameObject("TestZone"));
            zoneObject.transform.position = position;
            var zone = zoneObject.AddComponent<ExtractionZone>();
            zone.Configure(ZoneRadius);
            return zone;
        }

        private MissionDirector SpawnMission(CommRelay[] relays, ExtractionZone zone, MissionTuning tuning)
        {
            var missionObject = m_harness.Track(new GameObject("TestMission"));
            missionObject.SetActive(false);
            var networkObject = missionObject.AddComponent<NetworkObject>();
            var mission = missionObject.AddComponent<MissionDirector>();
            mission.Configure(relays, zone, m_director, tuning);
            HostTestHarness.AssignGlobalObjectIdHash(networkObject, 0xD20B0013u);
            missionObject.SetActive(true);
            networkObject.Spawn();
            return mission;
        }

        // Budget stays at zero: the tests read Running and Intensity, they never want spawns.
        private EnemyDirector SpawnDirector()
        {
            var spawnRoot = m_harness.Track(new GameObject("EnemySpawns"));
            var point = new GameObject("EnemySpawn_0");
            point.transform.SetParent(spawnRoot.transform, false);
            point.transform.position = new Vector3(35f, 0f, 35f);

            var directorObject = m_harness.Track(new GameObject("TestDirector"));
            directorObject.SetActive(false);
            var networkObject = directorObject.AddComponent<NetworkObject>();
            var director = directorObject.AddComponent<EnemyDirector>();
            DirectorTuning tuning = DirectorTuning.Default;
            tuning.BaseBudgetPerSecond = 0f;
            tuning.BudgetPerExtraPlayer = 0f;
            director.Configure(new[] { m_gruntTemplate.GetComponent<NetworkObject>() }, new[] { 1 }, new[] { point.transform }, tuning, () => 0.0);
            HostTestHarness.AssignGlobalObjectIdHash(networkObject, 0xD20B0014u);
            directorObject.SetActive(true);
            networkObject.Spawn();
            return director;
        }
    }
}
