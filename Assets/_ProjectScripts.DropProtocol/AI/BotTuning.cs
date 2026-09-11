using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Squad bot knobs for <see cref="BotBrain" /> and <see cref="BotCommandSource" />; one block in the inspector.</summary>
[Serializable]
public struct BotTuning
{
    [Min(0.05f)]
    public float ThinkSeconds;

    [Min(0f)]
    public float FollowStopRadius;

    [Min(0f)]
    public float FollowResumeRadius;

    [Min(0f)]
    public float EngageRange;

    [Min(0f)]
    public float DisengageRange;

    [Min(0f)]
    public float RetreatRange;

    [Min(0f)]
    public float RetreatStep;

    [Min(0f)]
    public float ReviveSearchRange;

    [Min(0f)]
    public float ReviveDangerRange;

    [Min(0f)]
    public float ReviveAbortRange;

    [Min(0.1f)]
    public float ReviveReach;

    [Tooltip("A bot works an objective only while its leader is this close to it, so it never runs off alone.")]
    [Min(0f)]
    public float ObjectiveAssistRange;

    [Min(0.1f)]
    public float ObjectiveReach;

    public static BotTuning Default => new()
    {
        ThinkSeconds = 0.2f,
        FollowStopRadius = 3f,
        FollowResumeRadius = 4.5f,
        EngageRange = 25f,
        DisengageRange = 30f,
        RetreatRange = 4f,
        RetreatStep = 3f,
        ReviveSearchRange = 30f,
        ReviveDangerRange = 8f,
        ReviveAbortRange = 4f,
        ReviveReach = 1.5f,
        ObjectiveAssistRange = 12f,
        ObjectiveReach = 1.5f
    };
}
}
