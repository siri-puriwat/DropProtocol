using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Reachability of the real mission map: every spawn, relay and the extraction point must sit on the
    /// baked NavMesh and be connected, because the director spawns enemies at the raw transforms and bots
    /// give up on a goal they cannot path to. Needs no host; the baked surface loads with the scene.
    /// </summary>
    public class MissionMapTests
    {
        private const string MissionScene = "10_Mission_Test";
        private const float SampleRadius = 1f;
        // A relay post is a hole in the mesh; what matters is that its 3 m interaction ring is reachable.
        private const float RelaySampleRadius = 2.5f;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (NetworkManager.Singleton != null)
                Object.Destroy(NetworkManager.Singleton.gameObject);
            yield return null;

            Scene scratch = SceneManager.CreateScene("MapTestScratch");
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scratch);
            if (previous.IsValid() && previous != scratch)
                yield return SceneManager.UnloadSceneAsync(previous);
        }

        [UnityTest]
        public IEnumerator MissionMap_AnchorsAreOnTheNavMeshAndConnected()
        {
            SceneManager.LoadScene(MissionScene);
            yield return null;

            CommRelay[] relays = Object.FindObjectsByType<CommRelay>(FindObjectsSortMode.None);
            Assert.That(relays.Length, Is.EqualTo(3));
            ExtractionZone zone = Object.FindFirstObjectByType<ExtractionZone>();
            Assert.That(zone, Is.Not.Null);

            Vector3[] playerSpawns = Anchors("SpawnPoint_", 4);
            Vector3[] enemySpawns = Anchors("EnemySpawn_", 8);

            foreach (Vector3 point in playerSpawns)
                AssertOnMesh(point, "player spawn");
            foreach (Vector3 point in enemySpawns)
                AssertOnMesh(point, "enemy spawn");
            foreach (CommRelay relay in relays)
                AssertOnMesh(relay.transform.position, relay.name, RelaySampleRadius);
            AssertOnMesh(zone.transform.position, "extraction");

            Vector3 origin = Snap(zone.transform.position, SampleRadius);
            foreach (Vector3 point in enemySpawns)
                AssertConnected(Snap(point, SampleRadius), origin, "enemy spawn to extraction");
            foreach (CommRelay relay in relays)
                AssertConnected(Snap(playerSpawns[0], SampleRadius), Snap(relay.transform.position, RelaySampleRadius), "squad to " + relay.name);
            AssertConnected(Snap(playerSpawns[0], SampleRadius), origin, "squad to extraction");
        }

        private static Vector3[] Anchors(string prefix, int count)
        {
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                GameObject anchor = GameObject.Find(prefix + i);
                Assert.That(anchor, Is.Not.Null, prefix + i + " missing");
                points[i] = anchor.transform.position;
            }

            return points;
        }

        private static void AssertOnMesh(Vector3 point, string label, float radius = SampleRadius)
        {
            Assert.That(NavMesh.SamplePosition(point, out _, radius, NavMesh.AllAreas), Is.True,
                label + " at " + point + " is off the NavMesh");
        }

        private static Vector3 Snap(Vector3 point, float radius)
        {
            return NavMesh.SamplePosition(point, out NavMeshHit hit, radius, NavMesh.AllAreas) ? hit.position : point;
        }

        private static void AssertConnected(Vector3 from, Vector3 to, string label)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label + " has no complete path");
        }
    }
}
