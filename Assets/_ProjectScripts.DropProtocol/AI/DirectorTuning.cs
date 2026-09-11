using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Threat budget knobs for <see cref="DirectorState" />; one block in the inspector.</summary>
[Serializable]
public struct DirectorTuning
{
    [Min(0f)]
    public float BaseBudgetPerSecond;

    [Min(0f)]
    public float BudgetPerExtraPlayer;

    [Min(1f)]
    public float MaxBudget;

    [Min(0)]
    public int BasePopulationCap;

    [Min(0)]
    public int PopulationPerExtraPlayer;

    [Min(0.05f)]
    public float ThinkSeconds;

    [Min(0f)]
    public float MinSpawnDistance;

    public int Seed;

    public static DirectorTuning Default => new()
    {
        BaseBudgetPerSecond = 0.5f,
        BudgetPerExtraPlayer = 0.25f,
        MaxBudget = 12f,
        BasePopulationCap = 6,
        PopulationPerExtraPlayer = 2,
        ThinkSeconds = 1f,
        MinSpawnDistance = 10f,
        Seed = 1234
    };
}
}
