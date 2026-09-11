using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Support protocols on a single in-process host: the host matches the entered sequence, gates the
    /// call on cooldown, downed state and mission phase, and each payload does its job where it lands.
    /// Directions go straight into the server entry point, bypassing the RPC.
    /// </summary>
    public class ProtocolHostTests
    {
        private const ushort TestPort = 7796;
        private const ulong ClientA = 42;
        private const ulong ClientB = 43;
        private const int PlayerHealth = 100;
        private const int GruntHealth = 40;
        private const float Cooldown = 1f;
        private const float InputTimeout = 0.3f;
        private const float ThrowDistance = 5f;
        private const int HealAmount = 30;
        private const float SupplyRadius = 6f;
        private const int SentryDamage = 20;
        private const float SentryRange = 8f;
        private const float StrikeWarning = 0.5f;
        private const float StrikeRadius = 4f;
        private const int StrikeDamage = 30;

        private static readonly ProtocolDirection[] SupplySequence =
            { ProtocolDirection.Down, ProtocolDirection.Down, ProtocolDirection.Up, ProtocolDirection.Right };

        private static readonly ProtocolDirection[] SentrySequence =
            { ProtocolDirection.Up, ProtocolDirection.Right, ProtocolDirection.Down, ProtocolDirection.Up };

        private static readonly ProtocolDirection[] StrikeSequence =
        {
            ProtocolDirection.Up, ProtocolDirection.Down, ProtocolDirection.Right, ProtocolDirection.Left,
            ProtocolDirection.Up
        };

        private HostTestHarness m_harness;
        private GameObject m_playerTemplate;
        private GameObject m_gruntTemplate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_harness = new HostTestHarness();
            m_harness.CreateGround(80f);
            m_harness.BuildNavMesh();

            WeaponDefinition rifle = m_harness.TrackAsset(WeaponDefinition.Create(20, 10f, 30, 0.5f, 0f, 60f));
            // Slow and harmless so the geometry of each test stays put.
            EnemyDefinition grunt = m_harness.TrackAsset(EnemyDefinition.Create(GruntHealth, 1, 0.1f, 0.4f, 0, 1.6f, 1.6f, 0f, 1f, 0.25f));
            m_gruntTemplate = m_harness.CreateEnemyTemplate(grunt, null, 0xD20B0016u);

            GameObject supply = m_harness.CreateNetworkTemplate("TestSupplyPod", 0xD20B0017u);
            supply.AddComponent<SupplyPod>().Configure(0.2f, SupplyRadius, HealAmount, 10f);
            GameObject sentry = m_harness.CreateNetworkTemplate("TestSentryTurret", 0xD20B0018u);
            sentry.AddComponent<SentryTurret>().Configure(0.1f, SentryRange, SentryDamage, 0.05f, 40, 10f);
            GameObject strike = m_harness.CreateNetworkTemplate("TestStrikeBeacon", 0xD20B0019u);
            strike.AddComponent<StrikeBeacon>().Configure(StrikeWarning, StrikeRadius, StrikeDamage, 1f);

            var loadout = new[]
            {
                m_harness.TrackAsset(ProtocolDefinition.Create("Supply", SupplySequence, Cooldown, supply.GetComponent<NetworkObject>())),
                m_harness.TrackAsset(ProtocolDefinition.Create("Sentry", SentrySequence, Cooldown, sentry.GetComponent<NetworkObject>())),
                m_harness.TrackAsset(ProtocolDefinition.Create("Strike", StrikeSequence, Cooldown, strike.GetComponent<NetworkObject>())),
            };
            ProtocolTuning tuning = ProtocolTuning.Default;
            tuning.InputTimeoutSeconds = InputTimeout;
            tuning.ThrowDistance = ThrowDistance;
            m_playerTemplate = m_harness.CreatePlayerTemplate(rifle, 0xD20B0015u, loadout, tuning);

            m_harness.StartHost(TestPort);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return m_harness.TearDown();
        }

        [UnityTest]
        public IEnumerator Sequence_Matched_SpawnsPayloadInFrontOfCaller()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            yield return null;

            yield return Enter(controller, SupplySequence);

            SupplyPod pod = FindPayload<SupplyPod>();
            Assert.That(pod, Is.Not.Null);
            Assert.That(pod.transform.position.z, Is.EqualTo(ThrowDistance).Within(0.05f));
            Assert.That(pod.transform.position.x, Is.EqualTo(0f).Within(0.05f));
            Assert.That(controller.Entered.Value, Is.Zero);
            Assert.That(controller.Cooldowns.Value[0], Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator Sequence_WrongDirection_ClearsEnteredWithoutSpawning()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            yield return null;

            controller.SubmitDirection(ProtocolDirection.Down);
            Assert.That(controller.Entered.Value & 0xF, Is.EqualTo(1));

            controller.SubmitDirection(ProtocolDirection.Left);
            yield return null;

            Assert.That(controller.Entered.Value, Is.Zero);
            Assert.That(FindPayload<SupplyPod>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator Sequence_Timeout_ClearsEntered()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            yield return null;

            controller.SubmitDirection(ProtocolDirection.Down);
            controller.SubmitDirection(ProtocolDirection.Down);
            yield return HostTestHarness.Wait(InputTimeout + 0.2f);

            Assert.That(controller.Entered.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Sequence_OnCooldown_DoesNotSpawnAgainUntilReady()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            yield return null;

            yield return Enter(controller, StrikeSequence);
            yield return Enter(controller, StrikeSequence);
            Assert.That(Object.FindObjectsByType<StrikeBeacon>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            yield return HostTestHarness.Wait(Cooldown + 0.2f);
            yield return Enter(controller, StrikeSequence);

            Assert.That(Object.FindObjectsByType<StrikeBeacon>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Sequence_DownedCaller_DoesNotSpawn()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            yield return null;
            controller.GetComponent<Health>().ApplyDamage(PlayerHealth);

            yield return Enter(controller, SupplySequence);

            Assert.That(FindPayload<SupplyPod>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator Sequence_DuringDeployment_BlockedThenAllowed()
        {
            MissionTuning tuning = MissionTuning.Default;
            tuning.DeploySeconds = 1f;
            MissionDirector mission = SpawnMission(tuning);
            ProtocolController controller = SpawnCaller(Vector3.zero);
            yield return null;

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Deployment));
            yield return Enter(controller, SupplySequence);
            Assert.That(FindPayload<SupplyPod>(), Is.Null);

            yield return HostTestHarness.Wait(tuning.DeploySeconds + 0.2f);
            Assert.That(mission.Phase.Value, Is.Not.EqualTo(MissionPhase.Deployment));
            yield return Enter(controller, SupplySequence);

            Assert.That(FindPayload<SupplyPod>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Supply_HealsAndRefillsStandingPlayerOnce_SkipsDowned()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            NetworkPlayer caller = controller.GetComponent<NetworkPlayer>();
            NetworkPlayer downed = m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(1f, 0.1f, ThrowDistance));
            yield return null;
            caller.Health.ApplyDamage(50);
            downed.Health.ApplyDamage(PlayerHealth);
            yield return HostTestHarness.Hold(caller, new PlayerCommand { Fire = true }, 0.3f);
            // An explicit release: the network source keeps repeating the last command for its stale window.
            yield return HostTestHarness.Hold(caller, PlayerCommand.None, 0.1f);
            WeaponController weapon = caller.GetComponent<WeaponController>();
            Assert.That(weapon.Ammo.Value, Is.LessThan(30));

            yield return Enter(controller, SupplySequence);
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(caller.Health.Current.Value, Is.EqualTo(50 + HealAmount));
            Assert.That(weapon.Ammo.Value, Is.EqualTo(30));
            Assert.That(downed.Health.IsDowned, Is.True);

            caller.Health.ApplyDamage(20);
            yield return HostTestHarness.Wait(0.3f);

            Assert.That(caller.Health.Current.Value, Is.EqualTo(50 + HealAmount - 20), "a pod serves each player once");
        }

        [UnityTest]
        public IEnumerator Sentry_KillsGruntInRange_IgnoresGruntOutOfRange()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            EnemyCharacter near = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, ThrowDistance + 4f));
            EnemyCharacter far = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, ThrowDistance + 30f));
            yield return null;

            yield return Enter(controller, SentrySequence);
            SentryTurret sentry = FindPayload<SentryTurret>();
            Assert.That(sentry, Is.Not.Null);
            yield return HostTestHarness.Wait(1f);

            Assert.That(near.IsDead, Is.True);
            Assert.That(far.GetComponent<Health>().Current.Value, Is.EqualTo(GruntHealth));
            Assert.That(sentry.Ammo.Value, Is.LessThan(40));
        }

        [UnityTest]
        public IEnumerator Strike_DamagesEverythingInRadiusAfterWarningOnly()
        {
            ProtocolController controller = SpawnCaller(Vector3.zero);
            NetworkPlayer caller = controller.GetComponent<NetworkPlayer>();
            NetworkPlayer bystander = m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(2f, 0.1f, ThrowDistance));
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, ThrowDistance + 1f));
            yield return null;

            yield return Enter(controller, StrikeSequence);
            StrikeBeacon beacon = FindPayload<StrikeBeacon>();
            Assert.That(beacon, Is.Not.Null);
            yield return HostTestHarness.Wait(StrikeWarning * 0.4f);

            Assert.That(beacon.HasStruck.Value, Is.False);
            Assert.That(grunt.GetComponent<Health>().Current.Value, Is.EqualTo(GruntHealth));
            Assert.That(bystander.Health.Current.Value, Is.EqualTo(PlayerHealth));

            yield return HostTestHarness.Wait(StrikeWarning);

            Assert.That(beacon.HasStruck.Value, Is.True);
            Assert.That(grunt.GetComponent<Health>().Current.Value, Is.EqualTo(GruntHealth - StrikeDamage));
            Assert.That(bystander.Health.Current.Value, Is.EqualTo(PlayerHealth - StrikeDamage));
            Assert.That(caller.Health.Current.Value, Is.EqualTo(PlayerHealth), "the caller stands outside the radius");
        }

        private ProtocolController SpawnCaller(Vector3 position)
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, position);
            return player.GetComponent<ProtocolController>();
        }

        private static IEnumerator Enter(ProtocolController controller, ProtocolDirection[] sequence)
        {
            foreach (ProtocolDirection direction in sequence)
                controller.SubmitDirection(direction);
            yield return null;
        }

        private T FindPayload<T>() where T : Component
        {
            T payload = Object.FindFirstObjectByType<T>();
            if (payload != null)
                m_harness.Track(payload.gameObject);
            return payload;
        }

        private MissionDirector SpawnMission(MissionTuning tuning)
        {
            var missionObject = m_harness.Track(new GameObject("TestMission"));
            missionObject.SetActive(false);
            var networkObject = missionObject.AddComponent<NetworkObject>();
            var mission = missionObject.AddComponent<MissionDirector>();
            mission.Configure(new CommRelay[0], null, null, tuning);
            HostTestHarness.AssignGlobalObjectIdHash(networkObject, 0xD20B001Au);
            missionObject.SetActive(true);
            networkObject.Spawn();
            return mission;
        }
    }
}
