using System.Collections.Generic;

namespace DropProtocol
{
/// <summary>
///     Per-character protocol state: the directions entered so far and one cooldown per loadout slot.
///     Time is passed in so the matcher and cooldowns are testable without a scene.
/// </summary>
public sealed class ProtocolState
{
    private readonly IReadOnlyList<ProtocolDirection[]> m_sequences;
    private readonly ProtocolTuning m_tuning;
    private readonly List<ProtocolDirection> m_entered = new(ProtocolRules.MaxSequenceLength);
    private readonly float[] m_cooldowns;
    private double m_lastPushAt;

    public ProtocolState(IReadOnlyList<ProtocolDirection[]> sequences, ProtocolTuning tuning)
    {
        m_sequences = sequences;
        m_tuning = tuning;
        m_cooldowns = new float[sequences.Count];
    }

    public IReadOnlyList<ProtocolDirection> Entered => m_entered;
    public int SlotCount => m_cooldowns.Length;

    public ProtocolMatch Push(ProtocolDirection direction, double now, out int slot)
    {
        ExpireEntry(now);
        m_lastPushAt = now;

        if (m_entered.Count >= ProtocolRules.MaxSequenceLength)
        {
            m_entered.Clear();
        }

        m_entered.Add(direction);
        var result = ProtocolRules.Match(m_sequences, m_entered, out slot);
        if (result != ProtocolMatch.Pending)
        {
            m_entered.Clear();
        }

        return result;
    }

    public void Tick(double now, float deltaTime)
    {
        ExpireEntry(now);
        for (int i = 0; i < m_cooldowns.Length; i++)
        {
            m_cooldowns[i] = ProtocolRules.CooldownStep(m_cooldowns[i], deltaTime);
        }
    }

    public void Reset()
    {
        m_entered.Clear();
    }

    public void StartCooldown(int slot, float seconds)
    {
        m_cooldowns[slot] = seconds;
    }

    public float CooldownRemaining(int slot)
    {
        return m_cooldowns[slot];
    }

    private void ExpireEntry(double now)
    {
        if (m_entered.Count > 0 && ProtocolRules.HasTimedOut(m_lastPushAt, now, m_tuning.InputTimeoutSeconds))
        {
            m_entered.Clear();
        }
    }
}
}
