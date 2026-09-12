using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// The real HUD prefab in the real mission scene: hosting binds the local player to every view and the
    /// views show the replicated values the IMGUI panels used to print.
    /// </summary>
    public class HudSmokeTests
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

            Scene scratch = SceneManager.CreateScene("HudSmokeScratch");
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scratch);
            if (previous.IsValid() && previous != scratch)
                yield return SceneManager.UnloadSceneAsync(previous);
        }

        [UnityTest]
        public IEnumerator Hud_BindsLocalPlayerAndShowsSquadAndMission()
        {
            SceneManager.LoadScene(MissionScene);
            yield return null;
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "exactly one audio listener");

            HudController hud = Object.FindFirstObjectByType<HudController>(FindObjectsInactive.Include);
            Assert.That(hud, Is.Not.Null, "mission scene should carry the Hud prefab");
            NetworkSession session = NetworkSession.Instance;
            Assert.That(session, Is.Not.Null, "session panel should have created the NetworkRoot");
            Assert.That(session.StartHost(), Is.True);
            yield return HostTestHarness.Wait(1f);
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "exactly one audio listener");

            Assert.That(hud.LocalPlayer, Is.SameAs(NetworkPlayer.LocalPlayer));

            var health = Object.FindFirstObjectByType<HealthView>(FindObjectsInactive.Include);
            Assert.That(health.Label, Is.EqualTo("100 / 100"));
            Assert.That(health.Fraction, Is.EqualTo(1f));

            var ammo = Object.FindFirstObjectByType<AmmoView>(FindObjectsInactive.Include);
            Assert.That(ammo.Label, Is.EqualTo("30 / 30"));

            var squad = Object.FindFirstObjectByType<SquadView>(FindObjectsInactive.Include);
            Assert.That(squad.VisibleRows, Is.EqualTo(SessionRules.MaxPlayers));
            int bots = 0;
            for (int i = 0; i < SessionRules.MaxPlayers; i++)
            {
                if (squad.Row(i).Name.Contains("BOT"))
                    bots++;
            }
            Assert.That(bots, Is.EqualTo(SessionRules.MaxPlayers - 1));

            var mission = Object.FindFirstObjectByType<MissionView>(FindObjectsInactive.Include);
            Assert.That(mission.PhaseLabel, Is.EqualTo(HudFormat.PhaseLabel(MissionPhase.Deployment)));

            var protocols = Object.FindFirstObjectByType<ProtocolView>(FindObjectsInactive.Include);
            Assert.That(protocols.Slot(0).Glyphs, Is.EqualTo("↓↓↑→"));

            var downed = Object.FindFirstObjectByType<DownedView>(FindObjectsInactive.Include);
            Assert.That(downed.IsShown, Is.False);
            NetworkPlayer.LocalPlayer.Health.ApplyDamage(100);
            yield return null;
            yield return null;
            Assert.That(downed.IsShown, Is.True);
            Assert.That(health.Label, Is.EqualTo("0 / 100"));
            Assert.That(health.Fraction, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator DebugTools_HiddenByDefault_ShowOnToggle()
        {
            SceneManager.LoadScene(MissionScene);
            yield return null;
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "exactly one audio listener");
            NetworkSession session = NetworkSession.Instance;
            Assert.That(session.StartHost(), Is.True);
            yield return HostTestHarness.Wait(0.5f);
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "exactly one audio listener");

            var overlay = Object.FindFirstObjectByType<NetworkDebugOverlay>(FindObjectsInactive.Include);
            Assert.That(overlay.IsShown, Is.False);
            overlay.Show(true);
            yield return null;
            Assert.That(overlay.IsShown, Is.True);
            Assert.That(overlay.Readout, Does.Contain("Host"));
            Assert.That(overlay.Readout, Does.Contain("objects"));

            var panel = Object.FindFirstObjectByType<DebugPanel>(FindObjectsInactive.Include);
            Assert.That(panel.IsShown, Is.False);
            panel.Toggle();
            yield return null;
            Assert.That(panel.IsShown, Is.True, "host should see the debug panel after toggling");
        }
    }
}
