using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Supply Protocol payload. After arriving it heals and refills every standing squadmate that steps
///     inside once, then despawns when everyone has been served or its lifetime runs out. No collider:
///     pickup is a server-side radius check like the relay and the extraction zone.
/// </summary>
public sealed class SupplyPod : NetworkBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float m_arriveSeconds = 1.5f;

    [SerializeField]
    [Min(0f)]
    private float m_radius = 3f;

    [SerializeField]
    [Min(0)]
    private int m_healAmount = 50;

    [SerializeField]
    [Min(0f)]
    private float m_lifetimeSeconds = 20f;

    public NetworkVariable<bool> IsOpen = new();
    public NetworkVariable<int> Served = new();

    private readonly HashSet<NetworkPlayer> m_served = new();
    private double m_spawnedAt;

    public float Radius => m_radius;

    /// <summary>Test seam, before <c>Spawn()</c>.</summary>
    public void Configure(float arriveSeconds, float radius, int healAmount, float lifetimeSeconds)
    {
        m_arriveSeconds = arriveSeconds;
        m_radius = radius;
        m_healAmount = healAmount;
        m_lifetimeSeconds = lifetimeSeconds;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_spawnedAt = Time.timeAsDouble;
        }
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }

        double age = Time.timeAsDouble - m_spawnedAt;
        if (age >= m_lifetimeSeconds)
        {
            NetworkObject.Despawn();
            return;
        }

        if (age < m_arriveSeconds)
        {
            return;
        }

        IsOpen.Value = true;
        Serve();

        int squad = NetworkPlayer.All.Count;
        if (squad > 0 && m_served.Count >= squad)
        {
            NetworkObject.Despawn();
        }
    }

    private void Serve()
    {
        var players = NetworkPlayer.All;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player.Health == null || player.Health.IsDowned || m_served.Contains(player))
            {
                continue;
            }

            if (!MissionRules.IsInside(player.transform.position, transform.position, m_radius))
            {
                continue;
            }

            player.Health.Heal(m_healAmount);
            var weapon = player.GetComponent<WeaponController>();
            if (weapon != null)
            {
                weapon.RefillAmmo();
            }

            m_served.Add(player);
            Served.Value = m_served.Count;
        }
    }
}
}
