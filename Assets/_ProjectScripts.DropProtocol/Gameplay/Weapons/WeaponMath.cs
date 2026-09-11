using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so shot direction math is testable without a scene.</summary>
public static class WeaponMath
{
    public static Vector2 ResolveAim(Vector2 commandAim, float yawDegrees)
    {
        if (commandAim.sqrMagnitude > 0f)
        {
            return commandAim.normalized;
        }

        float radians = yawDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    /// <summary>
    ///     Rotates a horizontal aim by a random fraction of the spread cone. The random sample is injected so
    ///     tests are deterministic; the server passes UnityEngine.Random.
    /// </summary>
    public static Vector3 SpreadDirection(Vector2 aimXZ, float spreadDegrees, float unitRandom)
    {
        float offset = (unitRandom * 2f - 1f) * spreadDegrees * 0.5f;
        float yaw = MotorMath.YawFromDirection(aimXZ) + offset;
        float radians = yaw * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
    }
}
}
