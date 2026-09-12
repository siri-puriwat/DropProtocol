using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     A looping source that fades in and out instead of starting and stopping hard. Lives on its own child
///     object so a despawning owner can leave it behind to finish the fade.
/// </summary>
public sealed class LoopFader : MonoBehaviour
{
    [SerializeField]
    private AudioSource m_source;

    [SerializeField]
    [Min(0.01f)]
    private float m_fadeSeconds = 0.3f;

    [SerializeField]
    [Range(0f, 1f)]
    private float m_volume = 0.5f;

    [Tooltip("Scene ambience: start looping as soon as the object exists.")]
    [SerializeField]
    private bool m_playOnStart;

    private bool m_playing;
    private bool m_destroyWhenSilent;

    public bool IsPlaying => m_playing;

    private void Awake()
    {
        if (m_source == null)
        {
            m_source = GetComponent<AudioSource>();
        }

        if (m_source != null)
        {
            m_source.loop = true;
            m_source.playOnAwake = false;
            m_source.volume = 0f;
        }
    }

    private void Start()
    {
        if (m_playOnStart)
        {
            SetPlaying(true);
        }
    }

    public void SetPlaying(bool playing)
    {
        if (m_source == null || m_playing == playing)
        {
            return;
        }

        m_playing = playing;
        if (playing && !m_source.isPlaying)
        {
            m_source.volume = 0f;
            m_source.Play();
        }
    }

    public void SetPitch(float pitch)
    {
        if (m_source != null)
        {
            m_source.pitch = pitch;
        }
    }

    /// <summary>Leaves the owner so a despawn does not cut the loop; the object destroys itself once silent.</summary>
    public void DetachAndFadeOut()
    {
        transform.SetParent(null, true);
        m_destroyWhenSilent = true;
        if (m_source == null || !m_source.isPlaying)
        {
            Destroy(gameObject);
            return;
        }

        SetPlaying(false);
    }

    private void Update()
    {
        if (m_source == null)
        {
            return;
        }

        float target = m_playing ? m_volume : 0f;
        float step = Mathf.Max(m_volume, 0.01f) / m_fadeSeconds * Time.deltaTime;
        m_source.volume = Mathf.MoveTowards(m_source.volume, target, step);

        if (m_playing || m_source.volume > 0f)
        {
            return;
        }

        if (m_source.isPlaying)
        {
            m_source.Stop();
        }

        if (m_destroyWhenSilent)
        {
            Destroy(gameObject);
        }
    }
}
}
