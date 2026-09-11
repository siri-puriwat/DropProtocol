using System;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     One sample of intent for a player character. Produced by any <see cref="IPlayerCommandSource" />
///     (human input, network, bot, test) and consumed by <see cref="PlayerCharacter" />.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Move" /> and <see cref="Aim" /> are world-space XZ vectors: x maps to world X and
///         y maps to world Z. Any camera-relative conversion is the producer's job, so the character never
///         needs to know about cameras.
///     </para>
///     <para>
///         Buttons are held state for this sample, not edge events. Consumers do their own edge
///         detection so a command stays meaningful when sampled at a network tick rate.
///     </para>
///     <para>
///         The struct serializes itself for Netcode so a client can send it to the host unchanged:
///         two vectors plus the buttons packed into one byte.
///     </para>
/// </remarks>
public struct PlayerCommand : INetworkSerializable
{
    /// <summary>World-space XZ movement direction, magnitude 0..1.</summary>
    public Vector2 Move;

    /// <summary>World-space XZ aim direction (unit length), or zero to keep the current facing.</summary>
    public Vector2 Aim;

    public bool Fire;
    public bool Reload;
    public bool Interact;

    /// <summary>A command that asks the character to do nothing.</summary>
    public static PlayerCommand None => default;

    public bool HasMove => Move.sqrMagnitude > 0f;
    public bool HasAim => Aim.sqrMagnitude > 0f;

    [Flags]
    private enum ButtonFlags : byte
    {
        None = 0,
        Fire = 1 << 0,
        Reload = 1 << 1,
        Interact = 1 << 2
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Move);
        serializer.SerializeValue(ref Aim);

        byte buttons = (byte)PackButtons();
        serializer.SerializeValue(ref buttons);
        UnpackButtons((ButtonFlags)buttons);
    }

    private ButtonFlags PackButtons()
    {
        var flags = ButtonFlags.None;
        if (Fire)
        {
            flags |= ButtonFlags.Fire;
        }

        if (Reload)
        {
            flags |= ButtonFlags.Reload;
        }

        if (Interact)
        {
            flags |= ButtonFlags.Interact;
        }

        return flags;
    }

    private void UnpackButtons(ButtonFlags flags)
    {
        Fire = (flags & ButtonFlags.Fire) != 0;
        Reload = (flags & ButtonFlags.Reload) != 0;
        Interact = (flags & ButtonFlags.Interact) != 0;
    }
}
}
