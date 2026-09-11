using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so projectile flight arithmetic is testable without a scene.</summary>
public static class ProjectileMath
{
    public static Vector3 LaunchDirection(Vector3 from, Vector3 to)
    {
        var flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
        if (flat.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return flat.normalized;
    }

    public static float StepLength(float speed, float deltaTime)
    {
        return Mathf.Max(0f, speed * deltaTime);
    }

    public static bool HasExpired(double spawnedAt, float lifetimeSeconds, double now)
    {
        return now >= spawnedAt + lifetimeSeconds;
    }
}
}
