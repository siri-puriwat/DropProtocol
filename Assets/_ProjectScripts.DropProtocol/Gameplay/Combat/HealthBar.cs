using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Presentation: overhead bar driven by replicated <see cref="Health" />. Flashing on a decrease is the
///     hit marker; a red track with no fill is the downed marker. Never writes gameplay.
/// </summary>
public sealed class HealthBar : NetworkBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private Health m_health;

    [SerializeField]
    private Transform m_fill;

    [SerializeField]
    private Renderer m_fillRenderer;

    [SerializeField]
    private Renderer m_trackRenderer;

    [SerializeField]
    private Color m_fullColor = new(0.35f, 0.9f, 0.45f);

    [SerializeField]
    private Color m_emptyColor = new(1f, 0.4f, 0.35f);

    [SerializeField]
    private Color m_flashColor = Color.white;

    [SerializeField]
    private Color m_trackColor = new(0.12f, 0.12f, 0.12f);

    [SerializeField]
    private Color m_downedTrackColor = new(0.7f, 0.1f, 0.1f);

    [SerializeField]
    [Min(0f)]
    private float m_flashSeconds = 0.1f;

    private MaterialPropertyBlock m_block;
    private Vector3 m_fullFillScale = Vector3.one;
    private float m_fraction;
    private float m_flashUntil;
    private Color m_lastFill;
    private Color m_lastTrack;

    private void Awake()
    {
        m_block = new MaterialPropertyBlock();
        if (m_fill != null)
        {
            m_fullFillScale = m_fill.localScale;
        }
    }

    private void LateUpdate()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            transform.rotation = camera.transform.rotation;
        }

        bool downed = m_health != null && m_health.IsDowned;
        bool flashing = Time.time < m_flashUntil;
        var fill = flashing ? m_flashColor : Color.Lerp(m_emptyColor, m_fullColor, m_fraction);
        var track = downed ? m_downedTrackColor : m_trackColor;

        SetColor(m_fillRenderer, fill, ref m_lastFill);
        SetColor(m_trackRenderer, track, ref m_lastTrack);
    }

    private void OnEnable()
    {
        if (m_health == null)
        {
            return;
        }

        m_health.Current.OnValueChanged += HandleHealthChanged;
        Apply(m_health.Current.Value);
    }

    private void OnDisable()
    {
        if (m_health != null)
        {
            m_health.Current.OnValueChanged -= HandleHealthChanged;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (m_health != null)
        {
            Apply(m_health.Current.Value);
        }
    }

    private void HandleHealthChanged(int previous, int current)
    {
        if (current < previous)
        {
            m_flashUntil = Time.time + m_flashSeconds;
        }

        Apply(current);
    }

    private void Apply(int current)
    {
        m_fraction = m_health.Max > 0 ? Mathf.Clamp01((float)current / m_health.Max) : 0f;
        if (m_fill == null)
        {
            return;
        }

        // The quad is centred, so shift it to keep the left edge anchored while it shrinks.
        float width = m_fullFillScale.x;
        m_fill.localScale = new Vector3(width * m_fraction, m_fullFillScale.y, m_fullFillScale.z);
        m_fill.localPosition = new Vector3(-(1f - m_fraction) * width * 0.5f, 0f, -0.001f);
    }

    private void SetColor(Renderer renderer, Color color, ref Color last)
    {
        if (renderer == null || color == last)
        {
            return;
        }

        last = color;
        renderer.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, color);
        renderer.SetPropertyBlock(m_block);
    }
}
}
