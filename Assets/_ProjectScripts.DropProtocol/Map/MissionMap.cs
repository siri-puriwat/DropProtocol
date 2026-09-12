using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace DropProtocol
{
/// <summary>
///     Assembles the mission map from a seed on every peer. The host, or a scene played without a
///     session, picks the seed in Awake, before Netcode spawns any in-scene object, so the anchors and
///     the NavMesh exist when the spawners run; the seed then replicates once and remote clients build
///     the same layout when this object spawns. Tiles are plain local objects: nothing about the map
///     itself crosses the network.
/// </summary>
public sealed class MissionMap : NetworkBehaviour
{
    [SerializeField]
    private TileSet m_tileSet;

    [SerializeField]
    private MapTuning m_tuning = MapTuning.Default;

    [Tooltip("Collects the colliders under this object; built after the tiles are placed.")]
    [SerializeField]
    private NavMeshSurface m_surface;

    [SerializeField]
    private Transform m_tilesRoot;

    [SerializeField]
    private Transform m_anchorsRoot;

    [Tooltip("In-scene relays moved onto the layout's relay anchors.")]
    [SerializeField]
    private CommRelay[] m_relays = new CommRelay[0];

    [SerializeField]
    private ExtractionZone m_zone;

    [SerializeField]
    private PlayerSpawner m_playerSpawner;

    [SerializeField]
    private EnemyDirector m_enemyDirector;

    [Tooltip("Seed used by the edit-mode preview context menu.")]
    [SerializeField]
    private int m_previewSeed = 1;

    public NetworkVariable<int> Seed = new();

    private Transform[] m_playerSpawns = new Transform[0];
    private Transform[] m_enemySpawns = new Transform[0];

    /// <summary>The scene's map, or null in the sandbox and in tests without one.</summary>
    public static MissionMap Instance { get; private set; }

    /// <summary>Seed the next map assembled in Awake uses, consumed once; null draws a random one.</summary>
    public static int? PendingSeed { get; set; }

    public MapLayout Layout { get; private set; }
    public TileSet TileSet => m_tileSet;
    public MapTuning Tuning => m_tuning;
    public IReadOnlyList<Transform> PlayerSpawns => m_playerSpawns;
    public IReadOnlyList<Transform> EnemySpawns => m_enemySpawns;

    private void Awake()
    {
        Instance = this;
        if (m_tileSet == null || m_tileSet.Tiles.Count == 0)
        {
            Debug.LogError("MissionMap has no tile set; the mission scene cannot be assembled.", this);
            return;
        }

        if (m_relays.Length != m_tuning.RelayCount)
        {
            Debug.LogWarning($"MissionMap moves {m_relays.Length} relays but the tuning places {m_tuning.RelayCount}.", this);
        }

        // A remote client receives the seed with this object's spawn payload; a map built now would be
        // thrown away. Everyone else (host, or a scene played without a session) assembles right away so
        // the anchors and the NavMesh exist before any in-scene NetworkObject spawns.
        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && manager.IsClient && !manager.IsServer)
        {
            return;
        }

        int seed = PendingSeed ?? NewSeed();
        PendingSeed = null;
        Assemble(seed, true);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (Layout == null)
            {
                Assemble(NewSeed(), true);
            }

            Seed.Value = Layout.Seed;
            return;
        }

        // A client that joined from the menu deferred in Awake; one that opened the scene itself built a
        // local map with its own seed. Both converge on the replicated seed here.
        if (Layout == null || Layout.Seed != Seed.Value)
        {
            Assemble(Seed.Value, false);
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        PendingSeed = null;
    }

    /// <summary>Tests configure in code before the object spawns.</summary>
    public void Configure(TileSet tileSet, MapTuning tuning, NavMeshSurface surface, Transform tilesRoot,
        Transform anchorsRoot, CommRelay[] relays, ExtractionZone zone, PlayerSpawner playerSpawner,
        EnemyDirector enemyDirector)
    {
        m_tileSet = tileSet;
        m_tuning = tuning;
        m_surface = surface;
        m_tilesRoot = tilesRoot;
        m_anchorsRoot = anchorsRoot;
        m_relays = relays;
        m_zone = zone;
        m_playerSpawner = playerSpawner;
        m_enemyDirector = enemyDirector;
    }

    /// <summary>Replaces whatever is assembled with the layout for <paramref name="seed" />.</summary>
    public void Assemble(int seed, bool buildNavMesh)
    {
        var watch = Stopwatch.StartNew();
        Clear();
        Layout = MapRules.Generate(seed, m_tuning, m_tileSet.Templates());
        SpawnTiles();
        m_playerSpawns = CreateAnchors("SpawnPoint_", Layout.SquadSpawns);
        m_enemySpawns = CreateAnchors("EnemySpawn_", Layout.EnemySpawns);
        PlaceObjectives();

        if (m_playerSpawner != null)
        {
            m_playerSpawner.SetSpawnPoints(m_playerSpawns);
        }

        if (m_enemyDirector != null)
        {
            m_enemyDirector.SetSpawnPoints(m_enemySpawns);
        }

        if (buildNavMesh && m_surface != null)
        {
            m_surface.BuildNavMesh();
        }

        Debug.Log($"Map seed {seed}: {Layout.CellCount} tiles in {watch.ElapsedMilliseconds} ms, NavMesh {(buildNavMesh ? "built" : "skipped")}.", this);
    }

    public void Clear()
    {
        Layout = null;
        m_playerSpawns = new Transform[0];
        m_enemySpawns = new Transform[0];
        DestroyChildren(m_tilesRoot);
        DestroyChildren(m_anchorsRoot);
        if (m_surface != null)
        {
            m_surface.RemoveData();
        }
    }

    private void SpawnTiles()
    {
        for (int cell = 0; cell < Layout.CellCount; cell++)
        {
            MapTile prefab = m_tileSet.Tiles[Layout.TileIndices[cell]];
            int quarterTurns = Layout.QuarterTurns[cell];
            MapTile tile = Instantiate(prefab, Layout.CellOrigin(cell), MapRules.Rotation(quarterTurns), m_tilesRoot);
            tile.name = $"{prefab.name} [{Layout.Column(cell)},{Layout.Row(cell)}]";
            MarkTransient(tile.gameObject);
            // Border walls come from the scene perimeter; a tile's own edge walls only matter inside.
            for (int side = 0; side < MapRules.SideCount; side++)
            {
                tile.SetEdgeActive(side, !Layout.IsBorderSide(cell, MapRules.Turn(side, quarterTurns)));
            }
        }
    }

    private Transform[] CreateAnchors(string prefix, Pose[] poses)
    {
        var anchors = new Transform[poses.Length];
        for (int i = 0; i < poses.Length; i++)
        {
            var anchor = new GameObject(prefix + i);
            anchor.transform.SetPositionAndRotation(poses[i].position, poses[i].rotation);
            anchor.transform.SetParent(m_anchorsRoot, true);
            MarkTransient(anchor);
            anchors[i] = anchor.transform;
        }

        return anchors;
    }

    private void PlaceObjectives()
    {
        for (int i = 0; i < m_relays.Length && i < Layout.Relays.Length; i++)
        {
            if (m_relays[i] != null)
            {
                m_relays[i].transform.SetPositionAndRotation(Layout.Relays[i].position, Layout.Relays[i].rotation);
            }
        }

        if (m_zone != null)
        {
            m_zone.transform.SetPositionAndRotation(Layout.Extraction.position, Layout.Extraction.rotation);
        }
    }

    private static void DestroyChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        // Detach and deactivate first: a deferred Destroy would still expose the old colliders to a
        // NavMesh build that runs later this frame.
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            child.transform.SetParent(null, true);
            child.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private static void MarkTransient(GameObject instance)
    {
        // Edit-mode previews must never be saved into the scene.
        if (!Application.isPlaying)
        {
            instance.hideFlags = HideFlags.DontSave;
        }
    }

    private static int NewSeed() => new Random().Next(1, int.MaxValue);

    [ContextMenu("Preview seed")]
    private void PreviewSeed()
    {
        Assemble(m_previewSeed, false);
    }

    [ContextMenu("Clear preview")]
    private void ClearPreview()
    {
        Clear();
    }
}
}
