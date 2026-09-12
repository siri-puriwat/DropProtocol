using System.Collections.Generic;
using UnityEngine;

namespace DropProtocol
{
/// <summary>The tile prefabs a map can be assembled from. Prefab order is part of what a seed means.</summary>
[CreateAssetMenu(menuName = "DropProtocol/Tile Set", fileName = "TileSet")]
public sealed class TileSet : ScriptableObject
{
    [SerializeField]
    private MapTile[] m_tiles = new MapTile[0];

    public IReadOnlyList<MapTile> Tiles => m_tiles;

    public TileTemplate[] Templates()
    {
        var templates = new TileTemplate[m_tiles.Length];
        for (int i = 0; i < templates.Length; i++)
        {
            templates[i] = m_tiles[i].ToTemplate();
        }

        return templates;
    }

    public static TileSet Create(params MapTile[] tiles)
    {
        var set = CreateInstance<TileSet>();
        set.m_tiles = tiles;
        return set;
    }
}
}
