using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Presentation: lays the visual child down while downed. Only the child moves, so NetworkTransform on
///     the root is never fought over.
/// </summary>
public sealed class DownedPose : MonoBehaviour
{
    [SerializeField]
    private Health m_health;

    [SerializeField]
    private Transform m_visual;

    [SerializeField]
    private Vector3 m_downedEuler = new(90f, 0f, 0f);

    [SerializeField]
    private Vector3 m_downedOffset = new(0f, 0.25f, 0f);

    private Vector3 m_standingPosition;
    private Quaternion m_standingRotation;

    private void Awake()
    {
        if (m_visual == null)
        {
            return;
        }

        m_standingPosition = m_visual.localPosition;
        m_standingRotation = m_visual.localRotation;
    }

    private void OnEnable()
    {
        if (m_health == null)
        {
            return;
        }

        m_health.Current.OnValueChanged += HandleHealthChanged;
        Apply(m_health.IsDowned);
    }

    private void OnDisable()
    {
        if (m_health != null)
        {
            m_health.Current.OnValueChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int previous, int current)
    {
        Apply(current <= 0);
    }

    private void Apply(bool downed)
    {
        if (m_visual == null)
        {
            return;
        }

        m_visual.localRotation = downed ? Quaternion.Euler(m_downedEuler) : m_standingRotation;
        m_visual.localPosition = downed ? m_standingPosition + m_downedOffset : m_standingPosition;
    }
}
}
