using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-simulated projectile. It carries no collider: each server frame sweeps a sphere over the
///     step it is about to take, so it cannot tunnel and can never block player hitscan. NetworkTransform
///     replicates the position for display.
/// </summary>
[RequireComponent(typeof(NetworkTransform))]
public sealed class SpitProjectile : NetworkBehaviour
{
    private const int HitBufferSize = 8;
    private const float Radius = 0.2f;

    private readonly RaycastHit[] m_hits = new RaycastHit[HitBufferSize];

    private Vector3 m_direction;
    private int m_damage;
    private float m_speed;
    private float m_lifetimeSeconds;
    private Transform m_shooterRoot;
    private double m_spawnedAt;
    private bool m_launched;

    private void Update()
    {
        if (!IsServer || !IsSpawned || !m_launched)
        {
            return;
        }

        if (ProjectileMath.HasExpired(m_spawnedAt, m_lifetimeSeconds, Time.timeAsDouble))
        {
            NetworkObject.Despawn();
            return;
        }

        float step = ProjectileMath.StepLength(m_speed, Time.deltaTime);
        RaycastHit hit;
        if (TryResolveHit(step, out hit))
        {
            var target = hit.collider.GetComponentInParent<Health>();
            if (target != null)
            {
                target.ApplyDamage(m_damage);
            }

            NetworkObject.Despawn();
            return;
        }

        transform.position += m_direction * step;
    }

    public void Launch(Vector3 direction, int damage, float speed, float lifetimeSeconds, Transform shooterRoot)
    {
        if (!IsServer)
        {
            return;
        }

        m_direction = direction.normalized;
        m_damage = damage;
        m_speed = speed;
        m_lifetimeSeconds = lifetimeSeconds;
        m_shooterRoot = shooterRoot;
        m_spawnedAt = Time.timeAsDouble;
        m_launched = true;
    }

    private bool TryResolveHit(float step, out RaycastHit closest)
    {
        closest = default;
        bool found = false;
        int count = Physics.SphereCastNonAlloc(transform.position, Radius, m_direction, m_hits, step,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var candidate = m_hits[i];
            if (m_shooterRoot != null && candidate.transform.root == m_shooterRoot)
            {
                continue;
            }

            // Enemies never block or hurt each other with spit; only players and level geometry stop it.
            if (candidate.collider.GetComponentInParent<EnemyCharacter>() != null)
            {
                continue;
            }

            if (!found || candidate.distance < closest.distance)
            {
                closest = candidate;
                found = true;
            }
        }

        return found;
    }
}
}
