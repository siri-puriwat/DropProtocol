using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Colours a supply pod by whether it has arrived. Presentation only.</summary>
public sealed class SupplyIndicator : NetworkBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private SupplyPod m_pod;

    [SerializeField]
    private Renderer m_renderer;

    [SerializeField]
    private Color m_incomingColor = new(0.4f, 0.4f, 0.45f);

    [SerializeField]
    private Color m_openColor = new(0.3f, 0.9f, 1f);

    [SerializeField]
    private GameObject m_landingVfx;

    [SerializeField]
    private AudioClip m_landingClip;

    private MaterialPropertyBlock m_block;

    private void Awake()
    {
        m_block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (m_pod == null)
        {
            return;
        }

        m_pod.IsOpen.OnValueChanged += HandleOpenChanged;
        Apply();
    }

    private void OnDisable()
    {
        if (m_pod == null)
        {
            return;
        }

        m_pod.IsOpen.OnValueChanged -= HandleOpenChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (m_pod != null)
        {
            Apply();
        }
    }

    private void HandleOpenChanged(bool previous, bool current)
    {
        if (current && !previous)
        {
            Vfx.Spawn(m_landingVfx, transform.position, Vector3.up);
            SfxPlayer.Play(m_landingClip, transform.position);
        }

        Apply();
    }

    private void Apply()
    {
        if (m_renderer == null)
        {
            return;
        }

        m_renderer.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, m_pod.IsOpen.Value ? m_openColor : m_incomingColor);
        m_renderer.SetPropertyBlock(m_block);
    }
}
}
