using UnityEngine;

namespace DropProtocol
{
/// <summary>Server-side player lookup for enemies; downed players are never targets.</summary>
public static class EnemyTargeting
{
    public static Health NearestAlive(Vector3 origin, out float flatDistance)
    {
        Health nearest = null;
        flatDistance = float.PositiveInfinity;

        foreach (var player in NetworkPlayer.All)
        {
            var health = player.Health;
            if (health == null || health.IsDowned)
            {
                continue;
            }

            float distance = EnemyRules.FlatDistance(origin, player.transform.position);
            if (distance >= flatDistance)
            {
                continue;
            }

            flatDistance = distance;
            nearest = health;
        }

        return nearest;
    }
}
}
