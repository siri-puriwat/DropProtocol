using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>The director on a single in-process host must respect budget, population and spawn distance.</summary>
    public class EnemyDirectorHostTests
    {
        private const ushort TestPort = 7793;
        private const ulong ClientA = 42;
        private const int PopulationCap = 3;

        private HostTestHarness m_harness;
        private GameObject m_playerTemplate;
        private GameObject m_gruntTemplate;
        private GameObject m_spawnRoot;
        private EnemyDirector m_director;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_harness = new HostTestHarness();
            m_harness.CreateGround(60f);
            m_harness.BuildNavMesh();

            WeaponDefinition rifle = m_harness.TrackAsset(WeaponDefinition.Create(20, 10f, 30, 0.2f, 0f, 60f));
            EnemyDefinition grunt = m_harness.TrackAsset(EnemyDefinition.Create(60, 1, 5f, 0.4f, 10, 1.6f, 1.6f, 0f, 1f, 0.25f));
            m_playerTemplate = m_harness.CreatePlayerTemplate(rifle, 0xD20B0008u);
            m_gruntTemplate = m_harness.CreateEnemyTemplate(grunt, null, 0xD20B0009u);

            m_spawnRoot = m_harness.Track(new GameObject("EnemySpawns"));
            Vector3[] points = { new Vector3(15f, 0f, 15f), new Vector3(-15f, 0f, -15f), new Vector3(15f, 0f, -15f), new Vector3(-15f, 0f, 15f) };
            var transforms = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var point = new GameObject("EnemySpawn_" + i);
                point.transform.SetParent(m_spawnRoot.transform, false);
                point.transform.position = points[i];
                transforms[i] = point.transform;
            }

            var directorObject = m_harness.Track(new GameObject("TestDirector"));
            directorObject.SetActive(false);
            var networkObject = directorObject.AddComponent<NetworkObject>();
            m_director = directorObject.AddComponent<EnemyDirector>();
            DirectorTuning tuning = DirectorTuning.Default;
            tuning.BaseBudgetPerSecond = 10f;
            tuning.MaxBudget = 12f;
            tuning.BasePopulationCap = PopulationCap;
            tuning.ThinkSeconds = 0.1f;
            tuning.MinSpawnDistance = 10f;
            m_director.Configure(new[] { m_gruntTemplate.GetComponent<NetworkObject>() }, new[] { 1 }, transforms, tuning, () => 0.0);

            m_harness.StartHost(TestPort);
            yield return null;

            HostTestHarness.AssignGlobalObjectIdHash(networkObject, 0xD20B000Au);
            directorObject.SetActive(true);
            networkObject.Spawn();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return m_harness.TearDown();
        }

        [UnityTest]
        public IEnumerator Director_SpawnsUpToPopulationCap()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            m_director.Running = true;

            yield return HostTestHarness.Wait(2f);

            Assert.That(EnemyCharacter.All, Has.Count.EqualTo(PopulationCap));
        }

        [UnityTest]
        public IEnumerator Director_NotRunning_SpawnsNothing()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));

            yield return HostTestHarness.Wait(1f);

            Assert.That(EnemyCharacter.All, Is.Empty);
            Assert.That(m_director.Budget, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Director_DebugSpawn_IgnoresBudget()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));

            EnemyCharacter spawned = m_director.Spawn(0);
            yield return null;

            Assert.That(spawned, Is.Not.Null);
            Assert.That(EnemyCharacter.All, Has.Count.EqualTo(1));
            Assert.That(m_director.Budget, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Director_SpawnsAwayFromPlayers()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(14f, 0.05f, 14f));
            yield return null;

            EnemyCharacter spawned = m_director.Spawn(0);

            Assert.That(EnemyRules.FlatDistance(spawned.transform.position, player.transform.position), Is.GreaterThan(10f));
        }
    }
}
