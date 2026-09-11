using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Squad bots on a single in-process host: they follow the nearest human, fight from formation without
    /// shooting through squadmates, revive downed players, and fill the squad slots humans leave empty.
    /// </summary>
    public class BotHostTests
    {
        private const ushort TestPort = 7794;
        private const ulong ClientA = 42;
        private const int PlayerHealth = 100;
        private const int GruntHealth = 1000;
        private const int Magazine = 30;
        private const float ReloadSeconds = 0.5f;
        private const int SlotCount = 4;

        private HostTestHarness m_harness;
        private GameObject m_playerTemplate;
        private GameObject m_gruntTemplate;
        private BotTuning m_tuning;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_harness = new HostTestHarness();
            m_harness.CreateGround(60f);
            m_harness.BuildNavMesh();

            WeaponDefinition rifle = m_harness.TrackAsset(WeaponDefinition.Create(20, 10f, Magazine, ReloadSeconds, 0f, 60f));
            // A tanky grunt that barely moves and cannot hurt anyone keeps the geometry of each test fixed.
            EnemyDefinition grunt = m_harness.TrackAsset(EnemyDefinition.Create(GruntHealth, 1, 0.1f, 0.4f, 0, 1.6f, 1.6f, 0f, 1f, 0.25f));
            m_playerTemplate = m_harness.CreatePlayerTemplate(rifle, 0xD20B000Bu);
            m_gruntTemplate = m_harness.CreateEnemyTemplate(grunt, null, 0xD20B000Cu);

            m_tuning = BotTuning.Default;
            m_tuning.ThinkSeconds = 0.1f;

            m_harness.StartHost(TestPort);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return m_harness.TearDown();
        }

        [UnityTest]
        public IEnumerator Bot_FollowsNearestAliveHuman()
        {
            NetworkPlayer human = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(12f, 0.1f, 0f), m_tuning);

            yield return HostTestHarness.Wait(3f);

            Assert.That(bot.IsBot, Is.True);
            Assert.That(EnemyRules.FlatDistance(bot.transform.position, human.transform.position), Is.InRange(1f, m_tuning.FollowResumeRadius));
        }

        [UnityTest]
        public IEnumerator Bot_WithoutHumans_HoldsPosition()
        {
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(0f, 0.1f, 0f), m_tuning);
            Vector3 start = bot.transform.position;

            yield return HostTestHarness.Wait(1f);

            Assert.That(EnemyRules.FlatDistance(bot.transform.position, start), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator Bot_PathsAroundAnObstacle()
        {
            GameObject wall = m_harness.Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1f, 5f);
            wall.transform.localScale = new Vector3(8f, 2f, 1f);
            yield return null;
            m_harness.BuildNavMesh();

            NetworkPlayer human = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 10f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(0f, 0.1f, 0f), m_tuning);

            yield return HostTestHarness.Wait(4f);

            Assert.That(bot.transform.position.z, Is.GreaterThan(5f));
            Assert.That(EnemyRules.FlatDistance(bot.transform.position, human.transform.position), Is.LessThan(m_tuning.FollowResumeRadius));
        }

        [UnityTest]
        public IEnumerator Bot_FiresAtEnemyInRange()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(2f, 0.1f, 0f), m_tuning);
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(2f, 0f, 10f));

            yield return HostTestHarness.Wait(1.5f);

            Assert.That(grunt.GetComponent<Health>().Current.Value, Is.LessThan(GruntHealth));
            Assert.That(bot.GetComponent<WeaponController>().Ammo.Value, Is.LessThan(Magazine));
            Assert.That(StateOf(bot), Is.EqualTo(BotState.Combat));
        }

        [UnityTest]
        public IEnumerator Bot_HoldsFireWhenASquadmateBlocksTheLine()
        {
            NetworkPlayer human = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 3f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(0f, 0.1f, 0f), m_tuning);
            m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 9f));

            yield return HostTestHarness.Wait(1f);

            Assert.That(StateOf(bot), Is.EqualTo(BotState.Combat));
            Assert.That(human.Health.Current.Value, Is.EqualTo(PlayerHealth));
            Assert.That(bot.GetComponent<WeaponController>().Ammo.Value, Is.EqualTo(Magazine));
        }

        [UnityTest]
        public IEnumerator Bot_BacksOffFromACloseEnemy()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(3f, 0.1f, -3f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(0f, 0.1f, 0f), m_tuning);
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 2f));

            yield return HostTestHarness.Wait(1.5f);

            Assert.That(EnemyRules.FlatDistance(bot.transform.position, grunt.transform.position), Is.GreaterThan(m_tuning.RetreatRange));
        }

        [UnityTest]
        public IEnumerator Bot_RevivesADownedHuman()
        {
            NetworkPlayer human = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.1f, 0f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(6f, 0.1f, 0f), m_tuning);
            yield return null;
            human.Health.ApplyDamage(PlayerHealth);

            yield return HostTestHarness.Wait(bot.GetComponent<ReviveController>().ReviveSeconds + 2.5f);

            Assert.That(human.Health.IsDowned, Is.False);
            Assert.That(human.Health.Current.Value, Is.EqualTo(HealthRules.ReviveHitPoints(PlayerHealth, 0.5f)));
        }

        [UnityTest]
        public IEnumerator Bot_ReloadsOnceNothingIsInSight()
        {
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(0f, 0.1f, 0f), m_tuning);
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 8f));
            var weapon = bot.GetComponent<WeaponController>();

            yield return HostTestHarness.Wait(0.6f);
            Assert.That(weapon.Ammo.Value, Is.LessThan(Magazine));

            grunt.GetComponent<Health>().ApplyDamage(GruntHealth);
            yield return HostTestHarness.Wait(ReloadSeconds + 1f);

            Assert.That(weapon.Ammo.Value, Is.EqualTo(Magazine));
        }

        [UnityTest]
        public IEnumerator DownedBot_DoesNothing()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(10f, 0.1f, 0f));
            NetworkPlayer bot = m_harness.SpawnBot(m_playerTemplate, new Vector3(0f, 0.1f, 0f), m_tuning);
            yield return null;
            bot.Health.ApplyDamage(PlayerHealth);
            Vector3 start = bot.transform.position;

            yield return HostTestHarness.Wait(1f);

            Assert.That(EnemyRules.FlatDistance(bot.transform.position, start), Is.LessThan(0.1f));
            Assert.That(bot.Character.LastCommand.HasMove, Is.False);
        }

        [UnityTest]
        public IEnumerator Spawner_FillsEmptySlotsWithBots()
        {
            PlayerSpawner spawner = SpawnSpawner(true);
            yield return null;

            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(SlotCount));
            Assert.That(spawner.BotCount, Is.EqualTo(SlotCount - 1));

            var botSlots = new List<int>();
            NetworkPlayer human = null;
            foreach (NetworkPlayer player in NetworkPlayer.All)
            {
                if (player.IsBot)
                    botSlots.Add(player.PlayerIndex.Value);
                else
                    human = player;
            }

            botSlots.Sort();
            Assert.That(botSlots, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(human, Is.Not.Null);
            Assert.That(NetworkPlayer.LocalPlayer, Is.SameAs(human));
        }

        [UnityTest]
        public IEnumerator Spawner_RemoveBots_DespawnsThem()
        {
            PlayerSpawner spawner = SpawnSpawner(true);
            yield return null;

            spawner.FillWithBots = false;
            yield return null;

            Assert.That(spawner.BotCount, Is.Zero);
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Spawner_EnablingBotsLater_FillsTheSquad()
        {
            PlayerSpawner spawner = SpawnSpawner(false);
            yield return null;
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(1));

            spawner.FillWithBots = true;
            yield return null;

            Assert.That(spawner.BotCount, Is.EqualTo(SlotCount - 1));
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(SlotCount));
        }

        private static BotState StateOf(NetworkPlayer bot)
        {
            return ((BotCommandSource)bot.Character.CommandSource).State;
        }

        // Mirrors the in-scene spawner: built inactive, configured, then spawned on the running host.
        private PlayerSpawner SpawnSpawner(bool fillWithBots)
        {
            GameObject root = m_harness.Track(new GameObject("SpawnPoints"));
            var points = new Transform[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                var point = new GameObject("SpawnPoint_" + i);
                point.transform.SetParent(root.transform, false);
                point.transform.position = new Vector3(i * 3f, 0.1f, 0f);
                points[i] = point.transform;
            }

            GameObject spawnerObject = m_harness.Track(new GameObject("TestSpawner"));
            spawnerObject.SetActive(false);
            var networkObject = spawnerObject.AddComponent<NetworkObject>();
            var spawner = spawnerObject.AddComponent<PlayerSpawner>();
            spawner.Configure(m_playerTemplate.GetComponent<NetworkObject>(), points, m_tuning, fillWithBots);
            HostTestHarness.AssignGlobalObjectIdHash(networkObject, 0xD20B000Du);
            spawnerObject.SetActive(true);
            networkObject.Spawn();
            return spawner;
        }
    }
}
