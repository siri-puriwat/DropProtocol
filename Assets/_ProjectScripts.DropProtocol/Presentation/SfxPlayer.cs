using UnityEngine;
using UnityEngine.Audio;

namespace DropProtocol
{
/// <summary>
///     Positional one-shot sounds through a <see cref="VoiceRing" /> on the SFX mixer group. Exists because
///     <c>AudioSource.PlayClipAtPoint</c> cannot route to a mixer group. Missing clips and a missing player
///     are both silent no-ops so tests and code-built scenes need no audio setup. Loops never go through
///     here; they own an <see cref="AudioSource" /> behind a <see cref="LoopFader" />.
/// </summary>
public sealed class SfxPlayer : MonoBehaviour
{
    private const int OneShotPriority = 128;

    [SerializeField]
    private AudioMixerGroup m_group;

    [SerializeField]
    [Range(1, 32)]
    private int m_voices = 12;

    [SerializeField]
    [Min(0f)]
    private float m_minDistance = 4f;

    [SerializeField]
    [Min(1f)]
    private float m_maxDistance = 45f;

    private VoiceRing m_ring;

    private static SfxPlayer m_instance;

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(this);
            return;
        }

        m_instance = this;
        m_ring = new VoiceRing(transform, "Voice", m_voices, m_group, true, m_minDistance, m_maxDistance, OneShotPriority);
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
        if (clip == null || m_instance == null || m_instance.m_ring == null)
        {
            return;
        }

        m_instance.m_ring.Play(clip, position, volume, 1f);
    }

    public static void Play(in SfxCue cue, Vector3 position)
    {
        if (cue.IsEmpty || m_instance == null || m_instance.m_ring == null)
        {
            return;
        }

        var clip = cue.Clips[SfxCueRules.Pick(cue.Clips.Length, Random.value)];
        float pitch = SfxCueRules.Pitch(cue.PitchRange, Random.value);
        m_instance.m_ring.Play(clip, position, cue.Volume, pitch);
    }
}
}
