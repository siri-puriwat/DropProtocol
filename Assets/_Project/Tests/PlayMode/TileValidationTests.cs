using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// The assembler guarantees a connected grid only if every tile is walkable between all of its open
    /// edges and its anchors, so each shipped tile is baked alone and checked: gate midpoints on the
    /// mesh and mutually reachable, sites clear enough for the squad square, enemy spawns reachable.
    /// </summary>
    public class TileValidationTests
    {
        private const string MissionScene = "10_Mission_Test";
        private const float SampleRadius = 1f;
        private const float GateDepth = 1.5f;
        private const float SquadSpread = 2f;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MissionMap.PendingSeed = null;
            if (NetworkManager.Singleton != null)
                Object.Destroy(NetworkManager.Singleton.gameObject);
            yield return null;

            Scene scratch = SceneManager.CreateScene("TileTestScratch");
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scratch);
            if (previous.IsValid() && previous != scratch)
                yield return SceneManager.UnloadSceneAsync(previous);
        }

        [UnityTest]
        public IEnumerator EveryTile_ConnectsItsGatesAndAnchors()
        {
            SceneManager.LoadScene(MissionScene);
            yield return null;
            TileSet set = MissionMap.Instance.TileSet;
            Assert.That(set, Is.Not.Null);

            Scene scratch = SceneManager.CreateScene("TileProbe");
            SceneManager.SetActiveScene(scratch);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(MissionScene));

            foreach (MapTile prefab in set.Tiles)
            {
                var root = new GameObject("Probe " + prefab.name);
                var surface = root.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                MapTile tile = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
                surface.BuildNavMesh();

                TileTemplate template = tile.ToTemplate();
                float edge = tile.Size * 0.5f - GateDepth;
                var gates = new List<Vector3>();
                for (int side = 0; side < MapRules.SideCount; side++)
                {
                    if (template.Sockets[side] != MapSocket.Open)
                        continue;
                    Vector3 gate = side switch
                    {
                        MapRules.North => new Vector3(0f, 0f, edge),
                        MapRules.East => new Vector3(edge, 0f, 0f),
                        MapRules.South => new Vector3(0f, 0f, -edge),
                        _ => new Vector3(-edge, 0f, 0f),
                    };
                    AssertOnMesh(gate, prefab.name + " gate " + side);
                    gates.Add(Snap(gate));
                }

                Assert.That(gates, Is.Not.Empty, prefab.name + " has no open side");
                for (int i = 1; i < gates.Count; i++)
                    AssertConnected(gates[0], gates[i], prefab.name + " gate 0 to gate " + i);

                foreach (Vector2Int site in template.Sites)
                {
                    var centre = new Vector3(site.x, 0f, site.y);
                    AssertOnMesh(centre, prefab.name + " site " + site);
                    AssertConnected(gates[0], Snap(centre), prefab.name + " gate to site " + site);
                    foreach (Vector3 corner in SquadCorners(centre))
                        AssertOnMesh(corner, prefab.name + " squad slot near site " + site, 0.5f);
                }

                foreach (Vector2Int spawn in template.EnemySpawns)
                {
                    var point = new Vector3(spawn.x, 0f, spawn.y);
                    AssertOnMesh(point, prefab.name + " enemy spawn " + spawn);
                    AssertConnected(gates[0], Snap(point), prefab.name + " gate to enemy spawn " + spawn);
                }

                surface.RemoveData();
                Object.Destroy(root);
                yield return null;
            }
        }

        private static IEnumerable<Vector3> SquadCorners(Vector3 centre)
        {
            yield return centre + new Vector3(-SquadSpread, 0f, -SquadSpread);
            yield return centre + new Vector3(SquadSpread, 0f, -SquadSpread);
            yield return centre + new Vector3(-SquadSpread, 0f, SquadSpread);
            yield return centre + new Vector3(SquadSpread, 0f, SquadSpread);
        }

        private static void AssertOnMesh(Vector3 point, string label, float radius = SampleRadius)
        {
            Assert.That(NavMesh.SamplePosition(point, out _, radius, NavMesh.AllAreas), Is.True,
                label + " at " + point + " is off the NavMesh");
        }

        private static Vector3 Snap(Vector3 point)
        {
            return NavMesh.SamplePosition(point, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas) ? hit.position : point;
        }

        private static void AssertConnected(Vector3 from, Vector3 to, string label)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label + " has no complete path");
        }
    }
}
