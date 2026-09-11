using System.Collections;
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
    /// Drives the server side of the command flow on a single in-process host: a spawned NetworkPlayer
    /// owned by a remote client must move when commands are submitted for it, and the host's own player
    /// must bind to its local command source. Multi-process behaviour is covered by Multiplayer Play Mode.
    /// </summary>
    public class NetworkPlayerHostTests
    {
        private const ushort TestPort = 7790;
        private const ulong RemoteClientId = 42;
        private const float SettleSeconds = 0.25f;
        private const float DriveSeconds = 0.5f;

        private GameObject m_ground;
        private GameObject m_networkRoot;
        private NetworkManager m_networkManager;
        private GameObject m_playerTemplate;
        private GameObject m_spawned;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            m_ground.transform.localScale = new Vector3(10f, 1f, 10f);

            m_networkRoot = new GameObject("TestNetworkManager");
            m_networkManager = m_networkRoot.AddComponent<NetworkManager>();
            var transport = m_networkRoot.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", TestPort, "127.0.0.1");
            m_networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
            };

            m_playerTemplate = CreatePlayerTemplate();
            m_networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = m_playerTemplate });

            Assert.That(m_networkManager.StartHost(), Is.True, "host failed to start");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_spawned != null)
                Object.Destroy(m_spawned);

            if (m_networkManager != null && m_networkManager.IsListening)
                m_networkManager.Shutdown();

            yield return null;

            Object.Destroy(m_networkRoot);
            Object.Destroy(m_playerTemplate);
            Object.Destroy(m_ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoteOwnedPlayer_MovesWhenCommandIsSubmitted()
        {
            NetworkPlayer player = Spawn(RemoteClientId, new Vector3(0f, 0.05f, 0f));
            yield return Wait(SettleSeconds);
            Vector3 start = player.transform.position;

            float end = Time.time + DriveSeconds;
            while (Time.time < end)
            {
                player.SubmitCommand(new PlayerCommand { Move = new Vector2(0f, 1f) });
                yield return null;
            }

            Vector3 delta = player.transform.position - start;
            Assert.That(delta.z, Is.GreaterThan(1f));
            Assert.That(Mathf.Abs(delta.x), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator RemoteOwnedPlayer_StopsWhenCommandsGoStale()
        {
            NetworkPlayer player = Spawn(RemoteClientId, new Vector3(0f, 0.05f, 0f));
            yield return Wait(SettleSeconds);

            player.SubmitCommand(new PlayerCommand { Move = new Vector2(1f, 0f) });
            yield return Wait((float)NetworkCommandSource.DefaultStaleSeconds + 0.2f);
            Vector3 afterStale = player.transform.position;

            yield return Wait(DriveSeconds);

            Assert.That((player.transform.position - afterStale).magnitude, Is.LessThan(0.01f));
            Assert.That(player.Character.LastCommand.HasMove, Is.False);
        }

        [UnityTest]
        public IEnumerator HostOwnedPlayer_BecomesLocalPlayer()
        {
            NetworkPlayer player = Spawn(m_networkManager.LocalClientId, new Vector3(5f, 0.05f, 5f));
            yield return null;

            Assert.That(NetworkPlayer.LocalPlayer, Is.SameAs(player));
            Assert.That(player.Character.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator RemoteOwnedPlayer_IsNotLocalPlayer()
        {
            Spawn(RemoteClientId, new Vector3(5f, 0.05f, 5f));
            yield return null;

            Assert.That(NetworkPlayer.LocalPlayer, Is.Null);
        }

        private NetworkPlayer Spawn(ulong ownerClientId, Vector3 position)
        {
            m_spawned = Object.Instantiate(m_playerTemplate, position, Quaternion.identity);
            m_spawned.SetActive(true);
            var networkObject = m_spawned.GetComponent<NetworkObject>();
            var player = m_spawned.GetComponent<NetworkPlayer>();
            networkObject.SpawnWithOwnership(ownerClientId);
            // NetworkVariables only know their behaviour after spawn; writing earlier logs a warning.
            player.PlayerIndex.Value = 0;
            return player;
        }

        // Built from code instead of the asset so the test owns its configuration; mirrors the prefab minus
        // visuals and HumanCommandSource. Kept inactive so it behaves like a prefab template.
        private static GameObject CreatePlayerTemplate()
        {
            var template = new GameObject("TestNetworkPlayerTemplate");
            template.SetActive(false);

            var controller = template.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            template.AddComponent<CharacterMotor>();
            template.AddComponent<PlayerCharacter>();

            var networkObject = template.AddComponent<NetworkObject>();
            AssignGlobalObjectIdHash(networkObject, 0xD20B0001u);
            var networkTransform = template.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            template.AddComponent<NetworkPlayer>();

            return template;
        }

        // Runtime-built NetworkObjects have no GlobalObjectIdHash, and Netcode refuses to spawn a zero hash.
        // The hash is an internal serialized field that only the editor normally writes, so set it directly;
        // Netcode's own integration tests do the same.
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
