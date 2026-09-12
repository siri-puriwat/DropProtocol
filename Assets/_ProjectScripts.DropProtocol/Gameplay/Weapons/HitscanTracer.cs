using UnityEngine;

namespace DropProtocol
{
/// <summary>Presentation: draws a brief line for each replicated shot. One renderer is enough at rifle rates.</summary>
public sealed class HitscanTracer : MonoBehaviour
{
    [SerializeField]
    private LineRenderer m_line;

    [Tooltip("Optional barrel tip on this peer; the replicated muzzle is used when empty.")]
    [SerializeField]
    private Transform m_origin;

    [SerializeField]
    [Min(0.01f)]
    private float m_lifetimeSeconds = 0.06f;

    [SerializeField]
    private Color m_missColor = new(1f, 1f, 1f, 0.7f);

    [SerializeField]
    private Color m_hitColor = new(1f, 0.85f, 0.25f);

    private IShotSource m_source;
    private float m_hideAt;

    private void Awake()
    {
        m_source = GetComponent<IShotSource>();
        if (m_line != null)
        {
            m_line.enabled = false;
        }
    }

    private void Update()
    {
        if (m_line != null && m_line.enabled && Time.time >= m_hideAt)
        {
            m_line.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (m_source != null)
        {
            m_source.ShotFired += HandleShotFired;
        }
    }

    private void OnDisable()
    {
        if (m_source != null)
        {
            m_source.ShotFired -= HandleShotFired;
        }
    }

    private void HandleShotFired(Vector3 muzzle, Vector3 end, bool hit)
    {
        if (m_line == null)
        {
            return;
        }

        m_line.SetPosition(0, m_origin != null ? m_origin.position : muzzle);
        m_line.SetPosition(1, end);
        var color = hit ? m_hitColor : m_missColor;
        m_line.startColor = color;
        m_line.endColor = color;
        m_line.enabled = true;
        m_hideAt = Time.time + m_lifetimeSeconds;
    }
}
}
