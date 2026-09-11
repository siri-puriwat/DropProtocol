using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so enemy range decisions are testable without a scene.</summary>
public static class EnemyRules
{
    public static EnemyMove ResolveMove(float distance, float preferredRange, float minRange)
    {
        if (distance > preferredRange)
        {
            return EnemyMove.Approach;
        }

        if (distance < minRange)
        {
            return EnemyMove.Retreat;
        }

        return EnemyMove.Hold;
    }

    public static bool InAttackRange(float distance, float attackRange)
    {
        return distance <= attackRange;
    }

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static Vector3 RetreatPoint(Vector3 self, Vector3 threat, float step)
    {
        var away = new Vector3(self.x - threat.x, 0f, self.z - threat.z);
        if (away.sqrMagnitude < 0.0001f)
        {
            away = Vector3.forward;
        }

        return self + away.normalized * step;
    }
}
}
