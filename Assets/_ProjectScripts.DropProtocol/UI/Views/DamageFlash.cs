using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>Full-screen vignette that flashes on damage. No direction: the attacker is not replicated.</summary>
public sealed class DamageFlash : HudView
{
    [SerializeField]
    private Image m_image;

    [SerializeField]
    [Range(0f, 1f)]
    private float m_peakAlpha = 0.22f;

    [SerializeField]
    [Min(0.01f)]
    private float m_fadeSeconds = 0.4f;

    [SerializeField]
    private CinemachineImpulseSource m_impulse;

    [Tooltip("Camera kick per point of damage taken by the local player.")]
    [SerializeField]
    [Min(0f)]
    private float m_impulsePerDamage = 0.02f;

    private Health m_health;
    private float m_alpha;

    public float Alpha => m_alpha;

    protected override void OnBind(NetworkPlayer player)
    {
        m_health = player.Health;
        if (m_health != null)
        {
            m_health.Current.OnValueChanged += HandleHealthChanged;
        }
    }

    protected override void OnUnbind(NetworkPlayer player)
    {
        if (m_health != null)
        {
            m_health.Current.OnValueChanged -= HandleHealthChanged;
        }

        m_health = null;
        m_alpha = 0f;
        Apply();
    }

    private void Update()
    {
        if (m_alpha <= 0f)
        {
            return;
        }

        m_alpha = Mathf.Max(0f, m_alpha - Time.deltaTime * m_peakAlpha / m_fadeSeconds);
        Apply();
    }

    private void HandleHealthChanged(int previous, int current)
    {
        if (current < previous)
        {
            m_alpha = m_peakAlpha;
            Apply();
            if (m_impulse != null)
            {
                m_impulse.GenerateImpulse(Vector3.down * ((previous - current) * m_impulsePerDamage));
            }
        }
    }

    private void Apply()
    {
        if (m_image == null)
        {
            return;
        }

        var color = m_image.color;
        color.a = m_alpha;
        m_image.color = color;
        m_image.enabled = m_alpha > 0f;
    }
}
}
