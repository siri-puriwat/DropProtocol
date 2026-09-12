using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    /// <summary>
    /// Every seed is resolved against the shipped tile set, so a missing signature class or a tile without
    /// anchors would only fail at runtime on some seed; this pins the catalog in the editor.
    /// </summary>
    public sealed class TileSetTests
    {
        private const string TileSetPath = "Assets/_Project/Settings/Missions/TileSet.asset";

        private static TileSet Shipped()
        {
            var set = AssetDatabase.LoadAssetAtPath<TileSet>(TileSetPath);
            Assert.That(set, Is.Not.Null, TileSetPath + " is missing; run DropProtocol/Map/Rebuild Tiles");
            return set;
        }

        [Test]
        public void ShippedTileSet_CoversEverySignature()
        {
            Assert.That(MapRules.MissingSignatures(Shipped().Templates()), Is.Empty);
        }

        [Test]
        public void ShippedTiles_CarryAnchorsEdgesAndAFloor()
        {
            int size = MapTuning.Default.TileSize;
            foreach (MapTile tile in Shipped().Tiles)
            {
                Assert.That(tile, Is.Not.Null, "tile set has an empty slot");
                Assert.That(tile.Size, Is.EqualTo(size), tile.name + " size");
                Assert.That(tile.GetComponent<BoxCollider>(), Is.Not.Null, tile.name + " has no floor collider");
                TileTemplate template = tile.ToTemplate();
                Assert.That(template.Sites.Length, Is.GreaterThanOrEqualTo(1), tile.name + " has no site");
                Assert.That(template.EnemySpawns.Length, Is.GreaterThanOrEqualTo(1), tile.name + " has no enemy spawn");
                foreach (Vector2Int site in template.Sites)
                    AssertInside(site, size / 2 - 3, tile.name + " site");
                foreach (Vector2Int spawn in template.EnemySpawns)
                    AssertInside(spawn, size / 2 - 1, tile.name + " enemy spawn");
                for (int side = 0; side < MapRules.SideCount; side++)
                {
                    GameObject edge = tile.Edge(side);
                    Assert.That(edge, Is.Not.Null, tile.name + " edge " + side + " missing");
                    Assert.That(edge.transform.childCount, Is.GreaterThan(0), tile.name + " edge " + side + " has no walls");
                }
            }
        }

        [Test]
        public void ShippedTileSet_ResolvesAThousandSeeds()
        {
            TileTemplate[] catalog = Shipped().Templates();
            MapTuning tuning = MapTuning.Default;
            for (int seed = 0; seed < 1000; seed++)
            {
                MapLayout layout = MapRules.Generate(seed, tuning, catalog);
                Assert.That(layout.Relays.Length, Is.EqualTo(tuning.RelayCount));
                Assert.That(layout.EnemySpawns.Length, Is.EqualTo(tuning.EnemySpawnCount));
            }
        }

        private static void AssertInside(Vector2Int point, int limit, string label)
        {
            Assert.That(Mathf.Abs(point.x), Is.LessThanOrEqualTo(limit), label + " " + point + " too close to the edge");
            Assert.That(Mathf.Abs(point.y), Is.LessThanOrEqualTo(limit), label + " " + point + " too close to the edge");
        }
    }
}
