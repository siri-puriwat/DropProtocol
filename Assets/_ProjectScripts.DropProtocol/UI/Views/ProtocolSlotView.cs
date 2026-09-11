using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>One loadout row: name, glyph sequence with the accepted prefix highlighted, cooldown.</summary>
public sealed class ProtocolSlotView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_name;

    [SerializeField]
    private TMP_Text m_glyphs;

    [SerializeField]
    private TMP_Text m_cooldown;

    [SerializeField]
    private Image m_cooldownFill;

    [SerializeField]
    private Color m_readyColor = Color.white;

    [SerializeField]
    private Color m_coolingColor = new(0.6f, 0.6f, 0.6f);

    public string Glyphs => m_glyphs != null ? m_glyphs.text : string.Empty;

    public void Apply(ProtocolDefinition definition, ProtocolDirection[] entered, int enteredCount, float remaining,
        string highlightHex)
    {
        gameObject.SetActive(definition != null);
        if (definition == null)
        {
            return;
        }

        bool ready = remaining <= 0f;
        if (m_name != null)
        {
            m_name.text = definition.DisplayName;
            m_name.color = ready ? m_readyColor : m_coolingColor;
        }

        if (m_glyphs != null)
        {
            m_glyphs.text = HudFormat.SequenceGlyphs(definition.Sequence, entered, enteredCount, highlightHex);
        }

        if (m_cooldown != null)
        {
            m_cooldown.text = HudFormat.CooldownLabel(remaining);
        }

        if (m_cooldownFill != null)
        {
            float total = Mathf.Max(0.01f, definition.CooldownSeconds);
            m_cooldownFill.fillAmount = ready ? 0f : Mathf.Clamp01(remaining / total);
        }
    }
}
}
