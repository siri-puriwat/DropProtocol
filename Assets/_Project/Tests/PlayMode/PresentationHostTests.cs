using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Character presentation on a single in-process host: the animator feed is derived from replicated
    /// state and transform motion only. Runs without an Animator; the snapshot is what would be written.
    /// </summary>
    public class PresentationHostTests
    {
        private const ushort TestPort = 7797;
        private const ulong ClientA = 42;
        private const float Cooldown = 1f;

        private static readonly ProtocolDirection[] SupplySequence =
            { ProtocolDirection.Down, ProtocolDirection.Down, ProtocolDirection.Up, ProtocolDirection.Right };

        private HostTestHarness m_harness;
        private GameObject m_playerTemplate;
        private GameObject m_gruntTemplate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_harness = new HostTestHarness();
            m_harness.CreateGround(40f);
            m_harness.BuildNavMesh();

            EnemyDefinition grunt = m_harness.TrackAsset(EnemyDefinition.Create(40, 1, 5f, 0.4f, 10, 1.6f, 1.6f, 0f, 1f, 0.3f));
            m_gruntTemplate = m_harness.CreateEnemyTemplate(grunt, null, 0xD20B001Du);
            m_gruntTemplate.AddComponent<EnemyPresentation>();

            WeaponDefinition rifle = m_harness.TrackAsset(WeaponDefinition.Create(20, 10f, 30, 0.5f, 0f, 60f));
            GameObject supply = m_harness.CreateNetworkTemplate("TestSupplyPod", 0xD20B001Cu);
            supply.AddComponent<SupplyPod>().Configure(0.2f, 6f, 30, 10f);
            var loadout = new[]
            {
                m_harness.TrackAsset(ProtocolDefinition.Create("Supply", SupplySequence, Cooldown, supply.GetComponent<NetworkObject>())),
            };
            m_playerTemplate = m_harness.CreatePlayerTemplate(rifle, 0xD20B001Bu, loadout, ProtocolTuning.Default);
            m_playerTemplate.AddComponent<CharacterPresentation>();
            m_playerTemplate.AddComponent<PlayerTint>();
            AddHealthBar(m_playerTemplate);

            m_harness.StartHost(TestPort);
            yield return null;
        }

        // The template is inactive, so wiring the private references here lands before Awake runs.
        private static void AddHealthBar(GameObject template)
        {
            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(template.transform, false);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);

            HealthBar healthBar = bar.AddComponent<HealthBar>();
            SetPrivateField(healthBar, "m_health", template.GetComponent<Health>());
            SetPrivateField(healthBar, "m_fill", fill.transform);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"{target.GetType().Name}.{name} not found");
            field.SetValue(target, value);
        }

        // Netcode applies the initial field values through ReadField, which never raises OnValueChanged.
        private static void ReadField(NetworkVariable<int> variable, int value)
        {
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            NetworkVariableSerialization<int>.Write(writer, ref value);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            variable.ReadField(reader);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return m_harness.TearDown();
        }

        // A client enables the instance at default values, receives the initial values silently, and only
        // then gets OnNetworkSpawn; presentation must re-read the replicated state there.
        [Test]
        public void EnabledBeforeInitialSync_ReappliesOnNetworkSpawn()
        {
            GameObject instance = m_harness.Track(Object.Instantiate(m_playerTemplate, new Vector3(0f, 0.1f, 0f), Quaternion.identity));
            instance.SetActive(true);
            var player = instance.GetComponent<NetworkPlayer>();
            var presentation = instance.GetComponent<CharacterPresentation>();
            var tint = instance.GetComponent<PlayerTint>();
            var healthBar = instance.GetComponentInChildren<HealthBar>();
            Transform fill = instance.transform.Find("HealthBar/Fill");
            Color unassigned = tint.CurrentColor;
            Assert.That(presentation.Snapshot.IsDowned, Is.True);
            Assert.That(fill.localScale.x, Is.EqualTo(0f));

            ReadField(player.Health.Current, 100);
            ReadField(player.PlayerIndex, 2);
            Assert.That(presentation.Snapshot.IsDowned, Is.True, "initial synchronisation must not raise OnValueChanged");
            Assert.That(fill.localScale.x, Is.EqualTo(0f));
            Assert.That(tint.CurrentColor, Is.EqualTo(unassigned));

            presentation.OnNetworkSpawn();
            tint.OnNetworkSpawn();
            healthBar.OnNetworkSpawn();

            Assert.That(presentation.Snapshot.IsDowned, Is.False);
            Assert.That(fill.localScale.x, Is.EqualTo(1f));
            Assert.That(tint.CurrentColor, Is.Not.EqualTo(unassigned));
        }

        [UnityTest]
        public IEnumerator MovingForward_FeedsForwardLocomotion()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return null;

            yield return HostTestHarness.Hold(player, new PlayerCommand { Move = Vector2.up, Aim = Vector2.up }, 0.6f);

            PresentationSnapshot snapshot = player.GetComponent<CharacterPresentation>().Snapshot;
            Assert.That(snapshot.Move.y, Is.GreaterThan(0.5f));
            Assert.That(Mathf.Abs(snapshot.Move.x), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator StrafingWhileAimingForward_FeedsSidewaysLocomotion()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return null;

            yield return HostTestHarness.Hold(player, new PlayerCommand { Move = Vector2.right, Aim = Vector2.up }, 0.6f);

            PresentationSnapshot snapshot = player.GetComponent<CharacterPresentation>().Snapshot;
            Assert.That(snapshot.Move.x, Is.GreaterThan(0.5f));
            Assert.That(Mathf.Abs(snapshot.Move.y), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator Standing_FeedsZeroLocomotion()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(player.GetComponent<CharacterPresentation>().Snapshot.Move, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator DownedAndRevived_TracksHealth()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return null;
            var presentation = player.GetComponent<CharacterPresentation>();

            player.Health.ApplyDamage(100);
            yield return null;
            Assert.That(presentation.Snapshot.IsDowned, Is.True);

            player.Health.Revive(50);
            yield return null;
            Assert.That(presentation.Snapshot.IsDowned, Is.False);
        }

        [UnityTest]
        public IEnumerator Reloading_TracksWeapon()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return null;
            var presentation = player.GetComponent<CharacterPresentation>();

            yield return HostTestHarness.Hold(player, new PlayerCommand { Fire = true, Aim = Vector2.up }, 0.15f);
            yield return HostTestHarness.Hold(player, new PlayerCommand { Reload = true, Aim = Vector2.up }, 0.1f);
            Assert.That(presentation.Snapshot.IsReloading, Is.True);

            yield return HostTestHarness.Wait(0.6f);
            Assert.That(presentation.Snapshot.IsReloading, Is.False);
        }

        [UnityTest]
        public IEnumerator HoldingInteract_ReplicatesAndFeedsInteracting()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return null;
            var presentation = player.GetComponent<CharacterPresentation>();

            yield return HostTestHarness.Hold(player, new PlayerCommand { Interact = true }, 0.2f);
            Assert.That(player.IsInteracting.Value, Is.True);
            Assert.That(presentation.Snapshot.IsInteracting, Is.True);

            yield return HostTestHarness.Hold(player, PlayerCommand.None, 0.2f);
            Assert.That(presentation.Snapshot.IsInteracting, Is.False);
        }

        [UnityTest]
        public IEnumerator ProtocolCall_RaisesCalledWithSlot()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            yield return null;
            var controller = player.GetComponent<ProtocolController>();
            int calledSlot = -1;
            controller.Called += slot => calledSlot = slot;

            foreach (ProtocolDirection direction in SupplySequence)
            {
                controller.SubmitDirection(direction);
                yield return null;
            }

            yield return null;
            Assert.That(calledSlot, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Enemy_ReplicatesChaseThenAttackWindupAsTelegraph()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 6f));
            var presentation = grunt.GetComponent<EnemyPresentation>();
            yield return HostTestHarness.Wait(0.3f);

            Assert.That(grunt.ReplicatedState.Value, Is.EqualTo(EnemyState.Chase));
            Assert.That(presentation.ShownState, Is.EqualTo(EnemyState.Chase));
            Assert.That(presentation.IsTelegraphing, Is.False);

            bool telegraphed = false;
            float end = Time.time + 3f;
            while (Time.time < end && !telegraphed)
            {
                telegraphed = presentation.IsTelegraphing && grunt.ReplicatedState.Value == EnemyState.Attack;
                yield return null;
            }

            Assert.That(telegraphed, Is.True, "attack windup should be shown as a telegraph");
        }

        [UnityTest]
        public IEnumerator Enemy_ReplicatesDeath()
        {
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 6f));
            var presentation = grunt.GetComponent<EnemyPresentation>();
            yield return null;

            grunt.GetComponent<Health>().ApplyDamage(40);
            yield return null;

            Assert.That(grunt.ReplicatedState.Value, Is.EqualTo(EnemyState.Dead));
            Assert.That(presentation.ShownState, Is.EqualTo(EnemyState.Dead));
            Assert.That(presentation.IsTelegraphing, Is.False);
        }
    }
}
