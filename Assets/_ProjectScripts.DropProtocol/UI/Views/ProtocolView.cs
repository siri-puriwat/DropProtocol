using UnityEngine;

namespace DropProtocol
{
public sealed class ProtocolView : HudView
{
    [SerializeField]
    private ProtocolSlotView[] m_slots = System.Array.Empty<ProtocolSlotView>();

    [SerializeField]
    private string m_highlightHex = "#FFD84A";

    [SerializeField]
    private AudioClip m_keyClip;

    [SerializeField]
    private AudioClip m_rejectClip;

    [SerializeField]
    private AudioSource m_audio;

    private readonly ProtocolDirection[] m_entered = new ProtocolDirection[ProtocolRules.MaxSequenceLength];
    private ProtocolController m_protocols;
    private int m_enteredCount;

    public ProtocolSlotView Slot(int index)
    {
        return index >= 0 && index < m_slots.Length ? m_slots[index] : null;
    }

    protected override void OnBind(NetworkPlayer player)
    {
        m_protocols = player.GetComponent<ProtocolController>();
        if (m_protocols == null)
        {
            foreach (var slot in m_slots)
            {
                slot.Apply(null, m_entered, 0, 0f, m_highlightHex);
            }

            return;
        }

        m_protocols.Entered.OnValueChanged += HandleEnteredChanged;
        m_protocols.Cooldowns.OnValueChanged += HandleCooldownsChanged;
        m_enteredCount = ProtocolRules.Unpack(m_protocols.Entered.Value, m_entered);
        Apply();
    }

    protected override void OnUnbind(NetworkPlayer player)
    {
        if (m_protocols != null)
        {
            m_protocols.Entered.OnValueChanged -= HandleEnteredChanged;
            m_protocols.Cooldowns.OnValueChanged -= HandleCooldownsChanged;
        }

        m_protocols = null;
    }

    private void HandleEnteredChanged(int previous, int current)
    {
        int previousCount = m_enteredCount;
        m_enteredCount = ProtocolRules.Unpack(current, m_entered);
        // A longer prefix is a key the host accepted; a reset after a partial entry is a rejection or a call.
        if (m_enteredCount > previousCount)
        {
            PlayUi(m_keyClip);
        }
        else if (m_enteredCount == 0 && previousCount > 0 && !AnyCooldownJustStarted())
        {
            PlayUi(m_rejectClip);
        }

        Apply();
    }

    private void HandleCooldownsChanged(ProtocolCooldowns previous, ProtocolCooldowns current)
    {
        Apply();
    }

    private bool AnyCooldownJustStarted()
    {
        var cooldowns = m_protocols.Cooldowns.Value;
        for (int i = 0; i < ProtocolCooldowns.SlotCount; i++)
        {
            if (cooldowns[i] > 0f)
            {
                return true;
            }
        }

        return false;
    }

    private void Apply()
    {
        var loadout = m_protocols.Loadout;
        var cooldowns = m_protocols.Cooldowns.Value;
        for (int i = 0; i < m_slots.Length; i++)
        {
            var definition = i < loadout.Count && i < ProtocolCooldowns.SlotCount ? loadout[i] : null;
            float remaining = i < ProtocolCooldowns.SlotCount ? cooldowns[i] : 0f;
            m_slots[i].Apply(definition, m_entered, m_enteredCount, remaining, m_highlightHex);
        }
    }

    private void PlayUi(AudioClip clip)
    {
        if (m_audio != null && clip != null)
        {
            m_audio.PlayOneShot(clip);
        }
    }
}
}
