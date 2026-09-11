using System;
using Unity.Netcode;

namespace DropProtocol
{
/// <summary>
///     Fixed three-slot cooldown block replicated as one NetworkVariable; Netcode does not discover arrays of
///     NetworkVariables, and a memcpy struct needs no serializer code.
/// </summary>
public struct ProtocolCooldowns : INetworkSerializeByMemcpy, IEquatable<ProtocolCooldowns>
{
    public const int SlotCount = 3;

    public float Slot0;
    public float Slot1;
    public float Slot2;

    public float this[int index]
    {
        get => index switch
        {
            0 => Slot0,
            1 => Slot1,
            2 => Slot2,
            _ => throw new IndexOutOfRangeException()
        };
        set
        {
            switch (index)
            {
                case 0:
                    Slot0 = value;
                    break;
                case 1:
                    Slot1 = value;
                    break;
                case 2:
                    Slot2 = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public bool Equals(ProtocolCooldowns other)
    {
        return Slot0.Equals(other.Slot0) && Slot1.Equals(other.Slot1) && Slot2.Equals(other.Slot2);
    }

    public override bool Equals(object obj)
    {
        return obj is ProtocolCooldowns other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Slot0, Slot1, Slot2);
    }
}
}
