using UnityEngine;

namespace DropProtocol
{
/// <summary>Pure cue and voice selection so the audio helpers stay testable without a scene.</summary>
public static class SfxCueRules
{
    public static bool IsEmpty(AudioClip[] clips)
    {
        if (clips == null)
        {
            return true;
        }

        foreach (var clip in clips)
        {
            if (clip != null)
            {
                return false;
            }
        }

        return true;
    }

    public static int Pick(int count, float random01)
    {
        if (count <= 0)
        {
            return -1;
        }

        int index = (int)(Mathf.Clamp01(random01) * count);
        return Mathf.Min(index, count - 1);
    }

    public static float Pitch(Vector2 range, float random01)
    {
        if (range.x <= 0f && range.y <= 0f)
        {
            return 1f;
        }

        float min = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(range.x, range.y);
        return Mathf.Lerp(min, max, Mathf.Clamp01(random01));
    }

    /// <summary>The first idle voice, else the one that started longest ago.</summary>
    public static int PickVoice(bool[] playing, double[] startedAt)
    {
        if (playing == null || playing.Length == 0)
        {
            return -1;
        }

        int oldest = 0;
        for (int i = 0; i < playing.Length; i++)
        {
            if (!playing[i])
            {
                return i;
            }

            if (startedAt != null && i < startedAt.Length && startedAt[i] < startedAt[oldest])
            {
                oldest = i;
            }
        }

        return oldest;
    }
}
}
