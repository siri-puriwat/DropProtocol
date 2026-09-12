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

    [SerializeField]
    private UiSfx m_sfx;

    [SerializeField]
    private AudioClip m_deployClip;

    [SerializeField]
    private AudioClip m_extractionClip;

    [SerializeField]
    private AudioClip m_extractionStinger;

    [SerializeField]
    private AudioClip m_relayStinger;

    private bool m_seeded;
    private MissionPhase m_shownPhase;
    private int m_shownRelays;

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
            m_seeded = false;
            return;
        }

        var phase = mission.Phase.Value;
        PlayTransitions(phase, mission.RelaysActivated.Value);
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

    // The first poll after the director spawns only seeds the cache, so a late joiner hears no replayed cue.
    private void PlayTransitions(MissionPhase phase, int relays)
    {
        if (m_seeded && m_sfx != null)
        {
            if (phase == MissionPhase.Active && m_shownPhase == MissionPhase.Deployment)
            {
                m_sfx.PlayUi(m_deployClip);
            }
            else if (phase == MissionPhase.Extraction && m_shownPhase == MissionPhase.Active)
            {
                m_sfx.PlayUi(m_extractionClip);
                m_sfx.PlayMusic(m_extractionStinger);
            }

            if (relays > m_shownRelays && phase == MissionPhase.Active)
            {
                m_sfx.PlayMusic(m_relayStinger);
            }
        }

        m_seeded = true;
        m_shownPhase = phase;
        m_shownRelays = relays;
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
