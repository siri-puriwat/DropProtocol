using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Colours a relay by its replicated progress. Presentation only; it never writes gameplay.</summary>
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

    private MaterialPropertyBlock m_block;

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
        Apply();
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

    public override void OnNetworkSpawn()
    {
        if (m_relay != null)
        {
            Apply();
        }
    }

    private void HandleProgressChanged(float previous, float current)
    {
        Apply();
    }

    private void HandleActivatedChanged(bool previous, bool current)
    {
        Apply();
    }

    private void Apply()
    {
        if (m_renderer == null)
        {
            return;
        }

        var color = m_relay.IsActivated.Value
            ? m_activatedColor
            : Color.Lerp(m_idleColor, m_chargingColor, m_relay.Progress.Value);
        m_renderer.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, color);
        m_renderer.SetPropertyBlock(m_block);
    }
}
}
