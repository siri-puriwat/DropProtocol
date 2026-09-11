using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so movement math is testable without a scene.</summary>
public static class MotorMath
{
    public static Vector3 PlanarVelocity(Vector2 move, float speed)
    {
        var clamped = Vector2.ClampMagnitude(move, 1f);
        return new Vector3(clamped.x * speed, 0f, clamped.y * speed);
    }

    public static float YawFromDirection(Vector2 direction)
    {
        return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
    }

    public static float ResolveTargetYaw(Vector2 aim, Vector2 move, float currentYaw)
    {
        if (aim.sqrMagnitude > 0f)
        {
            return YawFromDirection(aim);
        }

        if (move.sqrMagnitude > 0f)
        {
            return YawFromDirection(move);
        }

        return currentYaw;
    }

    public static float StepYaw(float currentYaw, float targetYaw, float turnSpeedDegreesPerSecond, float deltaTime)
    {
        return Mathf.MoveTowardsAngle(currentYaw, targetYaw, turnSpeedDegreesPerSecond * deltaTime);
    }
}
}
