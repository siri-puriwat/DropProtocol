using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.AI;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Shared pieces for in-process host fixtures that need enemies: a ground plane with a runtime
    /// NavMesh, a NetworkManager on a private port, code-built prefab templates and cleanup.
    /// </summary>
    public sealed class HostTestHarness
    {
        private readonly List<GameObject> m_spawned = new List<GameObject>();
        private readonly List<GameObject> m_templates = new List<GameObject>();
        private readonly List<Object> m_assets = new List<Object>();
        private GameObject m_root;
        private NavMeshSurface m_surface;

        public GameObject Ground { get; private set; }
        public NetworkManager NetworkManager { get; private set; }

        public void CreateGround(float size)
        {
            Ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
        }

        /// <summary>Bakes whatever colliders exist right now, so call it before spawning actors.</summary>
        public void BuildNavMesh()
        {
            if (m_surface == null)
            {
                m_surface = Ground.AddComponent<NavMeshSurface>();
                m_surface.collectObjects = CollectObjects.All;
                m_surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            }
            else
            {
                m_surface.RemoveData();
            }

            m_surface.BuildNavMesh();
        }

        public void StartHost(ushort port)
        {
            m_root = new GameObject("TestNetworkManager");
            NetworkManager = m_root.AddComponent<NetworkManager>();
            var transport = m_root.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", TestPorts.Free(port), "127.0.0.1");
            NetworkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
            };

            foreach (GameObject template in m_templates)
                NetworkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = template });

            Assert.That(NetworkManager.StartHost(), Is.True, "host failed to start");
        }

        public T TrackAsset<T>(T asset) where T : Object
        {
            m_assets.Add(asset);
            return asset;
        }

        public GameObject Track(GameObject spawned)
        {
            m_spawned.Add(spawned);
            return spawned;
        }

        public IEnumerator TearDown()
        {
            foreach (GameObject spawned in m_spawned)
            {
                if (spawned != null)
                    Object.Destroy(spawned);
            }
            m_spawned.Clear();

            if (NetworkManager != null && NetworkManager.IsListening)
                NetworkManager.Shutdown();

            yield return null;

            if (m_surface != null)
                m_surface.RemoveData();

            Object.Destroy(m_root);
            foreach (GameObject template in m_templates)
                Object.Destroy(template);
            foreach (Object asset in m_assets)
                Object.Destroy(asset);
            Object.Destroy(Ground);
            yield return null;
        }

        public GameObject CreatePlayerTemplate(WeaponDefinition weapon, uint hash)
        {
            var template = NewTemplate("TestPlayerTemplate");
            var controller = template.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            template.AddComponent<CharacterMotor>();
            template.AddComponent<PlayerCharacter>();
            template.AddComponent<Health>();
            template.AddComponent<WeaponController>().SetDefinition(weapon);
            template.AddComponent<ReviveController>();
            AddNetworking(template, hash);
            template.AddComponent<NetworkPlayer>();
            return template;
        }

        public GameObject CreatePlayerTemplate(WeaponDefinition weapon, uint hash, ProtocolDefinition[] loadout, ProtocolTuning tuning)
        {
            GameObject template = CreatePlayerTemplate(weapon, hash);
            template.AddComponent<ProtocolController>().Configure(loadout, tuning);
            return template;
        }

        /// <summary>Bare networked template; the caller adds the payload component and configures it.</summary>
        public GameObject CreateNetworkTemplate(string name, uint hash)
        {
            var template = NewTemplate(name);
            AddNetworking(template, hash);
            return template;
        }

        public GameObject CreateEnemyTemplate(EnemyDefinition definition, NetworkObject projectilePrefab, uint hash)
        {
            var template = NewTemplate("TestEnemyTemplate");
            var collider = template.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.4f;
            var agent = template.AddComponent<NavMeshAgent>();
            agent.radius = definition.AgentRadius;
            agent.height = 1.8f;
            agent.enabled = false;
            template.AddComponent<Health>();
            template.AddComponent<EnemyCharacter>().SetDefinition(definition);
            if (projectilePrefab != null)
                template.AddComponent<SpitAttack>().SetProjectilePrefab(projectilePrefab);
            else
                template.AddComponent<MeleeAttack>();
            AddNetworking(template, hash);
            return template;
        }

        public GameObject CreateProjectileTemplate(uint hash)
        {
            var template = NewTemplate("TestProjectileTemplate");
            AddNetworking(template, hash);
            template.AddComponent<SpitProjectile>();
            return template;
        }

        public NetworkPlayer SpawnPlayer(GameObject template, ulong ownerClientId, Vector3 position)
        {
            GameObject instance = Track(Object.Instantiate(template, position, Quaternion.identity));
            instance.SetActive(true);
            instance.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);
            return instance.GetComponent<NetworkPlayer>();
        }

        public NetworkPlayer SpawnBot(GameObject template, Vector3 position, BotTuning tuning)
        {
            GameObject instance = Track(Object.Instantiate(template, position, Quaternion.identity));
            instance.SetActive(true);
            var player = instance.GetComponent<NetworkPlayer>();
            player.ConfigureBot(new BotCommandSource(player, tuning, () => Time.timeAsDouble));
            instance.GetComponent<NetworkObject>().Spawn();
            return player;
        }

        public EnemyCharacter SpawnEnemy(GameObject template, Vector3 position)
        {
            GameObject instance = Track(Object.Instantiate(template, position, Quaternion.identity));
            instance.SetActive(true);
            instance.GetComponent<NetworkObject>().Spawn();
            return instance.GetComponent<EnemyCharacter>();
        }

        public static IEnumerator Hold(NetworkPlayer player, PlayerCommand command, float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                player.SubmitCommand(command);
                yield return null;
            }
        }

        public static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        private GameObject NewTemplate(string name)
        {
            var template = new GameObject(name);
            // Kept inactive so it behaves like a prefab template.
            template.SetActive(false);
            m_templates.Add(template);
            return template;
        }

        private static void AddNetworking(GameObject template, uint hash)
        {
            var networkObject = template.AddComponent<NetworkObject>();
            AssignGlobalObjectIdHash(networkObject, hash);
            var networkTransform = template.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        }

        // Runtime-built NetworkObjects have no GlobalObjectIdHash, and Netcode refuses to spawn a zero hash.
        public static void AssignGlobalObjectIdHash(NetworkObject networkObject, uint hash)
        {
            FieldInfo field = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "NetworkObject.GlobalObjectIdHash field not found; Netcode version changed?");
            field.SetValue(networkObject, hash);
        }
    }
}
