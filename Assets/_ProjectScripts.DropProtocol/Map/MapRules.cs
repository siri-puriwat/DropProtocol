using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace DropProtocol
{
/// <summary>
///     Pure layout rules for a seeded tile map. Every decision is integer math drawn from one
///     <see cref="System.Random" />, so peers running the same seed over the same catalog get the same
///     layout; the only floats are the final poses, converted exactly from whole metres.
/// </summary>
public static class MapRules
{
    public const int SideCount = 4;
    public const int North = 0;
    public const int East = 1;
    public const int South = 2;
    public const int West = 3;

    /// <summary>A CharacterController placed exactly on the floor tunnels through it.</summary>
    public const float PlayerSpawnHeight = 0.1f;

    private static readonly Vector2Int[] SquadCorners = { new(-1, -1), new(1, -1), new(-1, 1), new(1, 1) };

    public static int Opposite(int side) => (side + 2) % SideCount;

    /// <summary>World side faced by local side <paramref name="side" /> after clockwise quarter turns.</summary>
    public static int Turn(int side, int quarterTurns) => (side + Normalize(quarterTurns)) % SideCount;

    public static MapSocket[] Rotate(MapSocket[] sockets, int quarterTurns)
    {
        var rotated = new MapSocket[SideCount];
        for (int side = 0; side < SideCount; side++)
        {
            rotated[Turn(side, quarterTurns)] = sockets[side];
        }

        return rotated;
    }

    /// <summary>Rotates a tile-local point (x east, y north) clockwise seen from above.</summary>
    public static Vector2Int RotateLocal(Vector2Int point, int quarterTurns)
    {
        int turns = Normalize(quarterTurns);
        for (int i = 0; i < turns; i++)
        {
            point = new Vector2Int(point.y, -point.x);
        }

        return point;
    }

    /// <summary>Bit per open side: north 1, east 2, south 4, west 8.</summary>
    public static int Signature(MapSocket[] sockets)
    {
        int signature = 0;
        for (int side = 0; side < SideCount; side++)
        {
            if (sockets[side] == MapSocket.Open)
            {
                signature |= 1 << side;
            }
        }

        return signature;
    }

    public static int Signature(MapLayout layout, int cell)
    {
        int signature = 0;
        for (int side = 0; side < SideCount; side++)
        {
            if (layout.Socket(cell, side) == MapSocket.Open)
            {
                signature |= 1 << side;
            }
        }

        return signature;
    }

    public static bool Fits(TileTemplate tile, int quarterTurns, MapLayout layout, int cell)
    {
        for (int side = 0; side < SideCount; side++)
        {
            if (tile.Sockets[side] != layout.Socket(cell, Turn(side, quarterTurns)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Signatures no catalog tile fills in any rotation. All-walls is left out: it never occurs.</summary>
    public static List<int> MissingSignatures(IReadOnlyList<TileTemplate> catalog)
    {
        var missing = new List<int>();
        for (int signature = 1; signature < 1 << SideCount; signature++)
        {
            if (!Covers(catalog, signature))
            {
                missing.Add(signature);
            }
        }

        return missing;
    }

    public static MapLayout Generate(int seed, MapTuning tuning, IReadOnlyList<TileTemplate> catalog)
    {
        var rng = new Random(seed);
        var layout = new MapLayout(seed, tuning);
        CarveEdges(layout, tuning.ExtraOpenChance, rng);
        PickTiles(layout, catalog, rng);
        PlaceObjectives(layout, tuning, rng);
        PlaceEnemySpawns(layout, tuning, rng);
        ResolvePoses(layout, tuning, catalog, rng);
        return layout;
    }

    /// <summary>Walking distance in cells from <paramref name="start" /> through open sides; -1 when unreachable.</summary>
    public static int[] Distances(MapLayout layout, int start)
    {
        var distance = new int[layout.CellCount];
        for (int i = 0; i < distance.Length; i++)
        {
            distance[i] = -1;
        }

        var queue = new Queue<int>();
        distance[start] = 0;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue();
            for (int side = 0; side < SideCount; side++)
            {
                int next = layout.Neighbour(cell, side);
                if (next >= 0 && distance[next] < 0 && layout.Socket(cell, side) == MapSocket.Open)
                {
                    distance[next] = distance[cell] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        return distance;
    }

    /// <summary>Quarter turns that face the map centre along the dominant axis.</summary>
    public static int FacingCentre(Vector3 position)
    {
        if (Mathf.Abs(position.x) >= Mathf.Abs(position.z))
        {
            return position.x > 0f ? West : East;
        }

        return position.z > 0f ? South : North;
    }

    public static Quaternion Rotation(int quarterTurns) => Quaternion.Euler(0f, 90f * Normalize(quarterTurns), 0f);

    private static bool Covers(IReadOnlyList<TileTemplate> catalog, int signature)
    {
        for (int tile = 0; tile < catalog.Count; tile++)
        {
            for (int q = 0; q < SideCount; q++)
            {
                if (Signature(Rotate(catalog[tile].Sockets, q)) == signature)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // A random spanning tree keeps every cell reachable; extra open edges add the loops.
    private static void CarveEdges(MapLayout layout, float extraOpenChance, Random rng)
    {
        var edges = new List<int>();
        for (int cell = 0; cell < layout.CellCount; cell++)
        {
            if (layout.Neighbour(cell, East) >= 0)
            {
                edges.Add(cell * SideCount + East);
            }

            if (layout.Neighbour(cell, North) >= 0)
            {
                edges.Add(cell * SideCount + North);
            }
        }

        Shuffle(edges, rng);
        var parent = new int[layout.CellCount];
        for (int i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        foreach (int edge in edges)
        {
            int cell = edge / SideCount;
            int side = edge % SideCount;
            int other = layout.Neighbour(cell, side);
            int rootA = Find(parent, cell);
            int rootB = Find(parent, other);
            bool open = rootA != rootB || rng.NextDouble() < extraOpenChance;
            if (rootA != rootB)
            {
                parent[rootB] = rootA;
            }

            if (open)
            {
                layout.Sockets[cell * SideCount + side] = MapSocket.Open;
                layout.Sockets[other * SideCount + Opposite(side)] = MapSocket.Open;
            }
        }
    }

    private static void PickTiles(MapLayout layout, IReadOnlyList<TileTemplate> catalog, Random rng)
    {
        var candidates = new List<int>();
        for (int cell = 0; cell < layout.CellCount; cell++)
        {
            candidates.Clear();
            for (int tile = 0; tile < catalog.Count; tile++)
            {
                for (int q = 0; q < SideCount; q++)
                {
                    if (Fits(catalog[tile], q, layout, cell))
                    {
                        candidates.Add(tile * SideCount + q);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No tile in the catalog fits signature {Signature(layout, cell)} at cell {cell}.");
            }

            int pick = candidates[rng.Next(candidates.Count)];
            layout.TileIndices[cell] = pick / SideCount;
            layout.QuarterTurns[cell] = pick % SideCount;
        }
    }

    private static void PlaceObjectives(MapLayout layout, MapTuning tuning, Random rng)
    {
        var border = new List<int>();
        for (int cell = 0; cell < layout.CellCount; cell++)
        {
            if (layout.IsBorder(cell))
            {
                border.Add(cell);
            }
        }

        layout.SquadCell = border[rng.Next(border.Count)];
        int[] fromSquad = Distances(layout, layout.SquadCell);

        int farthest = 0;
        foreach (int distance in fromSquad)
        {
            farthest = Math.Max(farthest, distance);
        }

        var far = new List<int>();
        for (int cell = 0; cell < layout.CellCount; cell++)
        {
            if (fromSquad[cell] == farthest)
            {
                far.Add(cell);
            }
        }

        layout.ExtractionCell = far[rng.Next(far.Count)];
        int[] fromExtraction = Distances(layout, layout.ExtractionCell);

        var ordered = new List<int>();
        for (int cell = 0; cell < layout.CellCount; cell++)
        {
            if (cell != layout.SquadCell && cell != layout.ExtractionCell)
            {
                ordered.Add(cell);
            }
        }

        Shuffle(ordered, rng);
        var chosen = new List<int>(tuning.RelayCount);
        var table = new int[layout.CellCount][];
        for (int min = tuning.MinObjectiveDistance; min >= 0; min--)
        {
            chosen.Clear();
            if (TryPickRelays(layout, ordered, fromSquad, fromExtraction, table, min, chosen, 0, tuning.RelayCount))
            {
                break;
            }
        }

        if (chosen.Count < tuning.RelayCount)
        {
            throw new InvalidOperationException("Not enough cells for the relays.");
        }

        layout.RelayCells = chosen.ToArray();
    }

    private static bool TryPickRelays(MapLayout layout, List<int> ordered, int[] fromSquad, int[] fromExtraction,
        int[][] table, int min, List<int> chosen, int start, int need)
    {
        if (need == 0)
        {
            return true;
        }

        for (int i = start; i < ordered.Count; i++)
        {
            int cell = ordered[i];
            if (fromSquad[cell] < min || fromExtraction[cell] < min || !IsApart(layout, table, chosen, cell, min))
            {
                continue;
            }

            chosen.Add(cell);
            if (TryPickRelays(layout, ordered, fromSquad, fromExtraction, table, min, chosen, i + 1, need - 1))
            {
                return true;
            }

            chosen.RemoveAt(chosen.Count - 1);
        }

        return false;
    }

    private static bool IsApart(MapLayout layout, int[][] table, List<int> chosen, int cell, int min)
    {
        foreach (int other in chosen)
        {
            table[other] ??= Distances(layout, other);
            if (table[other][cell] < min)
            {
                return false;
            }
        }

        return true;
    }

    private static void PlaceEnemySpawns(MapLayout layout, MapTuning tuning, Random rng)
    {
        int[] fromSquad = Distances(layout, layout.SquadCell);
        var preferred = new List<int>();
        var inner = new List<int>();
        var close = new List<int>();
        for (int cell = 0; cell < layout.CellCount; cell++)
        {
            if (cell == layout.SquadCell)
            {
                continue;
            }

            if (fromSquad[cell] < tuning.MinEnemyDistance)
            {
                close.Add(cell);
            }
            else if (layout.IsBorder(cell))
            {
                preferred.Add(cell);
            }
            else
            {
                inner.Add(cell);
            }
        }

        Shuffle(preferred, rng);
        Shuffle(inner, rng);
        Shuffle(close, rng);
        preferred.AddRange(inner);
        preferred.AddRange(close);

        var cells = new int[tuning.EnemySpawnCount];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = preferred[i % preferred.Count];
        }

        layout.EnemySpawnCells = cells;
    }

    private static void ResolvePoses(MapLayout layout, MapTuning tuning, IReadOnlyList<TileTemplate> catalog, Random rng)
    {
        Vector3 drop = Anchor(layout, catalog, layout.SquadCell, true, rng);
        Quaternion facing = Rotation(FacingCentre(drop));
        var squad = new Pose[SessionRules.MaxPlayers];
        for (int i = 0; i < squad.Length; i++)
        {
            Vector2Int corner = SquadCorners[i % SquadCorners.Length];
            var offset = new Vector3(corner.x * tuning.SquadSpread, PlayerSpawnHeight, corner.y * tuning.SquadSpread);
            squad[i] = new Pose(drop + offset, facing);
        }

        layout.SquadSpawns = squad;
        layout.Extraction = new Pose(Anchor(layout, catalog, layout.ExtractionCell, true, rng),
            Rotation(layout.QuarterTurns[layout.ExtractionCell]));

        var relays = new Pose[layout.RelayCells.Length];
        for (int i = 0; i < relays.Length; i++)
        {
            int cell = layout.RelayCells[i];
            relays[i] = new Pose(Anchor(layout, catalog, cell, true, rng), Rotation(layout.QuarterTurns[cell]));
        }

        layout.Relays = relays;

        var enemies = new Pose[layout.EnemySpawnCells.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            Vector3 point = Anchor(layout, catalog, layout.EnemySpawnCells[i], false, rng);
            enemies[i] = new Pose(point, Rotation(FacingCentre(point)));
        }

        layout.EnemySpawns = enemies;
    }

    private static Vector3 Anchor(MapLayout layout, IReadOnlyList<TileTemplate> catalog, int cell, bool site, Random rng)
    {
        TileTemplate tile = catalog[layout.TileIndices[cell]];
        Vector2Int[] anchors = site || tile.EnemySpawns.Length == 0 ? tile.Sites : tile.EnemySpawns;
        Vector2Int local = anchors.Length > 0 ? anchors[rng.Next(anchors.Length)] : Vector2Int.zero;
        Vector2Int rotated = RotateLocal(local, layout.QuarterTurns[cell]);
        return layout.CellOrigin(cell) + new Vector3(rotated.x, 0f, rotated.y);
    }

    private static void Shuffle(List<int> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static int Find(int[] parent, int cell)
    {
        while (parent[cell] != cell)
        {
            parent[cell] = parent[parent[cell]];
            cell = parent[cell];
        }

        return cell;
    }

    private static int Normalize(int quarterTurns) => (quarterTurns % SideCount + SideCount) % SideCount;
}
}
