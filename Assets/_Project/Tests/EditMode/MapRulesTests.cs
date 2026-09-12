using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
    /// <summary>
    /// The layout rules are what every peer runs from the replicated seed, so determinism and
    /// connectivity are proven here over many seeds instead of in a networked PlayMode run.
    /// </summary>
    public class MapRulesTests
    {
        private const int SeedSweep = 1000;

        private static readonly Vector2Int[] Sites = { new(0, 0), new(6, 6) };
        private static readonly Vector2Int[] Enemies = { new(-7, -7), new(7, -7) };

        private static TileTemplate Tile(string name, params int[] openSides)
        {
            var sockets = new MapSocket[MapRules.SideCount];
            foreach (int side in openSides)
                sockets[side] = MapSocket.Open;
            return new TileTemplate(name, sockets, Sites, Enemies);
        }

        // One tile per signature class up to rotation; a real set adds variants but no new classes.
        private static List<TileTemplate> StubCatalog()
        {
            return new List<TileTemplate>
            {
                Tile("DeadEnd", MapRules.North),
                Tile("Corridor", MapRules.North, MapRules.South),
                Tile("Corner", MapRules.North, MapRules.East),
                Tile("Tee", MapRules.North, MapRules.East, MapRules.South),
                Tile("Cross", MapRules.North, MapRules.East, MapRules.South, MapRules.West),
            };
        }

        [Test]
        public void Rotate_FourTurns_IsIdentity()
        {
            MapSocket[] sockets = Tile("Corner", MapRules.North, MapRules.East).Sockets;
            Assert.That(MapRules.Rotate(sockets, 4), Is.EqualTo(sockets));
            Assert.That(MapRules.Rotate(MapRules.Rotate(sockets, 1), 3), Is.EqualTo(sockets));
        }

        [Test]
        public void Rotate_OneTurn_MovesNorthToEast()
        {
            MapSocket[] rotated = MapRules.Rotate(Tile("DeadEnd", MapRules.North).Sockets, 1);
            Assert.That(rotated[MapRules.East], Is.EqualTo(MapSocket.Open));
            Assert.That(rotated[MapRules.North], Is.EqualTo(MapSocket.Wall));
            Assert.That(MapRules.Turn(MapRules.West, 1), Is.EqualTo(MapRules.North));
            Assert.That(MapRules.Turn(MapRules.North, -1), Is.EqualTo(MapRules.West));
        }

        [Test]
        public void RotateLocal_MatchesSocketRotation()
        {
            Assert.That(MapRules.RotateLocal(new Vector2Int(0, 1), 1), Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(MapRules.RotateLocal(new Vector2Int(3, 5), 4), Is.EqualTo(new Vector2Int(3, 5)));
            Assert.That(MapRules.RotateLocal(new Vector2Int(3, 5), 2), Is.EqualTo(new Vector2Int(-3, -5)));
        }

        [Test]
        public void MissingSignatures_StubCatalogCoversEverything()
        {
            Assert.That(MapRules.MissingSignatures(StubCatalog()), Is.Empty);
        }

        [Test]
        public void MissingSignatures_ReportsAnAbsentClass()
        {
            List<TileTemplate> catalog = StubCatalog();
            catalog.RemoveAt(0);
            List<int> missing = MapRules.MissingSignatures(catalog);
            Assert.That(missing, Is.EquivalentTo(new[] { 1, 2, 4, 8 }));
        }

        [Test]
        public void Generate_WithoutDeadEnds_ThrowsForSomeSeed()
        {
            List<TileTemplate> catalog = StubCatalog();
            catalog.RemoveAt(0);
            Assert.Throws<InvalidOperationException>(() =>
            {
                for (int seed = 0; seed < SeedSweep; seed++)
                    MapRules.Generate(seed, MapTuning.Default, catalog);
            });
        }

        [Test]
        public void Generate_SameSeed_ProducesIdenticalLayout()
        {
            List<TileTemplate> catalog = StubCatalog();
            MapLayout a = MapRules.Generate(42, MapTuning.Default, catalog);
            MapLayout b = MapRules.Generate(42, MapTuning.Default, catalog);

            Assert.That(b.TileIndices, Is.EqualTo(a.TileIndices));
            Assert.That(b.QuarterTurns, Is.EqualTo(a.QuarterTurns));
            Assert.That(b.Sockets, Is.EqualTo(a.Sockets));
            Assert.That(b.SquadCell, Is.EqualTo(a.SquadCell));
            Assert.That(b.ExtractionCell, Is.EqualTo(a.ExtractionCell));
            Assert.That(b.RelayCells, Is.EqualTo(a.RelayCells));
            Assert.That(b.EnemySpawnCells, Is.EqualTo(a.EnemySpawnCells));
            Assert.That(Positions(b.SquadSpawns), Is.EqualTo(Positions(a.SquadSpawns)));
            Assert.That(Positions(b.Relays), Is.EqualTo(Positions(a.Relays)));
            Assert.That(Positions(b.EnemySpawns), Is.EqualTo(Positions(a.EnemySpawns)));
            Assert.That(b.Extraction.position, Is.EqualTo(a.Extraction.position));
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentLayouts()
        {
            List<TileTemplate> catalog = StubCatalog();
            MapLayout a = MapRules.Generate(1, MapTuning.Default, catalog);
            MapLayout b = MapRules.Generate(2, MapTuning.Default, catalog);
            Assert.That(b.Sockets, Is.Not.EqualTo(a.Sockets));
        }

        [Test]
        public void Generate_ManySeeds_AreConnectedAndConsistent()
        {
            List<TileTemplate> catalog = StubCatalog();
            MapTuning tuning = MapTuning.Default;
            for (int seed = 0; seed < SeedSweep; seed++)
            {
                MapLayout layout = MapRules.Generate(seed, tuning, catalog);
                int[] fromSquad = MapRules.Distances(layout, layout.SquadCell);
                for (int cell = 0; cell < layout.CellCount; cell++)
                {
                    Assert.That(fromSquad[cell], Is.GreaterThanOrEqualTo(0), $"seed {seed}: cell {cell} unreachable");
                    Assert.That(MapRules.Signature(layout, cell), Is.Not.Zero, $"seed {seed}: cell {cell} is sealed");
                    Assert.That(MapRules.Fits(catalog[layout.TileIndices[cell]], layout.QuarterTurns[cell], layout, cell),
                        $"seed {seed}: cell {cell} tile does not fit its sockets");
                    for (int side = 0; side < MapRules.SideCount; side++)
                    {
                        int other = layout.Neighbour(cell, side);
                        if (other < 0)
                            Assert.That(layout.Socket(cell, side), Is.EqualTo(MapSocket.Wall), $"seed {seed}: open border");
                        else
                            Assert.That(layout.Socket(other, MapRules.Opposite(side)), Is.EqualTo(layout.Socket(cell, side)),
                                $"seed {seed}: sockets disagree between {cell} and {other}");
                    }
                }

                Assert.That(layout.IsBorder(layout.SquadCell), $"seed {seed}: squad not on the border");
                Assert.That(layout.ExtractionCell, Is.Not.EqualTo(layout.SquadCell));
                Assert.That(layout.RelayCells.Length, Is.EqualTo(tuning.RelayCount));
                Assert.That(layout.RelayCells, Is.Unique);
                Assert.That(layout.RelayCells, Has.None.EqualTo(layout.SquadCell));
                Assert.That(layout.RelayCells, Has.None.EqualTo(layout.ExtractionCell));
                Assert.That(layout.EnemySpawnCells.Length, Is.EqualTo(tuning.EnemySpawnCount));
                Assert.That(layout.EnemySpawnCells, Is.Unique);
                Assert.That(layout.EnemySpawnCells, Has.None.EqualTo(layout.SquadCell));
                Assert.That(layout.SquadSpawns.Length, Is.EqualTo(SessionRules.MaxPlayers));
                AssertInsideCell(layout, layout.SquadCell, layout.SquadSpawns[0].position, seed);
                AssertInsideCell(layout, layout.ExtractionCell, layout.Extraction.position, seed);
                for (int i = 0; i < layout.Relays.Length; i++)
                    AssertInsideCell(layout, layout.RelayCells[i], layout.Relays[i].position, seed);
                for (int i = 0; i < layout.EnemySpawns.Length; i++)
                    AssertInsideCell(layout, layout.EnemySpawnCells[i], layout.EnemySpawns[i].position, seed);
            }
        }

        [Test]
        public void Generate_ManySeeds_KeepObjectivesApart()
        {
            List<TileTemplate> catalog = StubCatalog();
            for (int seed = 0; seed < SeedSweep; seed++)
            {
                MapLayout layout = MapRules.Generate(seed, MapTuning.Default, catalog);
                int[] fromSquad = MapRules.Distances(layout, layout.SquadCell);
                int[] fromExtraction = MapRules.Distances(layout, layout.ExtractionCell);
                foreach (int relay in layout.RelayCells)
                {
                    Assert.That(fromSquad[relay], Is.GreaterThanOrEqualTo(1), $"seed {seed}: relay next to the drop");
                    Assert.That(fromExtraction[relay], Is.GreaterThanOrEqualTo(1), $"seed {seed}: relay on the extraction");
                }

                int farthest = 0;
                foreach (int distance in fromSquad)
                    farthest = Math.Max(farthest, distance);
                Assert.That(fromSquad[layout.ExtractionCell], Is.EqualTo(farthest), $"seed {seed}: extraction is not the farthest cell");
            }
        }

        [Test]
        public void CellOrigin_CentresTheGridOnTheOrigin()
        {
            var layout = new MapLayout(0, MapTuning.Default);
            Assert.That(layout.CellOrigin(layout.Index(0, 0)), Is.EqualTo(new Vector3(-30f, 0f, -30f)));
            Assert.That(layout.CellOrigin(layout.Index(3, 3)), Is.EqualTo(new Vector3(30f, 0f, 30f)));
            Assert.That(layout.IsBorderSide(layout.Index(0, 0), MapRules.West), Is.True);
            Assert.That(layout.IsBorderSide(layout.Index(0, 0), MapRules.North), Is.False);
        }

        [Test]
        public void FacingCentre_PicksTheDominantAxis()
        {
            Assert.That(MapRules.FacingCentre(new Vector3(-30f, 0f, 5f)), Is.EqualTo(MapRules.East));
            Assert.That(MapRules.FacingCentre(new Vector3(5f, 0f, 30f)), Is.EqualTo(MapRules.South));
        }

        private static void AssertInsideCell(MapLayout layout, int cell, Vector3 position, int seed)
        {
            Vector3 origin = layout.CellOrigin(cell);
            float half = layout.TileSize * 0.5f;
            Assert.That(Mathf.Abs(position.x - origin.x), Is.LessThanOrEqualTo(half), $"seed {seed}: anchor outside cell {cell}");
            Assert.That(Mathf.Abs(position.z - origin.z), Is.LessThanOrEqualTo(half), $"seed {seed}: anchor outside cell {cell}");
        }

        private static Vector3[] Positions(Pose[] poses)
        {
            var positions = new Vector3[poses.Length];
            for (int i = 0; i < poses.Length; i++)
                positions[i] = poses[i].position;
            return positions;
        }
    }
}
