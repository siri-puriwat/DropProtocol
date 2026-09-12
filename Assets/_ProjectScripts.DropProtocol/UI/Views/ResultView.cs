using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>End-of-mission card. The host returns everyone to the menu; a client only leaves.</summary>
public sealed class ResultView : HudView
{
    [SerializeField]
    private GameObject m_root;

    [SerializeField]
    private TMP_Text m_title;

    [SerializeField]
    private TMP_Text m_stats;

    [SerializeField]
    private Button m_button;

    [SerializeField]
    private TMP_Text m_buttonLabel;

    [SerializeField]
    private UiSfx m_sfx;

    [SerializeField]
    private AudioClip m_completeClip;

    [SerializeField]
    private AudioClip m_failedClip;

    private MissionPhase m_shownPhase = MissionPhase.Deployment;

    public bool IsShown => m_root != null && m_root.activeSelf;

    private void Awake()
    {
        if (m_button != null)
        {
            m_button.onClick.AddListener(HandleButton);
        }
    }

    private void OnDestroy()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveListener(HandleButton);
        }
    }

    private void Update()
    {
        var mission = MissionDirector.Instance;
        var phase = mission != null && mission.IsSpawned ? mission.Phase.Value : MissionPhase.Deployment;
        bool over = phase == MissionPhase.Complete || phase == MissionPhase.Failed;

        if (m_root != null && m_root.activeSelf != over)
        {
            m_root.SetActive(over);
        }

        if (!over)
        {
            m_shownPhase = phase;
            return;
        }

        if (phase != m_shownPhase)
        {
            m_shownPhase = phase;
            PlayStinger(phase == MissionPhase.Complete ? m_completeClip : m_failedClip);
        }

        if (m_title != null)
        {
            m_title.text = HudFormat.ResultTitle(phase, mission.Outcome.Value);
        }

        if (m_stats != null)
        {
            m_stats.text =
                $"Extracted {mission.ExtractedCount.Value} / {NetworkPlayer.All.Count}\nKills {mission.EnemiesKilled.Value}\nTime {HudFormat.Clock(mission.Elapsed.Value, 0f)}";
        }

        var session = NetworkSession.Instance;
        if (m_buttonLabel != null)
        {
            m_buttonLabel.text = session != null && session.IsHost ? "Return to menu" : "Leave";
        }
    }

    private void HandleButton()
    {
        var session = NetworkSession.Instance;
        if (session != null)
        {
            session.Leave();
        }
    }

    private void PlayStinger(AudioClip clip)
    {
        if (m_sfx != null)
        {
            m_sfx.PlayMusic(clip);
        }
    }
}
}
