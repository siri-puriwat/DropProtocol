using TMPro;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Mission phase, objective, clock and kills. Polls the in-scene director because it spawns with the scene.</summary>
public sealed class MissionView : HudView
{
    [SerializeField]
    private GameObject m_root;

    [SerializeField]
    private TMP_Text m_phase;

    [SerializeField]
    private TMP_Text m_objective;

    [SerializeField]
    private TMP_Text m_clock;

    [SerializeField]
    private TMP_Text m_kills;

    public string PhaseLabel => m_phase != null ? m_phase.text : string.Empty;

    private void Update()
    {
        var mission = MissionDirector.Instance;
        bool show = mission != null && mission.IsSpawned;
        if (m_root != null && m_root.activeSelf != show)
        {
            m_root.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        var phase = mission.Phase.Value;
        SetText(m_phase, HudFormat.PhaseLabel(phase));
        SetText(m_kills, $"Kills {mission.EnemiesKilled.Value}");

        switch (phase)
        {
            case MissionPhase.Deployment:
                SetText(m_objective, $"Deploying in {Mathf.CeilToInt(mission.DeployRemaining.Value)}");
                SetText(m_clock, string.Empty);
                break;
            case MissionPhase.Active:
                SetText(m_objective, $"Relays {mission.RelaysActivated.Value} / {mission.RelayCount.Value}");
                SetText(m_clock, HudFormat.Clock(mission.Elapsed.Value, mission.TimeLimit.Value));
                break;
            case MissionPhase.Extraction:
                SetText(m_objective, $"Reach the extraction zone  {mission.ExtractionRemaining.Value:0.0}s");
                SetText(m_clock, HudFormat.Clock(mission.Elapsed.Value, mission.TimeLimit.Value));
                break;
            default:
                SetText(m_objective, string.Empty);
                SetText(m_clock, HudFormat.Clock(mission.Elapsed.Value, 0f));
                break;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null && text.text != value)
        {
            text.text = value;
        }
    }
}
}
