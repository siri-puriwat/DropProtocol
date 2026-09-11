using System.Text;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Pure formatting for the HUD so the strings are testable without a canvas.</summary>
public static class HudFormat
{
    private static readonly string[] Glyphs = { "↑", "↓", "←", "→" };
    private static readonly StringBuilder Builder = new();

    public static string Clock(float elapsed, float limit)
    {
        float shown = limit > 0f ? Mathf.Max(0f, limit - elapsed) : Mathf.Max(0f, elapsed);
        int minutes = Mathf.FloorToInt(shown / 60f);
        int seconds = Mathf.FloorToInt(shown % 60f);
        return $"{minutes:0}:{seconds:00}";
    }

    public static string CooldownLabel(float remaining)
    {
        return remaining > 0f ? $"{Mathf.CeilToInt(remaining)} s" : "READY";
    }

    public static float HealthFraction(int current, int max)
    {
        return max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }

    public static string HealthLabel(int current, int max)
    {
        return $"{Mathf.Max(0, current)} / {max}";
    }

    public static string AmmoLabel(int ammo, int magazine, bool reloading)
    {
        return reloading ? "RELOADING" : $"{ammo} / {magazine}";
    }

    public static string PhaseLabel(MissionPhase phase)
    {
        switch (phase)
        {
            case MissionPhase.Deployment:
                return "DEPLOYING";
            case MissionPhase.Active:
                return "ACTIVATE RELAYS";
            case MissionPhase.Extraction:
                return "EXTRACT";
            case MissionPhase.Complete:
                return "COMPLETE";
            case MissionPhase.Failed:
                return "FAILED";
            default:
                return phase.ToString().ToUpperInvariant();
        }
    }

    public static string ResultTitle(MissionPhase phase, MissionOutcome outcome)
    {
        if (phase == MissionPhase.Complete)
        {
            return "MISSION COMPLETE";
        }

        if (phase != MissionPhase.Failed)
        {
            return string.Empty;
        }

        return outcome == MissionOutcome.TimedOut ? "MISSION FAILED\nTime expired" : "MISSION FAILED\nSquad wiped";
    }

    public static string SquadName(int slot, bool isBot)
    {
        string tag = slot >= 0 ? $"P{slot + 1}" : "P?";
        return isBot ? tag + " BOT" : tag;
    }

    /// <summary>
    ///     Glyphs for a sequence with the prefix the host has accepted wrapped in a rich-text colour, so the
    ///     player sees how far the entry got. <paramref name="entered" /> holds the unpacked entered directions.
    /// </summary>
    public static string SequenceGlyphs(ProtocolDirection[] sequence, ProtocolDirection[] entered, int enteredCount,
        string highlightHex)
    {
        Builder.Clear();
        int matched = MatchedPrefix(sequence, entered, enteredCount);
        for (int i = 0; i < sequence.Length; i++)
        {
            if (i == 0 && matched > 0)
            {
                Builder.Append("<color=").Append(highlightHex).Append('>');
            }

            Builder.Append(Glyphs[(int)sequence[i]]);

            if (i == matched - 1)
            {
                Builder.Append("</color>");
            }
        }

        return Builder.ToString();
    }

    public static int MatchedPrefix(ProtocolDirection[] sequence, ProtocolDirection[] entered, int enteredCount)
    {
        if (enteredCount <= 0 || enteredCount > sequence.Length)
        {
            return 0;
        }

        for (int i = 0; i < enteredCount; i++)
        {
            if (sequence[i] != entered[i])
            {
                return 0;
            }
        }

        return enteredCount;
    }
}
}
