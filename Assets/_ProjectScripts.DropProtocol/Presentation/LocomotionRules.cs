using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Turns the motion NetworkTransform already replicates into blend-tree input, so every peer animates
///     from the same data and nothing about animation is sent over the wire.
/// </summary>
public static class LocomotionRules
{
    private const float StepThreshold = 0.1f;

    public static Vector2 WorldVelocity(Vector3 previous, Vector3 current, float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return Vector2.zero;
        }

        return new Vector2((current.x - previous.x) / deltaTime, (current.z - previous.z) / deltaTime);
    }

    /// <summary>World XZ velocity expressed as (strafe, forward) for a character facing <paramref name="yawDegrees" />.</summary>
    public static Vector2 ToLocal(Vector2 worldVelocity, float yawDegrees)
    {
        float radians = yawDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        var forward = new Vector2(sin, cos);
        var right = new Vector2(cos, -sin);
        return new Vector2(Vector2.Dot(worldVelocity, right), Vector2.Dot(worldVelocity, forward));
    }

    public static Vector2 Normalize(Vector2 localVelocity, float moveSpeed, float deadzone)
    {
        if (moveSpeed <= 0f)
        {
            return Vector2.zero;
        }

        var normalized = Vector2.ClampMagnitude(localVelocity / moveSpeed, 1f);
        return normalized.magnitude < deadzone ? Vector2.zero : normalized;
    }

    // Interpolated transforms arrive in small uneven steps; exponential smoothing hides that without lag spikes.
    public static Vector2 Damp(Vector2 current, Vector2 target, float sharpness, float deltaTime)
    {
        float t = 1f - Mathf.Exp(-sharpness * deltaTime);
        return Vector2.Lerp(current, target, t);
    }

    /// <summary>
    ///     Advances a footstep accumulator from the blend input rather than raw position deltas, so a spawn
    ///     snap or a network catch-up never fires a burst of steps: one frame adds at most one stride.
    ///     Returns true when a step lands.
    /// </summary>
    public static bool Stride(ref float accumulated, Vector2 move, float moveSpeed, float deltaTime, float strideMetres)
    {
        float magnitude = Mathf.Clamp01(move.magnitude);
        if (strideMetres <= 0f || moveSpeed <= 0f || deltaTime <= 0f || magnitude < StepThreshold)
        {
            accumulated = 0f;
            return false;
        }

        accumulated += Mathf.Min(magnitude * moveSpeed * deltaTime, strideMetres);
        if (accumulated < strideMetres)
        {
            return false;
        }

        accumulated -= strideMetres;
        return true;
    }
}
}
