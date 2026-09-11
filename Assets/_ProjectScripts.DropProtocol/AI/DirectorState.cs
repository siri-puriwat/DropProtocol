using System;
using System.Collections.Generic;

namespace DropProtocol
{
/// <summary>
///     Threat budget bookkeeping. Rolls the next archetype once and then saves up for it, so expensive
///     enemies are not starved by cheap ones. Time and randomness are injected for deterministic tests.
/// </summary>
public sealed class DirectorState
{
    private readonly IReadOnlyList<int> m_costs;
    private readonly IReadOnlyList<int> m_weights;
    private readonly DirectorTuning m_tuning;
    private readonly Func<double> m_nextRoll;

    public DirectorState(IReadOnlyList<int> costs, IReadOnlyList<int> weights, DirectorTuning tuning,
        Func<double> nextRoll)
    {
        if (costs.Count != weights.Count)
        {
            throw new ArgumentException("costs and weights must line up");
        }

        m_costs = costs;
        m_weights = weights;
        m_tuning = tuning;
        m_nextRoll = nextRoll;
        DesiredIndex = -1;
        Intensity = 1f;
    }

    public float Budget { get; private set; }
    public int DesiredIndex { get; private set; }

    /// <summary>Mission-progression multiplier on budget income; 1 until a mission drives it.</summary>
    public float Intensity { get; set; }

    /// <summary>Advances the budget and returns the archetype index to spawn now, or -1.</summary>
    public int Step(int playerCount, int population, float deltaTime)
    {
        float rate = DirectorRules.BudgetPerSecond(playerCount, m_tuning.BaseBudgetPerSecond,
            m_tuning.BudgetPerExtraPlayer, Intensity);
        Budget = DirectorRules.Accrue(Budget, rate, deltaTime, m_tuning.MaxBudget);

        if (population >=
            DirectorRules.PopulationCap(playerCount, m_tuning.BasePopulationCap, m_tuning.PopulationPerExtraPlayer))
        {
            return -1;
        }

        if (DesiredIndex < 0)
        {
            DesiredIndex = DirectorRules.PickWeighted(m_weights, m_nextRoll());
        }

        if (DesiredIndex < 0 || !DirectorRules.CanAfford(Budget, m_costs[DesiredIndex]))
        {
            return -1;
        }

        int spawned = DesiredIndex;
        Budget -= m_costs[spawned];
        DesiredIndex = -1;
        return spawned;
    }
}
}
