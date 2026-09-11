using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
public sealed class SquadRowView : MonoBehaviour
{
    [SerializeField]
    private Image m_dot;

    [SerializeField]
    private TMP_Text m_name;

    [SerializeField]
    private Image m_fill;

    [SerializeField]
    private Color m_fullColor = new(0.35f, 0.9f, 0.45f);

    [SerializeField]
    private Color m_downedColor = new(0.7f, 0.1f, 0.1f);

    public string Name => m_name != null ? m_name.text : string.Empty;

    public void Apply(NetworkPlayer player, bool isLocal)
    {
        gameObject.SetActive(player != null);
        if (player == null)
        {
            return;
        }

        var tint = player.GetComponent<PlayerTint>();
        if (m_dot != null && tint != null)
        {
            m_dot.color = tint.CurrentColor;
        }

        if (m_name != null)
        {
            string label = HudFormat.SquadName(player.PlayerIndex.Value, player.BotFlag.Value);
            m_name.text = isLocal ? label + " (you)" : label;
        }

        var health = player.Health;
        if (m_fill != null && health != null)
        {
            m_fill.fillAmount = HudFormat.HealthFraction(health.Current.Value, health.Max);
            m_fill.color = health.IsDowned ? m_downedColor : m_fullColor;
        }
    }
}
}
