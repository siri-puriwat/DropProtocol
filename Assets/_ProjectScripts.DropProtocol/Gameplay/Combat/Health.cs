using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-authoritative hit points. Downed is derived from <see cref="Current" /> so it can never
///     disagree with the replicated value.
/// </summary>
public sealed class Health : NetworkBehaviour
{
    [SerializeField]
    [Min(1)]
    private int m_maxHealth = 100;

    public NetworkVariable<int> Current = new();

    public int Max => m_maxHealth;
    public bool IsDowned => Current.Value <= 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Current.Value = m_maxHealth;
        }
    }

    // Lets data-driven owners (enemy definitions) override the prefab value before spawn.
    public void SetMaxHealth(int maxHealth)
    {
        m_maxHealth = Mathf.Max(1, maxHealth);
        if (IsSpawned && IsServer)
        {
            Current.Value = HealthRules.Clamp(Current.Value, m_maxHealth);
        }
    }

    public void ApplyDamage(int amount)
    {
        if (!IsServer)
        {
            return;
        }

        Current.Value = HealthRules.AfterDamage(Current.Value, amount);
    }

    public void Revive(int hitPoints)
    {
        if (!IsServer || !IsDowned)
        {
            return;
        }

        Current.Value = HealthRules.Clamp(hitPoints, m_maxHealth);
    }

    // Downed characters only come back through Revive, so a pickup cannot skip the revive rules.
    public void Heal(int amount)
    {
        if (!IsServer || IsDowned)
        {
            return;
        }

        Current.Value = HealthRules.AfterHeal(Current.Value, amount, m_maxHealth);
    }
}
}
