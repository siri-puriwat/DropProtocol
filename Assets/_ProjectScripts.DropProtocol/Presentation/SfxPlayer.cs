using UnityEngine;
using UnityEngine.Audio;

namespace DropProtocol
{
/// <summary>
///     Positional one-shot sounds through a small ring of sources. Exists because
///     <c>AudioSource.PlayClipAtPoint</c> cannot route to a mixer group. Missing clips and a missing player
///     are both silent no-ops so tests and code-built scenes need no audio setup.
/// </summary>
public sealed class SfxPlayer : MonoBehaviour
{
    [SerializeField]
    private AudioMixerGroup m_group;

    [SerializeField]
    [Range(1, 32)]
    private int m_voices = 8;

    [SerializeField]
    [Min(0f)]
    private float m_minDistance = 4f;

    [SerializeField]
    [Min(1f)]
    private float m_maxDistance = 45f;

    private AudioSource[] m_sources;
    private int m_next;

    private static SfxPlayer m_instance;

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(this);
            return;
        }

        m_instance = this;
        m_sources = new AudioSource[m_voices];
        for (int i = 0; i < m_voices; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = m_minDistance;
            source.maxDistance = m_maxDistance;
            source.outputAudioMixerGroup = m_group;
            m_sources[i] = source;
        }
    }

    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null;
        }
    }

    // Statics survive play sessions when domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        m_instance = null;
    }

    public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null || m_instance == null || m_instance.m_sources == null)
        {
            return;
        }

        var source = m_instance.m_sources[m_instance.m_next];
        m_instance.m_next = (m_instance.m_next + 1) % m_instance.m_sources.Length;

        source.transform.position = position;
        source.PlayOneShot(clip, volume);
    }
}
}
