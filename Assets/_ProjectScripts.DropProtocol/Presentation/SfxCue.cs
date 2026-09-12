using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     A one-shot sound as data: one of several clips at a random pitch. An empty cue is silent, which is what
///     tests and code-built scenes rely on.
/// </summary>
[Serializable]
public struct SfxCue
{
    public AudioClip[] Clips;

    [Range(0f, 1f)]
    public float Volume;

    [Tooltip("Random pitch between x and y. Both zero means 1.")]
    public Vector2 PitchRange;

    public static SfxCue Default => new SfxCue { Clips = Array.Empty<AudioClip>(), Volume = 1f, PitchRange = Vector2.one };

    public static SfxCue Single(AudioClip clip)
    {
        return new SfxCue { Clips = clip != null ? new[] { clip } : Array.Empty<AudioClip>(), Volume = 1f, PitchRange = Vector2.one };
    }

    public bool IsEmpty => SfxCueRules.IsEmpty(Clips);
}
}
