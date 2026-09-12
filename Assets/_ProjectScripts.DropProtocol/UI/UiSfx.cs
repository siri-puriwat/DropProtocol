using UnityEngine;
using UnityEngine.Audio;

namespace DropProtocol
{
/// <summary>
///     Non-positional sounds for one scene: HUD cues on the UI group and stingers on the Music group, each on
///     its own <see cref="VoiceRing" /> so a pitched key tick never re-pitches a stinger. Scene-local on
///     purpose: <see cref="SfxPlayer" /> only exists once the network root does, and the menu has no root.
/// </summary>
public sealed class UiSfx : MonoBehaviour
{
    private const int UiPriority = 64;
    private const int MusicPriority = 32;

    [SerializeField]
    private AudioMixerGroup m_uiGroup;

    [SerializeField]
    [Range(1, 8)]
    private int m_uiVoices = 3;

    [SerializeField]
    private AudioMixerGroup m_musicGroup;

    [SerializeField]
    [Range(1, 4)]
    private int m_musicVoices = 1;

    private VoiceRing m_ui;
    private VoiceRing m_music;

    private void Awake()
    {
        m_ui = new VoiceRing(transform, "UiVoice", m_uiVoices, m_uiGroup, false, 1f, 500f, UiPriority);
        m_music = new VoiceRing(transform, "MusicVoice", m_musicVoices, m_musicGroup, false, 1f, 500f, MusicPriority);
    }

    public void PlayUi(AudioClip clip, float pitch = 1f)
    {
        if (clip != null && m_ui != null)
        {
            m_ui.Play(clip, transform.position, 1f, pitch);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && m_music != null)
        {
            m_music.Play(clip, transform.position, 1f, 1f);
        }
    }
}
}
