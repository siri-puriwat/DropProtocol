using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Colours a relay by its replicated progress and hums while it charges. The hum and colour are state and
///     re-apply on spawn; the start and completion cues are edges, so the first observation only seeds them
///     and a late joiner never hears a replay. Presentation only; it never writes gameplay.
/// </summary>
public sealed class RelayIndicator : NetworkBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private CommRelay m_relay;

    [SerializeField]
    private Renderer m_renderer;

    [SerializeField]
    private Color m_idleColor = new(0.35f, 0.35f, 0.4f);

    [SerializeField]
    private Color m_chargingColor = new(1f, 0.8f, 0.2f);

    [SerializeField]
    private Color m_activatedColor = new(0.2f, 1f, 0.4f);

    [SerializeField]
    private SfxCue m_startCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_completeCue = SfxCue.Default;

    [SerializeField]
    private LoopFader m_hum;

    [Tooltip("Hum pitch at zero and full charge.")]
    [SerializeField]
    private Vector2 m_humPitch = new(0.9f, 1.1f);

    private MaterialPropertyBlock m_block;
    private bool m_seeded;
    private bool m_shownCharging;
    private bool m_shownActivated;

    private void Awake()
    {
        m_block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (m_relay == null)
        {
            return;
        }

        m_relay.Progress.OnValueChanged += HandleProgressChanged;
        m_relay.IsActivated.OnValueChanged += HandleActivatedChanged;
        Apply(false);
    }

    private void OnDisable()
    {
        if (m_relay == null)
        {
            return;
        }

        m_relay.Progress.OnValueChanged -= HandleProgressChanged;
        m_relay.IsActivated.OnValueChanged -= HandleActivatedChanged;
    }

    // Initial sync arrives without OnValueChanged, so this re-seeds the cached state without playing edges.
    public override void OnNetworkSpawn()
    {
        if (m_relay != null)
        {
            Apply(false);
        }
    }

    private void HandleProgressChanged(float previous, float current)
    {
        Apply(true);
    }

    private void HandleActivatedChanged(bool previous, bool current)
    {
        Apply(true);
    }

    private void Apply(bool emitEdges)
    {
        bool activated = m_relay.IsActivated.Value;
        float progress = m_relay.Progress.Value;
        bool charging = !activated && progress > 0f;

        if (emitEdges && m_seeded)
        {
            if (charging && !m_shownCharging)
            {
                SfxPlayer.Play(m_startCue, transform.position);
            }

            if (activated && !m_shownActivated)
            {
                SfxPlayer.Play(m_completeCue, transform.position);
            }
        }

        m_seeded = true;
        m_shownCharging = charging;
        m_shownActivated = activated;

        if (m_hum != null)
        {
            m_hum.SetPlaying(charging);
            m_hum.SetPitch(Mathf.Lerp(m_humPitch.x, m_humPitch.y, progress));
        }

        if (m_renderer == null)
        {
            return;
        }

        var color = activated
            ? m_activatedColor
            : Color.Lerp(m_idleColor, m_chargingColor, progress);
        m_renderer.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, color);
        m_renderer.SetPropertyBlock(m_block);
    }
}
}
