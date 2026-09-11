using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so health arithmetic is testable without a scene.</summary>
public static class HealthRules
{
    public static int AfterDamage(int current, int amount)
    {
        if (amount <= 0)
        {
            return current;
        }

        return Mathf.Max(0, current - amount);
    }

    public static int AfterHeal(int current, int amount, int max)
    {
        if (amount <= 0)
        {
            return current;
        }

        return Mathf.Min(max, current + amount);
    }

    public static int Clamp(int value, int max)
    {
        return Mathf.Clamp(value, 0, max);
    }

    public static int ReviveHitPoints(int max, float fraction)
    {
        return Mathf.Max(1, Mathf.CeilToInt(max * fraction));
    }
}
}
