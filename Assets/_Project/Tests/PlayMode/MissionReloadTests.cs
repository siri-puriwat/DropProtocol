using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// The debug panel's "New map" reloads the mission scene through Netcode: the same scene comes back
    /// with a fresh map, a fresh mission and a respawned squad, while the session itself stays up.
    /// </summary>
    public class MissionReloadTests
    {
        private const string MissionScene = "10_Mission_Test";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MissionMap.PendingSeed = null;
            NetworkSession session = NetworkSession.Instance;
            if (session != null && session.IsInSession)
                session.Leave();
            yield return HostTestHarness.Wait(0.5f);

            if (NetworkManager.Singleton != null)
                Object.Destroy(NetworkManager.Singleton.gameObject);
            yield return null;

            Scene scratch = SceneManager.CreateScene("MissionReloadScratch");
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scratch);
            if (previous.IsValid() && previous != scratch)
                yield return SceneManager.UnloadSceneAsync(previous);
        }

        [UnityTest]
        public IEnumerator ReloadGameplayScene_BringsANewMapAndRespawnsTheSquad()
        {
            MissionMap.PendingSeed = 11;
            SceneManager.LoadScene(MissionScene);
            yield return null;

            NetworkSession session = NetworkSession.Instance;
            Assert.That(session.StartHost(), Is.True);
            yield return HostTestHarness.Wait(0.5f);
            MissionMap first = MissionMap.Instance;
            Assert.That(first.Seed.Value, Is.EqualTo(11), "host should publish the seed it assembled");
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(SessionRules.MaxPlayers));

            MissionMap.PendingSeed = 12;
            Assert.That(session.ReloadGameplayScene(), Is.True);
            yield return HostTestHarness.Wait(1.5f);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MissionScene));
            Assert.That(session.IsInSession, Is.True, "the session must survive the reload");
            MissionMap second = MissionMap.Instance;
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Layout, Is.Not.Null);
            Assert.That(second.Seed.Value, Is.EqualTo(12));
            Assert.That(NetworkPlayer.LocalPlayer, Is.Not.Null, "host player should respawn");
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(SessionRules.MaxPlayers), "bots should refill the squad");
            Assert.That(MissionDirector.Instance.Phase.Value, Is.EqualTo(MissionPhase.Deployment));
            Assert.That(Object.FindObjectsByType<CommRelay>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
        }
    }
}
