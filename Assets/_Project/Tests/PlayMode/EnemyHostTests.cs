using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Enemies on a single in-process host: a grunt must path to the nearest alive player and hit it,
    /// a spitter must keep its distance and land projectiles, and lethal damage must despawn the enemy.
    /// </summary>
    public class EnemyHostTests
    {
        private const ushort TestPort = 7792;
        private const ulong ClientA = 42;
        private const ulong ClientB = 43;
        private const int PlayerHealth = 100;
        private const int GruntHealth = 60;
        private const int SpitterHealth = 80;

        private HostTestHarness m_harness;
        private GameObject m_playerTemplate;
        private GameObject m_gruntTemplate;
        private GameObject m_spitterTemplate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_harness = new HostTestHarness();
            m_harness.CreateGround(60f);
            m_harness.BuildNavMesh();

            WeaponDefinition rifle = m_harness.TrackAsset(WeaponDefinition.Create(20, 10f, 30, 0.2f, 0f, 60f));
            EnemyDefinition grunt = m_harness.TrackAsset(EnemyDefinition.Create(GruntHealth, 1, 5f, 0.4f, 10, 1.6f, 1.6f, 0f, 1f, 0.25f));
            EnemyDefinition spitter = m_harness.TrackAsset(EnemyDefinition.Create(SpitterHealth, 3, 4f, 0.4f, 15, 12f, 8f, 5f, 2f, 0.4f));

            m_playerTemplate = m_harness.CreatePlayerTemplate(rifle, 0xD20B0004u);
            GameObject projectile = m_harness.CreateProjectileTemplate(0xD20B0005u);
            m_gruntTemplate = m_harness.CreateEnemyTemplate(grunt, null, 0xD20B0006u);
            m_spitterTemplate = m_harness.CreateEnemyTemplate(spitter, projectile.GetComponent<NetworkObject>(), 0xD20B0007u);

            m_harness.StartHost(TestPort);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return m_harness.TearDown();
        }

        [UnityTest]
        public IEnumerator Grunt_ChasesAndDamagesNearestPlayer()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 8f));

            yield return HostTestHarness.Wait(3f);

            Assert.That(EnemyRules.FlatDistance(grunt.transform.position, player.transform.position), Is.LessThan(2.5f));
            Assert.That(player.Health.Current.Value, Is.LessThan(PlayerHealth));
        }

        [UnityTest]
        public IEnumerator Grunt_TargetsAlivePlayerOverDownedOne()
        {
            NetworkPlayer downed = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 3f));
            NetworkPlayer alive = m_harness.SpawnPlayer(m_playerTemplate, ClientB, new Vector3(0f, 0.05f, -6f));
            yield return null;
            downed.Health.ApplyDamage(PlayerHealth);

            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 8f));
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(grunt.Target, Is.SameAs(alive.Health));
            Assert.That(grunt.State, Is.EqualTo(EnemyState.Chase));
        }

        [UnityTest]
        public IEnumerator Enemy_WithNoAlivePlayers_StaysIdle()
        {
            NetworkPlayer downed = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            yield return null;
            downed.Health.ApplyDamage(PlayerHealth);

            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 8f));
            Vector3 start = grunt.transform.position;
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(grunt.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(EnemyRules.FlatDistance(grunt.transform.position, start), Is.LessThan(0.1f));
            Assert.That(downed.Health.Current.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Enemy_DiesAndDespawnsOnLethalDamage()
        {
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 8f));
            yield return null;
            Assert.That(EnemyCharacter.All, Has.Count.EqualTo(1));

            grunt.GetComponent<Health>().ApplyDamage(GruntHealth);
            yield return null;

            Assert.That(grunt.IsDead, Is.True);
            Assert.That(grunt.GetComponent<Collider>().enabled, Is.False);

            yield return HostTestHarness.Wait(1.3f);

            Assert.That(grunt == null, Is.True, "dead enemy should be despawned and destroyed");
            Assert.That(EnemyCharacter.All, Is.Empty);
        }

        [UnityTest]
        public IEnumerator DeadEnemy_IsNotRevivedByHeldInteract()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            EnemyCharacter grunt = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 1.5f));
            var gruntHealth = grunt.GetComponent<Health>();
            var reviver = player.GetComponent<ReviveController>();
            yield return null;

            gruntHealth.ApplyDamage(GruntHealth);
            yield return HostTestHarness.Hold(player, new PlayerCommand { Interact = true }, 0.5f);

            Assert.That(reviver.Progress.Value, Is.Zero);
            Assert.That(gruntHealth.Current.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Spitter_ProjectileDamagesPlayer()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            m_harness.SpawnEnemy(m_spitterTemplate, new Vector3(0f, 0f, 8f));

            yield return HostTestHarness.Wait(2f);

            Assert.That(player.Health.Current.Value, Is.LessThan(PlayerHealth));
        }

        [UnityTest]
        public IEnumerator Spitter_RetreatsWhenPlayerIsClose()
        {
            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            EnemyCharacter spitter = m_harness.SpawnEnemy(m_spitterTemplate, new Vector3(0f, 0f, 2f));

            yield return HostTestHarness.Wait(2f);

            Assert.That(EnemyRules.FlatDistance(spitter.transform.position, player.transform.position), Is.GreaterThan(3f));
        }

        [UnityTest]
        public IEnumerator Projectile_IsStoppedByObstacle()
        {
            GameObject wall = m_harness.Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 0.5f, 4f);
            wall.transform.localScale = new Vector3(3f, 1f, 1f);

            NetworkPlayer player = m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            m_harness.SpawnEnemy(m_spitterTemplate, new Vector3(0f, 0f, 8f));

            yield return HostTestHarness.Wait(2f);

            Assert.That(player.Health.Current.Value, Is.EqualTo(PlayerHealth));
        }

        [UnityTest]
        public IEnumerator Projectile_IgnoresShooterAndOtherEnemies()
        {
            m_harness.SpawnPlayer(m_playerTemplate, ClientA, new Vector3(0f, 0.05f, 0f));
            EnemyCharacter blocker = m_harness.SpawnEnemy(m_gruntTemplate, new Vector3(0f, 0f, 4f));
            EnemyCharacter spitter = m_harness.SpawnEnemy(m_spitterTemplate, new Vector3(0f, 0f, 8f));

            yield return HostTestHarness.Wait(2f);

            Assert.That(blocker.GetComponent<Health>().Current.Value, Is.EqualTo(GruntHealth));
            Assert.That(spitter.GetComponent<Health>().Current.Value, Is.EqualTo(SpitterHealth));
        }
    }
}
