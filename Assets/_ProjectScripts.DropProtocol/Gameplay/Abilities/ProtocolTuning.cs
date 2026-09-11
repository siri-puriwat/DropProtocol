using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Knobs shared by every protocol slot on a character; one block in the inspector.</summary>
[Serializable]
public struct ProtocolTuning
{
    [Tooltip("Seconds without a new direction before a partly entered sequence is discarded.")]
    [Min(0.1f)]
    public float InputTimeoutSeconds;

    [Tooltip("Metres in front of the caller where the payload lands.")]
    [Min(0f)]
    public float ThrowDistance;

    public static ProtocolTuning Default => new()
    {
        InputTimeoutSeconds = 1.5f,
        ThrowDistance = 5f
    };
}
}
