using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Grid and placement knobs for <see cref="MapRules.Generate" />; one block in the inspector.</summary>
[Serializable]
public struct MapTuning
{
    [Min(2)]
    public int Columns;

    [Min(2)]
    public int Rows;

    [Tooltip("Tile edge in metres; the tile prefabs must be built for this size.")]
    [Min(4)]
    public int TileSize;

    [Tooltip("Chance that an internal edge outside the spanning tree opens too; higher means more loops.")]
    [Range(0f, 1f)]
    public float ExtraOpenChance;

    [Min(1)]
    public int RelayCount;

    [Min(1)]
    public int EnemySpawnCount;

    [Tooltip("Minimum walking distance in cells between the squad drop, each relay and the extraction.")]
    [Min(0)]
    public int MinObjectiveDistance;

    [Tooltip("Preferred walking distance in cells from the squad drop to an enemy spawn cell.")]
    [Min(0)]
    public int MinEnemyDistance;

    [Tooltip("Half-width of the square the four squad spawns form around the drop site.")]
    [Min(0f)]
    public float SquadSpread;

    public static MapTuning Default => new()
    {
        Columns = 4,
        Rows = 4,
        TileSize = 20,
        ExtraOpenChance = 0.5f,
        RelayCount = 3,
        EnemySpawnCount = 8,
        MinObjectiveDistance = 2,
        MinEnemyDistance = 2,
        SquadSpread = 2f
    };
}
}
