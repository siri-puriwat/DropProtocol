using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Strike Protocol payload. Counts down a replicated warning, then damages everything with health
///     inside the radius, squadmates included, and lingers briefly so peers can show the impact.
/// </summary>
public sealed class StrikeBeacon : NetworkBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float m_warningSeconds = 3f;

    [SerializeField]
    [Min(0.1f)]
    private float m_radius = 4f;

    [SerializeField]
    [Min(0)]
    private int m_damage = 120;

    [SerializeField]
    [Min(0f)]
    private float m_aftermathSeconds = 1f;

    public NetworkVariable<float> Remaining = new();
    public NetworkVariable<bool> HasStruck = new();

    private double m_spawnedAt;

    public float WarningSeconds => m_warningSeconds;
    public float Radius => m_radius;

    /// <summary>Test seam, before <c>Spawn()</c>.</summary>
    public void Configure(float warningSeconds, float radius, int damage, float aftermathSeconds)
    {
        m_warningSeconds = warningSeconds;
        m_radius = radius;
        m_damage = damage;
        m_aftermathSeconds = aftermathSeconds;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        m_spawnedAt = Time.timeAsDouble;
        Remaining.Value = m_warningSeconds;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }

        float age = (float)(Time.timeAsDouble - m_spawnedAt);
        if (HasStruck.Value)
        {
            if (age >= m_warningSeconds + m_aftermathSeconds)
            {
                NetworkObject.Despawn();
            }

            return;
        }

        Remaining.Value = Mathf.Max(0f, m_warningSeconds - age);
        if (Remaining.Value > 0f)
        {
            return;
        }

        AreaDamage.Apply(transform.position, m_radius, m_damage);
        HasStruck.Value = true;
    }
}
}
