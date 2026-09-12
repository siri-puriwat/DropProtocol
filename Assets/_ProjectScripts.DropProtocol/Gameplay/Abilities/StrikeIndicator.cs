using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Scales the warning ring to the strike radius and colours it by the replicated countdown. Presentation only.</summary>
public sealed class StrikeIndicator : NetworkBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private StrikeBeacon m_beacon;

    [SerializeField]
    private Transform m_ring;

    [SerializeField]
    private Renderer m_renderer;

    [SerializeField]
    private Color m_armedColor = new(1f, 0.6f, 0.1f, 0.6f);

    [SerializeField]
    private Color m_imminentColor = new(1f, 0.15f, 0.1f, 0.9f);

    [SerializeField]
    private Color m_struckColor = new(0.2f, 0.2f, 0.2f, 0.8f);

    [SerializeField]
    private GameObject m_explosionVfx;

    [Tooltip("Loops while the countdown runs; a late joiner hears it mid-way instead of a replayed siren.")]
    [SerializeField]
    private LoopFader m_warning;

    [SerializeField]
    private SfxCue m_tickCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_explosionCue = SfxCue.Default;

    [SerializeField]
    private CinemachineImpulseSource m_impulse;

    [SerializeField]
    [Min(0f)]
    private float m_impulseForce = 1.5f;

    private MaterialPropertyBlock m_block;

    private void Awake()
    {
        m_block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (m_beacon == null)
        {
            return;
        }

        if (m_ring != null)
        {
            var scale = m_ring.localScale;
            m_ring.localScale = new Vector3(m_beacon.Radius * 2f, scale.y, m_beacon.Radius * 2f);
        }

        m_beacon.Remaining.OnValueChanged += HandleRemainingChanged;
        m_beacon.HasStruck.OnValueChanged += HandleStruckChanged;
        Apply();
    }

    private void OnDisable()
    {
        if (m_beacon == null)
        {
            return;
        }

        m_beacon.Remaining.OnValueChanged -= HandleRemainingChanged;
        m_beacon.HasStruck.OnValueChanged -= HandleStruckChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (m_beacon != null)
        {
            Apply();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (m_warning != null)
        {
            m_warning.DetachAndFadeOut();
            m_warning = null;
        }
    }

    private void HandleRemainingChanged(float previous, float current)
    {
        if (current > 0f && Mathf.CeilToInt(previous) != Mathf.CeilToInt(current))
        {
            SfxPlayer.Play(m_tickCue, transform.position);
        }

        Apply();
    }

    private void HandleStruckChanged(bool previous, bool current)
    {
        if (current && !previous)
        {
            Vfx.Spawn(m_explosionVfx, transform.position, Vector3.up);
            SfxPlayer.Play(m_explosionCue, transform.position);
            if (m_impulse != null)
            {
                m_impulse.GenerateImpulseAt(transform.position, Vector3.down * m_impulseForce);
            }
        }

        Apply();
    }

    private void Apply()
    {
        if (m_warning != null)
        {
            m_warning.SetPlaying(!m_beacon.HasStruck.Value && m_beacon.Remaining.Value > 0f);
        }

        if (m_renderer == null)
        {
            return;
        }

        Color color;
        if (m_beacon.HasStruck.Value)
        {
            color = m_struckColor;
        }
        else
        {
            float warning = Mathf.Max(0.01f, m_beacon.WarningSeconds);
            color = Color.Lerp(m_imminentColor, m_armedColor, m_beacon.Remaining.Value / warning);
        }

        m_renderer.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, color);
        m_renderer.SetPropertyBlock(m_block);
    }
}
}
