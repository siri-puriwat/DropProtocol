using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Sentry Protocol payload. Server-side it turns toward the nearest living enemy and fires hitscan
///     shots until its ammo or lifetime runs out. It has no collider and no health: enemies ignore it,
///     player shots pass through it, and NetworkTransform replicates the yaw for display.
/// </summary>
[RequireComponent(typeof(NetworkTransform))]
public sealed class SentryTurret : NetworkBehaviour, IShotSource
{
    private const int HitBufferSize = 8;

    [SerializeField]
    [Min(0f)]
    private float m_deploySeconds = 1f;

    [SerializeField]
    [Min(0.1f)]
    private float m_range = 12f;

    [SerializeField]
    [Min(0)]
    private int m_damage = 15;

    [SerializeField]
    [Min(0.01f)]
    private float m_fireInterval = 0.25f;

    [SerializeField]
    [Min(1)]
    private int m_ammo = 40;

    [SerializeField]
    [Min(0f)]
    private float m_lifetimeSeconds = 30f;

    [SerializeField]
    [Min(0f)]
    private float m_headHeight = 0.9f;

    [SerializeField]
    private LayerMask m_hitMask = Physics.DefaultRaycastLayers;

    [SerializeField]
    private Transform m_muzzle;

    public NetworkVariable<int> Ammo = new();
    public NetworkVariable<bool> IsDeployed = new();

    private readonly RaycastHit[] m_hits = new RaycastHit[HitBufferSize];
    private double m_spawnedAt;
    private double m_nextFireAt;

    public event Action<Vector3, Vector3, bool> ShotFired;

    /// <summary>Test seam, before <c>Spawn()</c>.</summary>
    public void Configure(float deploySeconds, float range, int damage, float fireInterval, int ammo,
        float lifetimeSeconds)
    {
        m_deploySeconds = deploySeconds;
        m_range = range;
        m_damage = damage;
        m_fireInterval = fireInterval;
        m_ammo = ammo;
        m_lifetimeSeconds = lifetimeSeconds;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        m_spawnedAt = Time.timeAsDouble;
        Ammo.Value = m_ammo;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }

        double now = Time.timeAsDouble;
        if (now - m_spawnedAt >= m_lifetimeSeconds || Ammo.Value <= 0)
        {
            NetworkObject.Despawn();
            return;
        }

        if (now - m_spawnedAt < m_deploySeconds)
        {
            return;
        }

        IsDeployed.Value = true;

        var target = FindTarget();
        if (target == null)
        {
            return;
        }

        var toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(toTarget);
        if (now < m_nextFireAt)
        {
            return;
        }

        Fire(toTarget.normalized, now);
    }

    private void Fire(Vector3 direction, double now)
    {
        var origin = transform.position + Vector3.up * m_headHeight;
        if (!Hitscan.TryResolveHit(origin, direction, m_range, m_hitMask, m_hits, transform.root, out var hit))
        {
            return;
        }

        // Anything but an enemy in the line (a squadmate, a wall) holds fire rather than wasting a round.
        var enemy = hit.collider.GetComponentInParent<EnemyCharacter>();
        if (enemy == null)
        {
            return;
        }

        m_nextFireAt = now + m_fireInterval;
        Ammo.Value -= 1;

        var health = enemy.GetComponent<Health>();
        if (health != null)
        {
            health.ApplyDamage(m_damage);
        }

        var muzzle = m_muzzle != null ? m_muzzle.position : origin;
        ShotFiredRpc(muzzle, hit.point, true);
    }

    private EnemyCharacter FindTarget()
    {
        EnemyCharacter closest = null;
        float closestDistance = m_range;
        var enemies = EnemyCharacter.All;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy.IsDead)
            {
                continue;
            }

            float distance = EnemyRules.FlatDistance(transform.position, enemy.transform.position);
            if (distance <= closestDistance)
            {
                closest = enemy;
                closestDistance = distance;
            }
        }

        return closest;
    }

    // Cosmetic only; damage already travelled via NetworkVariable.
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void ShotFiredRpc(Vector3 muzzle, Vector3 end, bool hit)
    {
        ShotFired?.Invoke(muzzle, end, hit);
    }
}
}
