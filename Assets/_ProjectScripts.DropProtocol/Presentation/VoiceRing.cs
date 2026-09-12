using UnityEngine;
using UnityEngine.Audio;

namespace DropProtocol
{
/// <summary>
///     A pool of one-shot sources, one per child object, so setting position and pitch for a new sound never
///     moves or re-pitches one that is still playing on a shared source.
/// </summary>
public sealed class VoiceRing
{
    private readonly AudioSource[] m_sources;
    private readonly bool[] m_playing;
    private readonly double[] m_startedAt;

    public VoiceRing(Transform parent, string name, int count, AudioMixerGroup group, bool spatial,
        float minDistance, float maxDistance, int priority)
    {
        count = Mathf.Max(1, count);
        m_sources = new AudioSource[count];
        m_playing = new bool[count];
        m_startedAt = new double[count];

        for (int i = 0; i < count; i++)
        {
            var voice = new GameObject(name + i);
            voice.transform.SetParent(parent, false);
            var source = voice.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.outputAudioMixerGroup = group;
            source.priority = priority;
            m_sources[i] = source;
        }
    }

    public int Count => m_sources.Length;

    public AudioSource Play(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        if (clip == null)
        {
            return null;
        }

        for (int i = 0; i < m_sources.Length; i++)
        {
            m_playing[i] = m_sources[i] != null && m_sources[i].isPlaying;
        }

        int index = SfxCueRules.PickVoice(m_playing, m_startedAt);
        var source = m_sources[index];
        if (source == null)
        {
            return null;
        }

        source.transform.position = position;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
        m_startedAt[index] = Time.unscaledTimeAsDouble;
        return source;
    }
}
}
