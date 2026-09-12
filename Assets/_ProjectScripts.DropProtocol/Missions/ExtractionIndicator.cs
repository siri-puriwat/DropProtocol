using UnityEngine;

namespace DropProtocol
{
/// <summary>Lights the extraction zone once the mission reaches Extraction. Presentation only.</summary>
public sealed class ExtractionIndicator : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private Renderer m_renderer;

    [SerializeField]
    private Color m_closedColor = new(0.3f, 0.3f, 0.35f);

    [SerializeField]
    private Color m_openColor = new(0.2f, 0.9f, 0.3f);

    [SerializeField]
    private LoopFader m_beacon;

    private MaterialPropertyBlock m_block;
    private bool m_lastOpen;
    private bool m_applied;

    private void Awake()
    {
        m_block = new MaterialPropertyBlock();
    }

    // Polled: the mission director may spawn after this object on a late-joining client.
    private void Update()
    {
        var mission = MissionDirector.Instance;
        bool open = mission != null && mission.Phase.Value == MissionPhase.Extraction;
        if (m_applied && open == m_lastOpen)
        {
            return;
        }

        m_applied = true;
        m_lastOpen = open;
        if (m_beacon != null)
        {
            m_beacon.SetPlaying(open);
        }

        Apply(open ? m_openColor : m_closedColor);
    }

    private void Apply(Color color)
    {
        if (m_renderer == null)
        {
            return;
        }

        m_renderer.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, color);
        m_renderer.SetPropertyBlock(m_block);
    }
}
}
