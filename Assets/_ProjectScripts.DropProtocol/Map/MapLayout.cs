using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Result of <see cref="MapRules.Generate" />: which tile sits in each cell at which rotation, which
///     cells host the squad drop, relays, extraction and enemy spawns, and the world poses for each.
///     Cells are row-major, column east and row north; the grid is centred on the world origin.
/// </summary>
public sealed class MapLayout
{
    private static readonly int[] ColumnStep = { 0, 1, 0, -1 };
    private static readonly int[] RowStep = { 1, 0, -1, 0 };

    public MapLayout(int seed, MapTuning tuning)
    {
        Seed = seed;
        Columns = tuning.Columns;
        Rows = tuning.Rows;
        TileSize = tuning.TileSize;
        CellCount = Columns * Rows;
        Sockets = new MapSocket[CellCount * MapRules.SideCount];
        TileIndices = new int[CellCount];
        QuarterTurns = new int[CellCount];
    }

    public int Seed { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int TileSize { get; }
    public int CellCount { get; }

    /// <summary>Socket per cell and side, indexed cell * 4 + side.</summary>
    public MapSocket[] Sockets { get; }

    public int[] TileIndices { get; }
    public int[] QuarterTurns { get; }

    public int SquadCell { get; set; }
    public int ExtractionCell { get; set; }
    public int[] RelayCells { get; set; }
    public int[] EnemySpawnCells { get; set; }

    public Pose[] SquadSpawns { get; set; }
    public Pose Extraction { get; set; }
    public Pose[] Relays { get; set; }
    public Pose[] EnemySpawns { get; set; }

    public MapSocket Socket(int cell, int side) => Sockets[cell * MapRules.SideCount + side];
    public int Index(int column, int row) => row * Columns + column;
    public int Column(int cell) => cell % Columns;
    public int Row(int cell) => cell / Columns;

    public bool IsBorder(int cell)
    {
        int column = Column(cell);
        int row = Row(cell);
        return column == 0 || column == Columns - 1 || row == 0 || row == Rows - 1;
    }

    /// <summary>The neighbouring cell across a side, or -1 at the map border.</summary>
    public int Neighbour(int cell, int side)
    {
        int column = Column(cell) + ColumnStep[side];
        int row = Row(cell) + RowStep[side];
        return column < 0 || column >= Columns || row < 0 || row >= Rows ? -1 : Index(column, row);
    }

    public bool IsBorderSide(int cell, int side) => Neighbour(cell, side) < 0;

    public Vector3 CellOrigin(int cell)
    {
        float half = TileSize * 0.5f;
        return new Vector3(-Columns * half + (Column(cell) + 0.5f) * TileSize, 0f,
            -Rows * half + (Row(cell) + 0.5f) * TileSize);
    }
}
}
