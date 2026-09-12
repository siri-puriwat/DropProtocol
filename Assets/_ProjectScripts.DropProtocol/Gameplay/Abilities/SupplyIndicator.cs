using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Colours a supply pod by whether it has arrived and opens its lid. The lid pose is state: a late joiner
///     sees it already open instead of watching it open. Presentation only.
/// </summary>
public sealed class SupplyIndicator : NetworkBehaviour
{
    private static readonly int IsOpenId = Animator.StringToHash("IsOpen");

    [SerializeField]
    private SupplyPod m_pod;

    [SerializeField]
    private Renderer[] m_renderers = System.Array.Empty<Renderer>();

    [SerializeField]
    private Animator m_animator;

    [Tooltip("Animator state to snap to when the pod is already open on spawn.")]
    [SerializeField]
    private string m_openState = "Open";

    [SerializeField]
    private Color m_incomingColor = new(0.4f, 0.4f, 0.45f);

    [SerializeField]
    private Color m_openColor = new(0.3f, 0.9f, 1f);

    [SerializeField]
    private GameObject m_landingVfx;

    [SerializeField]
    private SfxCue m_landingCue = SfxCue.Default;

    [Tooltip("Loops while the pod is still falling.")]
    [SerializeField]
    private LoopFader m_descent;

    private RendererTint m_tint;

    private void Awake()
    {
        m_tint = new RendererTint(m_renderers);
    }

    private void OnEnable()
    {
        if (m_pod == null)
        {
            return;
        }

        m_pod.IsOpen.OnValueChanged += HandleOpenChanged;
        Apply(false);
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
            Apply(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (m_descent != null)
        {
            m_descent.DetachAndFadeOut();
            m_descent = null;
        }
    }

    private void HandleOpenChanged(bool previous, bool current)
    {
        if (current && !previous)
        {
            Vfx.Spawn(m_landingVfx, transform.position, Vector3.up);
            SfxPlayer.Play(m_landingCue, transform.position);
        }

        Apply(true);
    }

    private void Apply(bool animate)
    {
        bool open = m_pod.IsOpen.Value;
        if (m_descent != null)
        {
            m_descent.SetPlaying(!open);
        }

        if (m_animator != null)
        {
            m_animator.SetBool(IsOpenId, open);
            if (open && !animate)
            {
                m_animator.Play(m_openState, 0, 1f);
            }
        }

        m_tint.Apply(open ? m_openColor : m_incomingColor);
    }
}
}
