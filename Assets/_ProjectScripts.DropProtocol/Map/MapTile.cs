using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Authoring data on a tile prefab root: the socket on each side, anchors in tile-local whole metres
///     (x east, y north, origin at the centre), and the four edge wall groups the map switches off on
///     the outer border, where the scene perimeter takes over.
/// </summary>
public sealed class MapTile : MonoBehaviour
{
    private const float SitePad = 6f;

    [SerializeField]
    private MapSocket m_north;

    [SerializeField]
    private MapSocket m_east;

    [SerializeField]
    private MapSocket m_south;

    [SerializeField]
    private MapSocket m_west;

    [Tooltip("Clear 6 m pads that can take the squad drop, a relay or the extraction.")]
    [SerializeField]
    private Vector2Int[] m_sites = new Vector2Int[0];

    [SerializeField]
    private Vector2Int[] m_enemySpawns = new Vector2Int[0];

    [Tooltip("Wall groups per side, north, east, south, west; disabled on the map border.")]
    [SerializeField]
    private GameObject[] m_edges = new GameObject[MapRules.SideCount];

    [SerializeField]
    [Min(4)]
    private int m_size = 20;

    public int Size => m_size;

    public MapSocket Socket(int side)
    {
        switch (side)
        {
            case MapRules.North: return m_north;
            case MapRules.East: return m_east;
            case MapRules.South: return m_south;
            default: return m_west;
        }
    }

    public GameObject Edge(int side) => side >= 0 && side < m_edges.Length ? m_edges[side] : null;

    public TileTemplate ToTemplate()
    {
        return new TileTemplate(name, new[] { m_north, m_east, m_south, m_west }, m_sites, m_enemySpawns);
    }

    public void SetEdgeActive(int side, bool active)
    {
        GameObject edge = Edge(side);
        if (edge != null && edge.activeSelf != active)
        {
            edge.SetActive(active);
        }
    }

    /// <summary>The tile factory and tests build tiles in code.</summary>
    public void Configure(MapSocket[] sockets, Vector2Int[] sites, Vector2Int[] enemySpawns, GameObject[] edges, int size)
    {
        m_north = sockets[MapRules.North];
        m_east = sockets[MapRules.East];
        m_south = sockets[MapRules.South];
        m_west = sockets[MapRules.West];
        m_sites = sites;
        m_enemySpawns = enemySpawns;
        m_edges = edges;
        m_size = size;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (Vector2Int site in m_sites)
        {
            Gizmos.DrawWireCube(transform.TransformPoint(new Vector3(site.x, 0.1f, site.y)), new Vector3(SitePad, 0.2f, SitePad));
        }

        Gizmos.color = Color.red;
        foreach (Vector2Int spawn in m_enemySpawns)
        {
            Gizmos.DrawSphere(transform.TransformPoint(new Vector3(spawn.x, 0.5f, spawn.y)), 0.4f);
        }
    }
}
}
