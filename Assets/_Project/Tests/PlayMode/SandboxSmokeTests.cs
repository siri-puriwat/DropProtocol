using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// End-to-end check of the real sandbox scene: the NetworkRoot prefab hosts, PlayerSpawner places
    /// the host player, and the in-scene director spawns the real enemy prefabs, which must reach
    /// and hurt the player over the baked NavMesh.
    /// </summary>
    public class SandboxSmokeTests
    {
        private const string SandboxScene = "90_Sandbox";
        private const float EngageSeconds = 10f;
        private const float BotKillSeconds = 6f;

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
            Scene scratch = SceneManager.CreateScene("SmokeTestScratch");
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scratch);
            if (previous.IsValid() && previous != scratch)
                yield return SceneManager.UnloadSceneAsync(previous);
        }

        [UnityTest]
        public IEnumerator Sandbox_HostedDirectorSpawnsPrefabsThatEngageThePlayer()
        {
            SceneManager.LoadScene(SandboxScene);
            yield return null;

            NetworkSession session = NetworkSession.Instance;
            Assert.That(session, Is.Not.Null, "sandbox panel should have created the NetworkRoot");
            Assert.That(session.StartHost(), Is.True);
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SandboxScene), "hosting from the sandbox must keep it");
            NetworkPlayer host = NetworkPlayer.LocalPlayer;
            Assert.That(host, Is.Not.Null);
            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(1));

            EnemyDirector director = Object.FindFirstObjectByType<EnemyDirector>();
            Assert.That(director, Is.Not.Null);
            Assert.That(director.Prefabs.Count, Is.EqualTo(3));
            EnemyCharacter grunt = director.Spawn(0);
            EnemyCharacter spitter = director.Spawn(1);
            EnemyCharacter brute = director.Spawn(2);
            Assert.That(grunt, Is.Not.Null);
            Assert.That(spitter, Is.Not.Null);
            Assert.That(brute, Is.Not.Null);
            Assert.That(grunt.Definition.ThreatCost, Is.EqualTo(1));
            Assert.That(spitter.Definition.ThreatCost, Is.EqualTo(3));
            Assert.That(brute.Definition.ThreatCost, Is.EqualTo(6));
            Assert.That(brute.GetComponent<Health>().Current.Value, Is.EqualTo(brute.Definition.MaxHealth));
            AssertPresentationWired(grunt.gameObject);
            AssertPresentationWired(spitter.gameObject);
            AssertPresentationWired(brute.gameObject);
            AssertPresentationWired(host.gameObject);
            director.Running = true;

            yield return HostTestHarness.Wait(EngageSeconds);

            Assert.That(EnemyCharacter.All, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(EnemyRules.FlatDistance(grunt.transform.position, host.transform.position), Is.LessThan(3f));
            Assert.That(host.Health.Current.Value, Is.LessThan(host.Health.Max));
            Assert.That(director.Budget, Is.GreaterThanOrEqualTo(0f));
        }

        // Guards the prefab wiring: a character that lost its animator or controller would fail silently.
        private static void AssertPresentationWired(GameObject character)
        {
            bool hasPresentation = character.GetComponent<CharacterPresentation>() != null
                || character.GetComponent<EnemyPresentation>() != null;
            Assert.That(hasPresentation, Is.True, $"{character.name} has no presentation component");

            Animator animator = character.GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null, $"{character.name} has no Animator");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, $"{character.name} Animator has no controller");
            Assert.That(character.GetComponent<DownedPose>(), Is.Null, $"{character.name} still carries DownedPose");
        }

        [UnityTest]
        public IEnumerator Sandbox_FilledBots_KillAGruntAndLeaveTheHostAsLocalPlayer()
        {
            SceneManager.LoadScene(SandboxScene);
            yield return null;

            NetworkSession session = NetworkSession.Instance;
            Assert.That(session.StartHost(), Is.True);
            yield return HostTestHarness.Wait(0.5f);

            NetworkPlayer host = NetworkPlayer.LocalPlayer;
            PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            Assert.That(spawner, Is.Not.Null);
            spawner.FillWithBots = true;
            yield return HostTestHarness.Wait(0.5f);

            Assert.That(NetworkPlayer.All, Has.Count.EqualTo(SessionRules.MaxPlayers));
            Assert.That(spawner.BotCount, Is.EqualTo(SessionRules.MaxPlayers - 1));
            Assert.That(NetworkPlayer.LocalPlayer, Is.SameAs(host));
            Assert.That(host.IsBot, Is.False);

            EnemyDirector director = Object.FindFirstObjectByType<EnemyDirector>();
            EnemyCharacter grunt = director.Spawn(0);
            Assert.That(grunt, Is.Not.Null);
            yield return HostTestHarness.Wait(BotKillSeconds);

            // The host never fires, so only the bots can have killed it.
            Assert.That(grunt == null || grunt.IsDead, Is.True, "bots did not kill the grunt");
            Assert.That(NetworkPlayer.LocalPlayer, Is.SameAs(host));
        }
    }
}
