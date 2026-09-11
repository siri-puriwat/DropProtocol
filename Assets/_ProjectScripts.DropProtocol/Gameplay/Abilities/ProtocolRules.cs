using System.Collections.Generic;
using UnityEngine;

namespace DropProtocol
{
public enum ProtocolMatch
{
    Pending,
    Matched,
    Rejected
}

/// <summary>Pure sequence, cooldown and placement rules for support protocols. Static so they test without a scene.</summary>
public static class ProtocolRules
{
    public const int MaxSequenceLength = 8;
    public const float PressThreshold = 0.5f;

    private const int CountBits = 4;
    private const int CountMask = (1 << CountBits) - 1;
    private const int DirectionBits = 2;
    private const int DirectionMask = (1 << DirectionBits) - 1;

    /// <summary>Rising edge of a digital direction vector. Vertical wins when both axes rise in the same sample.</summary>
    public static bool TryPressedDirection(Vector2 previous, Vector2 current, out ProtocolDirection direction)
    {
        if (current.y >= PressThreshold && previous.y < PressThreshold)
        {
            direction = ProtocolDirection.Up;
            return true;
        }

        if (current.y <= -PressThreshold && previous.y > -PressThreshold)
        {
            direction = ProtocolDirection.Down;
            return true;
        }

        if (current.x >= PressThreshold && previous.x < PressThreshold)
        {
            direction = ProtocolDirection.Right;
            return true;
        }

        if (current.x <= -PressThreshold && previous.x > -PressThreshold)
        {
            direction = ProtocolDirection.Left;
            return true;
        }

        direction = default;
        return false;
    }

    public static ProtocolMatch Match(IReadOnlyList<ProtocolDirection[]> sequences,
        IReadOnlyList<ProtocolDirection> entered, out int slot)
    {
        slot = -1;
        if (entered.Count == 0)
        {
            return ProtocolMatch.Pending;
        }

        bool anyPrefix = false;
        for (int i = 0; i < sequences.Count; i++)
        {
            var sequence = sequences[i];
            if (sequence == null || sequence.Length == 0 || !StartsWith(sequence, entered))
            {
                continue;
            }

            if (sequence.Length == entered.Count)
            {
                slot = i;
                return ProtocolMatch.Matched;
            }

            anyPrefix = true;
        }

        return anyPrefix ? ProtocolMatch.Pending : ProtocolMatch.Rejected;
    }

    /// <summary>No sequence may be a prefix of (or equal to) another, or the shorter one could never be told apart.</summary>
    public static bool IsPrefixFree(IReadOnlyList<ProtocolDirection[]> sequences)
    {
        for (int i = 0; i < sequences.Count; i++)
        {
            for (int j = 0; j < sequences.Count; j++)
            {
                if (i != j && StartsWith(sequences[j], sequences[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool HasTimedOut(double lastPushAt, double now, float timeoutSeconds)
    {
        return now - lastPushAt > timeoutSeconds;
    }

    public static bool CanCall(bool downed, float cooldownRemaining, bool missionOpen)
    {
        return !downed && cooldownRemaining <= 0f && missionOpen;
    }

    public static float CooldownStep(float remaining, float deltaTime)
    {
        return Mathf.Max(0f, remaining - deltaTime);
    }

    public static Vector3 TargetPoint(Vector3 position, Vector3 forward, float throwDistance)
    {
        var flat = new Vector3(forward.x, 0f, forward.z);
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = Vector3.forward;
        }

        return position + flat.normalized * throwDistance;
    }

    /// <summary>Count in the low four bits, then two bits per direction, so the entered prefix fits one replicated int.</summary>
    public static int Pack(IReadOnlyList<ProtocolDirection> entered)
    {
        int count = Mathf.Min(entered.Count, MaxSequenceLength);
        int packed = count;
        for (int i = 0; i < count; i++)
        {
            packed |= (int)entered[i] << (CountBits + i * DirectionBits);
        }

        return packed;
    }

    public static int Unpack(int packed, ProtocolDirection[] into)
    {
        int count = Mathf.Min(packed & CountMask, Mathf.Min(into.Length, MaxSequenceLength));
        for (int i = 0; i < count; i++)
        {
            into[i] = (ProtocolDirection)((packed >> (CountBits + i * DirectionBits)) & DirectionMask);
        }

        return count;
    }

    private static bool StartsWith(ProtocolDirection[] sequence, IReadOnlyList<ProtocolDirection> prefix)
    {
        if (prefix.Count > sequence.Length)
        {
            return false;
        }

        for (int i = 0; i < prefix.Count; i++)
        {
            if (sequence[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }
}
}
