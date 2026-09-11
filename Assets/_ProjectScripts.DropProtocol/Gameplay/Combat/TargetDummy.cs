using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Sandbox practice target: heals back to full a few seconds after being downed.</summary>
[RequireComponent(typeof(Health))]
public sealed class TargetDummy : NetworkBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float m_resetSeconds = 3f;

    private Health m_health;
    private bool m_resetPending;
    private double m_resetAt;

    public float ResetSeconds => m_resetSeconds;

    private void Awake()
    {
        m_health = GetComponent<Health>();
    }

    private void Update()
    {
        if (!IsServer || !m_resetPending || Time.timeAsDouble < m_resetAt)
        {
            return;
        }

        m_resetPending = false;
        m_health.Revive(m_health.Max);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_health.Current.OnValueChanged += HandleHealthChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        m_health.Current.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int previous, int current)
    {
        if (previous > 0 && current <= 0)
        {
            m_resetPending = true;
            m_resetAt = Time.timeAsDouble + m_resetSeconds;
        }
    }
}
}
