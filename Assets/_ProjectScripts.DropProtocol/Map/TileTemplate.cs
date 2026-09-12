using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     What the layout rules know about a tile prefab: its sockets (north, east, south, west) and its
///     anchors in tile-local whole metres, x east and y north, origin at the tile centre. Plain data so
///     the rules are testable without prefabs.
/// </summary>
public sealed class TileTemplate
{
    public TileTemplate(string name, MapSocket[] sockets, Vector2Int[] sites, Vector2Int[] enemySpawns)
    {
        if (sockets == null || sockets.Length != MapRules.SideCount)
        {
            throw new ArgumentException("A tile needs exactly four sockets.", nameof(sockets));
        }

        Name = name;
        Sockets = sockets;
        Sites = sites ?? Array.Empty<Vector2Int>();
        EnemySpawns = enemySpawns ?? Array.Empty<Vector2Int>();
    }

    public string Name { get; }
    public MapSocket[] Sockets { get; }

    /// <summary>Clear pads that can take the squad drop, a relay or the extraction.</summary>
    public Vector2Int[] Sites { get; }

    public Vector2Int[] EnemySpawns { get; }
}
}
