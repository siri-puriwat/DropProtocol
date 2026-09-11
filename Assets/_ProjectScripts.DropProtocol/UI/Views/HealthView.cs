using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
public sealed class HealthView : HudView
{
    [SerializeField]
    private Image m_fill;

    [SerializeField]
    private TMP_Text m_label;

    [SerializeField]
    private Color m_fullColor = new(0.35f, 0.9f, 0.45f);

    [SerializeField]
    private Color m_emptyColor = new(1f, 0.4f, 0.35f);

    [SerializeField]
    private Color m_downedColor = new(0.7f, 0.1f, 0.1f);

    private Health m_health;

    public string Label => m_label != null ? m_label.text : string.Empty;

    protected override void OnBind(NetworkPlayer player)
    {
        m_health = player.Health;
        if (m_health == null)
        {
            return;
        }

        m_health.Current.OnValueChanged += HandleHealthChanged;
        Apply(m_health.Current.Value);
    }

    protected override void OnUnbind(NetworkPlayer player)
    {
        if (m_health != null)
        {
            m_health.Current.OnValueChanged -= HandleHealthChanged;
        }

        m_health = null;
    }

    private void HandleHealthChanged(int previous, int current)
    {
        Apply(current);
    }

    private void Apply(int current)
    {
        float fraction = HudFormat.HealthFraction(current, m_health.Max);
        if (m_fill != null)
        {
            m_fill.fillAmount = fraction;
            m_fill.color = current <= 0 ? m_downedColor : Color.Lerp(m_emptyColor, m_fullColor, fraction);
        }

        if (m_label != null)
        {
            m_label.text = HudFormat.HealthLabel(current, m_health.Max);
        }
    }
}
}
