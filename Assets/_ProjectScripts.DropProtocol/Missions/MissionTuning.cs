using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Mission knobs for <see cref="MissionState" /> and <see cref="MissionDirector" />; one block in the inspector.</summary>
[Serializable]
public struct MissionTuning
{
    [Min(0f)]
    public float DeploySeconds;

    [Tooltip("Seconds from the start of Active until the mission fails; 0 disables the limit.")]
    [Min(0f)]
    public float TimeLimitSeconds;

    [Min(0f)]
    public float ExtractionSeconds;

    [Min(0f)]
    public float IntensityPerRelay;

    [Min(0f)]
    public float ExtractionIntensity;

    public static MissionTuning Default => new()
    {
        DeploySeconds = 5f,
        TimeLimitSeconds = 0f,
        ExtractionSeconds = 8f,
        IntensityPerRelay = 0.5f,
        ExtractionIntensity = 3f
    };
}
}
