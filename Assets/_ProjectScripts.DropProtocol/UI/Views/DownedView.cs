using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>
///     Centre prompt while the local player is downed, or while it is reviving someone. The reviver's
///     target is not replicated, so a downed player sees the highest revive progress in the squad, which
///     is right whenever one revive is in progress.
/// </summary>
public sealed class DownedView : HudView
{
    [SerializeField]
    private GameObject m_root;

    [SerializeField]
    private TMP_Text m_label;

    [SerializeField]
    private Image m_progress;

    private Health m_health;
    private ReviveController m_reviver;

    public bool IsShown => m_root != null && m_root.activeSelf;

    protected override void OnBind(NetworkPlayer player)
    {
        m_health = player.Health;
        m_reviver = player.GetComponent<ReviveController>();
    }

    protected override void OnUnbind(NetworkPlayer player)
    {
        m_health = null;
        m_reviver = null;
        Show(false);
    }

    private void Update()
    {
        if (m_health == null)
        {
            Show(false);
            return;
        }

        if (m_health.IsDowned)
        {
            float progress = SquadReviveProgress();
            Show(true);
            SetLabel(progress > 0f ? "BEING REVIVED" : "DOWNED\nWait for a squadmate");
            SetProgress(progress);
            return;
        }

        float reviving = m_reviver != null ? m_reviver.Progress.Value : 0f;
        Show(reviving > 0f);
        if (reviving > 0f)
        {
            SetLabel("REVIVING");
            SetProgress(reviving);
        }
    }

    private static float SquadReviveProgress()
    {
        float best = 0f;
        var players = NetworkPlayer.All;
        for (int i = 0; i < players.Count; i++)
        {
            var reviver = players[i].GetComponent<ReviveController>();
            if (reviver != null)
            {
                best = Mathf.Max(best, reviver.Progress.Value);
            }
        }

        return best;
    }

    private void Show(bool shown)
    {
        if (m_root != null && m_root.activeSelf != shown)
        {
            m_root.SetActive(shown);
        }
    }

    private void SetLabel(string text)
    {
        if (m_label != null && m_label.text != text)
        {
            m_label.text = text;
        }
    }

    private void SetProgress(float value)
    {
        if (m_progress != null)
        {
            m_progress.fillAmount = Mathf.Clamp01(value);
        }
    }
}
}
