using UnityEngine;

namespace DropProtocol.Editor
{
internal readonly struct CoverItem
{
    public CoverItem(string prefab, int x, int z, int quarterTurns)
    {
        Prefab = prefab;
        X = x;
        Z = z;
        QuarterTurns = quarterTurns;
    }

    public readonly string Prefab;
    public readonly int X;
    public readonly int Z;
    public readonly int QuarterTurns;
}

internal sealed class TileRecipe
{
    public string Name;
    public MapSocket[] Sockets;
    public CoverItem[] Cover;
    public Vector2Int[] Sites;
    public Vector2Int[] EnemySpawns;
}

/// <summary>
///     Grey-box tiles in tile-local whole metres, x east and z north, origin at the tile centre; sockets
///     are north, east, south, west of the unrotated prefab. Authoring rules: cover meant to block fire is
///     at least 1 m tall, routes are at least 2 m wide, the middle 12 m of an open edge stays clear 3 m
///     deep, a site is a clear 6 m pad, and every tile has at least one site and one enemy spawn.
/// </summary>
internal static class TileRecipes
{
    private const int N = MapRules.North;
    private const int E = MapRules.East;
    private const int S = MapRules.South;
    private const int W = MapRules.West;

    public static TileRecipe[] All => new[]
    {
        Recipe("DeadEnd_Pad", Open(N),
            Cover(C("ContainerTall", -7, -6), C("ContainerTall", 7, -6), C("Pillar", -6, 3, 1), C("Pillar", 6, 3, 1),
                C("Rail", 0, -8)),
            Points(P(0, -2)), Points(P(-8, -8), P(8, -8))),
        Recipe("DeadEnd_Storage", Open(N),
            Cover(C("ContainerWide", -6, -4), C("Container", -4, -4), C("ContainerTall", -6, -6), C("Skip", 6, -5, 1),
                C("Table", 7, 3), C("ComputerWide", -6, 4, 1), C("DisplayWall", -9, 1, 1)),
            Points(P(0, -3)), Points(P(-8, -8), P(8, 7))),
        Recipe("Corridor_Pillars", Open(N, S),
            Cover(C("Pillar", -5, -6), C("Pillar", -5, 0), C("Pillar", -5, 6), C("Pillar", 5, -6), C("Pillar", 5, 0),
                C("Pillar", 5, 6)),
            Points(P(0, 0)), Points(P(-8, 0), P(8, 0))),
        Recipe("Corridor_Crates", Open(N, S),
            Cover(C("ContainerTall", -7, -5), C("Container", -5, -5), C("ContainerWide", 6, -2), C("ContainerTall", 7, -4),
                C("Skip", -6, 4), C("Container", 5, 5), C("Table", 8, -8, 1)),
            Points(P(0, 1)), Points(P(-8, 8), P(8, 0))),
        Recipe("Corner_Open", Open(N, E),
            Cover(C("Pillar", -6, -6), C("ContainerTall", -7, 2), C("Container", 2, -7), C("Rail", 5, -6), C("Rail", -6, 5, 1)),
            Points(P(0, 0)), Points(P(-8, -8), P(8, -8))),
        Recipe("Corner_Console", Open(N, E),
            Cover(C("Wall_Station", -5, -3), C("Wall_Station", -3, -5, 1), C("ComputerWide", -7, -7), C("DisplayWall", -9, -5, 1),
                C("ContainerTall", 5, -7), C("Pillar", -6, 6)),
            Points(P(3, 3)), Points(P(-8, 8), P(7, -8))),
        Recipe("T_Plaza", Open(N, E, S),
            Cover(C("Pillar", -6, 5, 1), C("Pillar", -6, -5, 1), C("ContainerWide", 5, 4), C("Container", 5, -4), C("Rail", -8, 0, 1)),
            Points(P(-2, 0)), Points(P(-8, 8), P(-8, -8))),
        Recipe("T_Blocks", Open(N, E, S),
            Cover(C("ContainerTall", -4, 6), C("ContainerTall", -4, 4), C("Skip", 4, -5, 1), C("Container", 6, -3),
                C("Table", -7, -6), C("ContainerFlat", -7, 0, 1)),
            Points(P(0, 0)), Points(P(-8, 8), P(8, 8))),
        Recipe("Cross_Hub", Open(N, E, S, W),
            Cover(C("Pillar", -5, 5), C("Pillar", 5, 5), C("Pillar", -5, -5), C("Pillar", 5, -5), C("ContainerTall", -7, 7),
                C("Skip", 7, -8)),
            Points(P(0, 0)), Points(P(8, 8), P(-8, -8))),
        Recipe("Cross_Yard", Open(N, E, S, W),
            Cover(C("Wall_Station", -3, 0), C("Wall_Station", 3, 0), C("ContainerWide", 0, -4), C("Rail", -7, -7),
                C("Rail", 7, 7, 1), C("Table", -8, -7)),
            Points(P(0, 4)), Points(P(-8, 8), P(8, -8))),
    };

    private static MapSocket[] Open(params int[] sides)
    {
        var sockets = new MapSocket[MapRules.SideCount];
        foreach (int side in sides)
        {
            sockets[side] = MapSocket.Open;
        }

        return sockets;
    }

    private static CoverItem C(string prefab, int x, int z, int quarterTurns = 0) => new(prefab, x, z, quarterTurns);
    private static CoverItem[] Cover(params CoverItem[] items) => items;
    private static Vector2Int P(int x, int z) => new(x, z);
    private static Vector2Int[] Points(params Vector2Int[] points) => points;

    private static TileRecipe Recipe(string name, MapSocket[] sockets, CoverItem[] cover, Vector2Int[] sites, Vector2Int[] enemySpawns)
    {
        return new TileRecipe { Name = name, Sockets = sockets, Cover = cover, Sites = sites, EnemySpawns = enemySpawns };
    }
}
}
