using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace DropProtocol
{
/// <summary>
///     Server-side enemy spawner living in the gameplay scene. <see cref="DirectorState" /> accrues the
///     threat budget; this component spends it on prefabs at spawn points away from the squad.
///     Idle until <see cref="Running" /> is set, so the sandbox stays quiet by default.
/// </summary>
public sealed class EnemyDirector : NetworkBehaviour
{
    [Tooltip("One per archetype; the threat cost is read from each prefab EnemyDefinition.")]
    [SerializeField]
    private NetworkObject[] m_enemyPrefabs = new NetworkObject[0];

    [Tooltip("Relative pick chance per prefab, same order as the prefabs.")]
    [SerializeField]
    private int[] m_spawnWeights = { 6, 3, 1 };

    [SerializeField]
    private Transform[] m_spawnPoints = new Transform[0];

    [SerializeField]
    private DirectorTuning m_tuning = DirectorTuning.Default;

    [Tooltip("Mission-progression multiplier on budget income; the mission director overrides it at runtime.")]
    [SerializeField]
    [Min(0f)]
    private float m_intensity = 1f;

    [SerializeField]
    private bool m_runOnSpawn;

    private readonly List<Vector3> m_playerPositions = new();
    private readonly List<Vector3> m_points = new();

    private DirectorState m_state;
    private Func<double> m_nextRoll;
    private float m_elapsed;
    private int m_cursor;

    public bool Running { get; set; }

    /// <summary>Mission-progression multiplier on budget income; the mission director drives it.</summary>
    public float Intensity
    {
        get => m_intensity;
        set
        {
            m_intensity = value;
            if (m_state != null)
            {
                m_state.Intensity = value;
            }
        }
    }

    public float Budget => m_state != null ? m_state.Budget : 0f;
    public int Population => EnemyCharacter.All.Count;
    public IReadOnlyList<NetworkObject> Prefabs => m_enemyPrefabs;

    private void Update()
    {
        if (!IsServer || !Running || m_state == null)
        {
            return;
        }

        m_elapsed += Time.deltaTime;
        if (m_elapsed < m_tuning.ThinkSeconds)
        {
            return;
        }

        int index = m_state.Step(NetworkPlayer.All.Count, Population, m_elapsed);
        m_elapsed = 0f;
        if (index >= 0)
        {
            Spawn(index);
        }
    }

    /// <summary>Tests configure in code before the object spawns; the roll replaces the seeded random.</summary>
    public void Configure(NetworkObject[] prefabs, int[] weights, Transform[] spawnPoints, DirectorTuning tuning,
        Func<double> nextRoll)
    {
        m_enemyPrefabs = prefabs;
        m_spawnWeights = weights;
        m_spawnPoints = spawnPoints;
        m_tuning = tuning;
        m_nextRoll = nextRoll;
    }

    /// <summary>The mission map hands over the anchors of the assembled layout before this object spawns.</summary>
    public void SetSpawnPoints(Transform[] spawnPoints)
    {
        m_spawnPoints = spawnPoints;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        var roll = m_nextRoll ?? new Random(m_tuning.Seed).NextDouble;
        m_state = new DirectorState(ReadCosts(), m_spawnWeights, m_tuning, roll);
        m_state.Intensity = m_intensity;
        Running = m_runOnSpawn;
    }

    /// <summary>Spawns right away, ignoring the budget; the sandbox panel and tests use it.</summary>
    public EnemyCharacter Spawn(int prefabIndex)
    {
        if (!IsServer || prefabIndex < 0 || prefabIndex >= m_enemyPrefabs.Length || m_spawnPoints.Length == 0)
        {
            return null;
        }

        m_playerPositions.Clear();
        foreach (var player in NetworkPlayer.All)
        {
            if (player.Health == null || !player.Health.IsDowned)
            {
                m_playerPositions.Add(player.transform.position);
            }
        }

        m_points.Clear();
        foreach (var point in m_spawnPoints)
        {
            m_points.Add(point.position);
        }

        int pointIndex =
            DirectorRules.ChooseSpawnPoint(m_points, m_playerPositions, m_tuning.MinSpawnDistance, m_cursor);
        m_cursor = pointIndex + 1;
        var spawnPoint = m_spawnPoints[pointIndex];

        var instance = Instantiate(m_enemyPrefabs[prefabIndex], spawnPoint.position, spawnPoint.rotation);
        // Test templates are inactive stand-ins for prefabs.
        if (!instance.gameObject.activeSelf)
        {
            instance.gameObject.SetActive(true);
        }

        instance.Spawn(true);
        return instance.GetComponent<EnemyCharacter>();
    }

    private int[] ReadCosts()
    {
        int[] costs = new int[m_enemyPrefabs.Length];
        for (int i = 0; i < costs.Length; i++)
        {
            costs[i] = m_enemyPrefabs[i].GetComponent<EnemyCharacter>().Definition.ThreatCost;
        }

        return costs;
    }
}
}
