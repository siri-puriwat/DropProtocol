using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so bot follow, engage and reload decisions are testable without a scene.</summary>
public static class BotRules
{
    // Start walking only past the resume radius, then keep walking until inside the stop radius.
    public static bool ShouldMoveToLeader(float distance, bool wasMoving, float stopRadius, float resumeRadius)
    {
        return distance > (wasMoving ? stopRadius : resumeRadius);
    }

    // Open fire inside the engage range, then keep firing until the target is past the disengage range.
    public static bool ShouldEngage(float distance, bool wasEngaged, float engageRange, float disengageRange)
    {
        return distance <= (wasEngaged ? disengageRange : engageRange);
    }

    // Top up whenever nothing is in sight; under fire only an empty magazine is worth the pause.
    public static bool ShouldReload(int ammo, int magazineSize, bool isReloading, bool hasEnemy)
    {
        if (isReloading || ammo >= magazineSize)
        {
            return false;
        }

        return !hasEnemy || ammo == 0;
    }

    /// <summary>Skips corners already within reach; the last corner is the destination and is never skipped.</summary>
    public static int NextCornerIndex(Vector3[] corners, int cornerCount, int current, Vector3 position, float reachRadius)
    {
        int index = current;
        while (index < cornerCount - 1 && EnemyRules.FlatDistance(position, corners[index]) <= reachRadius)
        {
            index++;
        }

        return index;
    }
}
}
