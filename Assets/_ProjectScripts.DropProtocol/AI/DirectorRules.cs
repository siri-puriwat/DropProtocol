using System.Collections.Generic;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so the threat budget is testable without a scene.</summary>
public static class DirectorRules
{
    public static float BudgetPerSecond(int playerCount, float baseRate, float perExtraPlayer, float intensity)
    {
        return (baseRate + perExtraPlayer * Mathf.Max(0, playerCount - 1)) * intensity;
    }

    public static float Accrue(float budget, float ratePerSecond, float deltaTime, float maxBudget)
    {
        return Mathf.Min(budget + ratePerSecond * deltaTime, maxBudget);
    }

    public static int PopulationCap(int playerCount, int baseCap, int perExtraPlayer)
    {
        return baseCap + perExtraPlayer * Mathf.Max(0, playerCount - 1);
    }

    public static bool CanAfford(float budget, int cost)
    {
        return budget >= cost;
    }

    /// <summary>Weighted pick for a roll in [0, 1); -1 when no weight is positive.</summary>
    public static int PickWeighted(IReadOnlyList<int> weights, double roll)
    {
        int total = 0;
        int last = -1;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] <= 0)
            {
                continue;
            }

            total += weights[i];
            last = i;
        }

        if (total <= 0)
        {
            return -1;
        }

        double target = roll * total;
        double accumulated = 0.0;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] <= 0)
            {
                continue;
            }

            accumulated += weights[i];
            if (target < accumulated)
            {
                return i;
            }
        }

        return last;
    }

    /// <summary>
    ///     Walks the points round-robin from the cursor and returns the first one at least
    ///     minDistance from every player; when none qualifies, the one farthest from the players.
    /// </summary>
    public static int ChooseSpawnPoint(IReadOnlyList<Vector3> points, IReadOnlyList<Vector3> players, float minDistance,
        int cursor)
    {
        if (points.Count == 0)
        {
            return -1;
        }

        int farthestIndex = -1;
        float farthestDistance = -1f;
        for (int step = 0; step < points.Count; step++)
        {
            int index = (cursor + step) % points.Count;
            float nearest = NearestDistance(points[index], players);
            if (nearest >= minDistance)
            {
                return index;
            }

            if (nearest > farthestDistance)
            {
                farthestDistance = nearest;
                farthestIndex = index;
            }
        }

        return farthestIndex;
    }

    private static float NearestDistance(Vector3 point, IReadOnlyList<Vector3> players)
    {
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < players.Count; i++)
        {
            nearest = Mathf.Min(nearest, EnemyRules.FlatDistance(point, players[i]));
        }

        return nearest;
    }
}
}
