using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// End-to-end check of the real mission scene: hosting from the scene keeps it, the spawner fills the
    /// squad with bots, the in-scene mission finds its relays and moves from deployment into the mission.
    /// </summary>
    public class MissionSmokeTests
    {
        private const string MissionScene = "10_Mission_Test";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            NetworkSession session = NetworkSession.Instance;
            if (session != null && session.IsInSession)
                session.Leave();
            yield return HostTestHarness.Wait(0.5f);

            if (NetworkManager.Singleton != null)
                Object.Destroy(NetworkManager.Singleton.gameObject);
            yield return null;

            // Leave the runner in an empty scene so later fixtures do not build on the menu.
            Scene scratch = SceneManager.CreateScene("MissionSmokeTestScratch");
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scratch);
            if (previous.IsValid() && previous != scratch)
                yield return SceneManager.UnloadSceneAsync(previous);
        }

        [UnityTest]
        public IEnumerator Mission_HostFillsBotsAndReachesActive()
        {
            SceneManager.LoadScene(MissionScene);
            yield return null;
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "exactly one audio listener");

            NetworkSession session = NetworkSession.Instance;
            Assert.That(session, Is.Not.Null, "mission panel should have created the NetworkRoot");
            Assert.That(session.StartHost(), Is.True);
            yield return HostTestHarness.Wait(0.5f);
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "exactly one audio listener");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MissionScene), "hosting from the scene must keep it");
            Assert.That(NetworkPlayer.LocalPlayer, Is.Not.Null);
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(SessionRules.MaxPlayers));
            PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            Assert.That(spawner.BotCount, Is.EqualTo(SessionRules.MaxPlayers - 1));

            MissionDirector mission = MissionDirector.Instance;
            Assert.That(mission, Is.Not.Null);
            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Deployment));
            Assert.That(mission.RelayCount.Value, Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<CommRelay>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
            Assert.That(mission.Zone, Is.Not.Null);
            Assert.That(mission.TimeLimit.Value, Is.GreaterThan(0f));

            EnemyDirector director = Object.FindFirstObjectByType<EnemyDirector>();
            Assert.That(director.Running, Is.False);

            ProtocolController protocols = NetworkPlayer.LocalPlayer.GetComponent<ProtocolController>();
            Assert.That(protocols, Is.Not.Null, "player prefab should carry the protocol loadout");
            Assert.That(protocols.Loadout.Count, Is.EqualTo(3));
            foreach (ProtocolDefinition definition in protocols.Loadout)
                Assert.That(definition.Payload, Is.Not.Null, definition.DisplayName + " has no payload prefab");

            yield return HostTestHarness.Wait(mission.Tuning.DeploySeconds + 1f);

            Assert.That(mission.Phase.Value, Is.EqualTo(MissionPhase.Active));
            Assert.That(director.Running, Is.True);
            Assert.That(NetworkPlayer.LocalPlayer.Health.IsDowned, Is.False);
        }
    }
}
