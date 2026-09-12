using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Reachability of the assembled mission map: for several seeds every spawn, relay and the extraction
    /// point must sit on the runtime NavMesh and be connected, because the director spawns enemies at the
    /// raw anchors and bots give up on a goal they cannot path to. Needs no host; the map assembles and
    /// builds its surface in Awake when no session is running.
    /// </summary>
    public class MissionMapTests
    {
        private const string MissionScene = "10_Mission_Test";
        private const float SampleRadius = 1f;
        // A relay console carves a hole in the mesh; what matters is that its 3 m interaction ring is reachable.
        private const float RelaySampleRadius = 2.5f;

        private static readonly int[] Seeds = { 1, 2, 3, 42, 1234 };

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MissionMap.PendingSeed = null;
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
        public IEnumerator MissionMap_AnchorsAreOnTheNavMeshAndConnected([ValueSource(nameof(Seeds))] int seed)
        {
            MissionMap.PendingSeed = seed;
            SceneManager.LoadScene(MissionScene);
            yield return null;

            MissionMap map = MissionMap.Instance;
            Assert.That(map, Is.Not.Null, "mission scene has no MissionMap");
            Assert.That(map.Layout, Is.Not.Null, "map did not assemble in Awake");
            Assert.That(map.Layout.Seed, Is.EqualTo(seed));

            CommRelay[] relays = Object.FindObjectsByType<CommRelay>(FindObjectsSortMode.None);
            Assert.That(relays.Length, Is.EqualTo(3));
            ExtractionZone zone = Object.FindFirstObjectByType<ExtractionZone>();
            Assert.That(zone, Is.Not.Null);
            Assert.That(zone.transform.position, Is.EqualTo(map.Layout.Extraction.position), "extraction zone not on its anchor");
            foreach (CommRelay relay in relays)
                Assert.That(Positions(map.Layout.Relays), Has.Member(relay.transform.position), relay.name + " not on a relay anchor");

            Vector3[] playerSpawns = Positions(map.PlayerSpawns);
            Vector3[] enemySpawns = Positions(map.EnemySpawns);
            Assert.That(playerSpawns.Length, Is.EqualTo(SessionRules.MaxPlayers));
            Assert.That(enemySpawns.Length, Is.EqualTo(map.Tuning.EnemySpawnCount));

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

        [UnityTest]
        public IEnumerator MissionMap_SameSeedTwice_PlacesIdenticalAnchors()
        {
            MissionMap.PendingSeed = 7;
            SceneManager.LoadScene(MissionScene);
            yield return null;
            Vector3[] first = Snapshot(MissionMap.Instance);

            MissionMap.PendingSeed = 7;
            SceneManager.LoadScene(MissionScene);
            yield return null;
            Vector3[] second = Snapshot(MissionMap.Instance);

            Assert.That(second, Is.EqualTo(first));
        }

        private static Vector3[] Snapshot(MissionMap map)
        {
            Assert.That(map, Is.Not.Null);
            var points = new List<Vector3>();
            points.AddRange(Positions(map.PlayerSpawns));
            points.AddRange(Positions(map.EnemySpawns));
            points.AddRange(Positions(map.Layout.Relays));
            points.Add(map.Layout.Extraction.position);
            foreach (Transform tile in Object.FindFirstObjectByType<MissionMap>().transform.Find("Tiles"))
                points.Add(tile.position);
            return points.ToArray();
        }

        private static Vector3[] Positions(IReadOnlyList<Transform> transforms)
        {
            var points = new Vector3[transforms.Count];
            for (int i = 0; i < points.Length; i++)
                points[i] = transforms[i].position;
            return points;
        }

        private static Vector3[] Positions(Pose[] poses)
        {
            var points = new Vector3[poses.Length];
            for (int i = 0; i < points.Length; i++)
                points[i] = poses[i].position;
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
