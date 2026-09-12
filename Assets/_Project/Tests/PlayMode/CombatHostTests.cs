using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Drives combat on a single in-process host: firing must damage what the server raycast hits,
    /// reloads must refill, downed characters must ignore commands, and a teammate holding Interact must
    /// revive them. Mirrors the NetworkPlayerHostTests harness.
    /// </summary>
    public class CombatHostTests
    {
        private const ushort TestPort = 7791;
        private const ulong ClientA = 42;
        private const ulong ClientB = 43;
        private const float SettleSeconds = 0.25f;
        private const float FireSeconds = 0.3f;
        private const int MaxHealth = 100;

        private GameObject m_ground;
        private GameObject m_networkRoot;
        private NetworkManager m_networkManager;
        private GameObject m_playerTemplate;
        private GameObject m_dummyTemplate;
        private WeaponDefinition m_rifle;
        private readonly List<GameObject> m_spawned = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            m_ground.transform.localScale = new Vector3(10f, 1f, 10f);

            m_networkRoot = new GameObject("TestNetworkManager");
            m_networkManager = m_networkRoot.AddComponent<NetworkManager>();
            var transport = m_networkRoot.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", TestPorts.Free(TestPort), "127.0.0.1");
            m_networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
            };

            m_rifle = WeaponDefinition.Create(damage: 20, roundsPerSecond: 10f, magazineSize: 30, reloadSeconds: 0.2f, spreadDegrees: 0f, range: 60f);
            m_playerTemplate = CreatePlayerTemplate(m_rifle);
            m_dummyTemplate = CreateDummyTemplate();
            m_networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = m_playerTemplate });
            m_networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = m_dummyTemplate });

            Assert.That(m_networkManager.StartHost(), Is.True, "host failed to start");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject spawned in m_spawned)
            {
                if (spawned != null)
                    Object.Destroy(spawned);
            }
            m_spawned.Clear();

            if (m_networkManager != null && m_networkManager.IsListening)
                m_networkManager.Shutdown();

            yield return null;

            Object.Destroy(m_networkRoot);
            Object.Destroy(m_playerTemplate);
            Object.Destroy(m_dummyTemplate);
            Object.Destroy(m_ground);
            Object.Destroy(m_rifle);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FireCommand_DamagesDummyInFront()
        {
            NetworkPlayer shooter = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            Health dummy = SpawnDummy(new Vector3(0f, 0f, 4f));
            yield return Wait(SettleSeconds);

            yield return Hold(shooter, new PlayerCommand { Fire = true, Aim = new Vector2(0f, 1f) }, FireSeconds);

            Assert.That(dummy.Current.Value, Is.LessThan(MaxHealth));
            Assert.That(shooter.GetComponent<WeaponController>().Ammo.Value, Is.LessThan(m_rifle.MagazineSize));
        }

        [UnityTest]
        public IEnumerator FireCommand_DoesNotDamageDummyBehind()
        {
            NetworkPlayer shooter = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            Health dummy = SpawnDummy(new Vector3(0f, 0f, -4f));
            yield return Wait(SettleSeconds);

            yield return Hold(shooter, new PlayerCommand { Fire = true, Aim = new Vector2(0f, 1f) }, FireSeconds);

            Assert.That(dummy.Current.Value, Is.EqualTo(MaxHealth));
        }

        [UnityTest]
        public IEnumerator FireWithEmptyMagazine_ReloadsAutomatically()
        {
            WeaponDefinition tiny = WeaponDefinition.Create(damage: 20, roundsPerSecond: 50f, magazineSize: 2, reloadSeconds: 0.2f, spreadDegrees: 0f, range: 60f);
            NetworkPlayer shooter = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f), tiny);
            var weapon = shooter.GetComponent<WeaponController>();
            yield return Wait(SettleSeconds);

            yield return Hold(shooter, new PlayerCommand { Fire = true, Aim = new Vector2(0f, 1f) }, 0.15f);
            Assert.That(weapon.Ammo.Value, Is.Zero);
            Assert.That(weapon.IsReloading.Value, Is.True);

            yield return Hold(shooter, PlayerCommand.None, 0.3f);
            Assert.That(weapon.Ammo.Value, Is.EqualTo(2));
            Assert.That(weapon.IsReloading.Value, Is.False);

            Object.Destroy(tiny);
        }

        [UnityTest]
        public IEnumerator ReloadCommand_RefillsPartialMagazine()
        {
            NetworkPlayer shooter = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            var weapon = shooter.GetComponent<WeaponController>();
            yield return Wait(SettleSeconds);

            shooter.SubmitCommand(new PlayerCommand { Fire = true, Aim = new Vector2(0f, 1f) });
            yield return null;
            yield return null;
            Assert.That(weapon.Ammo.Value, Is.LessThan(m_rifle.MagazineSize));

            yield return Hold(shooter, new PlayerCommand { Reload = true }, 0.05f);
            yield return Wait(m_rifle.ReloadSeconds + 0.1f);

            Assert.That(weapon.Ammo.Value, Is.EqualTo(m_rifle.MagazineSize));
        }

        [UnityTest]
        public IEnumerator DownedPlayer_IgnoresMoveCommands()
        {
            NetworkPlayer player = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            var health = player.GetComponent<Health>();
            yield return Wait(SettleSeconds);

            health.ApplyDamage(MaxHealth);
            Assert.That(health.IsDowned, Is.True);
            Vector3 start = player.transform.position;

            yield return Hold(player, new PlayerCommand { Move = new Vector2(0f, 1f) }, 0.5f);

            Assert.That((player.transform.position - start).magnitude, Is.LessThan(0.01f));
            Assert.That(player.Character.LastCommand.HasMove, Is.False);
        }

        [UnityTest]
        public IEnumerator FriendlyFire_DamagesTeammate()
        {
            NetworkPlayer shooter = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            NetworkPlayer teammate = SpawnPlayer(ClientB, new Vector3(0f, 0.05f, 4f));
            var teammateHealth = teammate.GetComponent<Health>();
            yield return Wait(SettleSeconds);

            yield return Hold(shooter, new PlayerCommand { Fire = true, Aim = new Vector2(0f, 1f) }, FireSeconds);

            Assert.That(teammateHealth.Current.Value, Is.LessThan(MaxHealth));
            Assert.That(shooter.GetComponent<Health>().Current.Value, Is.EqualTo(MaxHealth));
        }

        [UnityTest]
        public IEnumerator Revive_ByTeammateHoldingInteract_RestoresHealth()
        {
            NetworkPlayer reviver = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            NetworkPlayer downed = SpawnPlayer(ClientB, new Vector3(0f, 0.05f, 1.5f));
            var downedHealth = downed.GetComponent<Health>();
            var reviveController = reviver.GetComponent<ReviveController>();
            yield return Wait(SettleSeconds);

            downedHealth.ApplyDamage(MaxHealth);
            yield return Hold(reviver, new PlayerCommand { Interact = true }, reviveController.ReviveSeconds + 0.3f);

            Assert.That(downedHealth.IsDowned, Is.False);
            Assert.That(downedHealth.Current.Value, Is.EqualTo(HealthRules.ReviveHitPoints(MaxHealth, reviveController.ReviveHealthFraction)));
            Assert.That(reviveController.Progress.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Revive_ReleasingInteract_ResetsProgress()
        {
            NetworkPlayer reviver = SpawnPlayer(ClientA, new Vector3(0f, 0.05f, 0f));
            NetworkPlayer downed = SpawnPlayer(ClientB, new Vector3(0f, 0.05f, 1.5f));
            var reviveController = reviver.GetComponent<ReviveController>();
            yield return Wait(SettleSeconds);

            downed.GetComponent<Health>().ApplyDamage(MaxHealth);
            yield return Hold(reviver, new PlayerCommand { Interact = true }, 0.5f);
            Assert.That(reviveController.Progress.Value, Is.GreaterThan(0f));

            yield return Hold(reviver, PlayerCommand.None, 0.1f);

            Assert.That(reviveController.Progress.Value, Is.Zero);
            Assert.That(downed.GetComponent<Health>().IsDowned, Is.True);
        }

        [UnityTest]
        public IEnumerator TargetDummy_ResetsAfterDelay()
        {
            Health dummy = SpawnDummy(new Vector3(0f, 0f, 4f));
            float resetSeconds = dummy.GetComponent<TargetDummy>().ResetSeconds;
            yield return null;

            dummy.ApplyDamage(MaxHealth);
            Assert.That(dummy.IsDowned, Is.True);

            yield return Wait(resetSeconds + 0.3f);

            Assert.That(dummy.Current.Value, Is.EqualTo(MaxHealth));
        }

        private NetworkPlayer SpawnPlayer(ulong ownerClientId, Vector3 position, WeaponDefinition definition = null)
        {
            GameObject instance = Object.Instantiate(m_playerTemplate, position, Quaternion.identity);
            m_spawned.Add(instance);
            if (definition != null)
                instance.GetComponent<WeaponController>().SetDefinition(definition);

            instance.SetActive(true);
            var player = instance.GetComponent<NetworkPlayer>();
            instance.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);
            player.PlayerIndex.Value = (int)(ownerClientId - ClientA);
            return player;
        }

        private Health SpawnDummy(Vector3 position)
        {
            GameObject instance = Object.Instantiate(m_dummyTemplate, position, Quaternion.identity);
            m_spawned.Add(instance);
            instance.SetActive(true);
            instance.GetComponent<NetworkObject>().Spawn();
            return instance.GetComponent<Health>();
        }

        private static IEnumerator Hold(NetworkPlayer player, PlayerCommand command, float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                player.SubmitCommand(command);
                yield return null;
            }
        }

        private static GameObject CreatePlayerTemplate(WeaponDefinition definition)
        {
            var template = new GameObject("TestCombatPlayerTemplate");
            template.SetActive(false);

            var controller = template.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            template.AddComponent<CharacterMotor>();
            template.AddComponent<PlayerCharacter>();
            template.AddComponent<Health>();
            template.AddComponent<WeaponController>().SetDefinition(definition);
            template.AddComponent<ReviveController>();

            var networkObject = template.AddComponent<NetworkObject>();
            AssignGlobalObjectIdHash(networkObject, 0xD20B0002u);
            var networkTransform = template.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            template.AddComponent<NetworkPlayer>();

            return template;
        }

        private static GameObject CreateDummyTemplate()
        {
            var template = new GameObject("TestDummyTemplate");
            template.SetActive(false);

            var collider = template.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.4f;
            template.AddComponent<Health>();
            template.AddComponent<TargetDummy>();

            var networkObject = template.AddComponent<NetworkObject>();
            AssignGlobalObjectIdHash(networkObject, 0xD20B0003u);

            return template;
        }

        // Runtime-built NetworkObjects have no GlobalObjectIdHash, and Netcode refuses to spawn a zero hash.
        private static void AssignGlobalObjectIdHash(NetworkObject networkObject, uint hash)
        {
            FieldInfo field = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "NetworkObject.GlobalObjectIdHash field not found; Netcode version changed?");
            field.SetValue(networkObject, hash);
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }
    }
}
