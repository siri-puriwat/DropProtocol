using UnityEngine;

namespace DropProtocol
{
/// <summary>Per-character blend-tree input derived from transform motion; shared by player and enemy presentation.</summary>
public sealed class LocomotionFeed
{
    private readonly float m_deadzone;
    private readonly float m_sharpness;
    private Vector3 m_lastPosition;

    public LocomotionFeed(float deadzone, float sharpness)
    {
        m_deadzone = deadzone;
        m_sharpness = sharpness;
    }

    public Vector2 Move { get; private set; }

    public void Reset(Vector3 position)
    {
        m_lastPosition = position;
        Move = Vector2.zero;
    }

    public Vector2 Step(Vector3 position, float yawDegrees, float moveSpeed, float deltaTime)
    {
        var world = LocomotionRules.WorldVelocity(m_lastPosition, position, deltaTime);
        m_lastPosition = position;

        var local = LocomotionRules.ToLocal(world, yawDegrees);
        var target = LocomotionRules.Normalize(local, moveSpeed, m_deadzone);
        Move = LocomotionRules.Damp(Move, target, m_sharpness, deltaTime);
        return Move;
    }
}
}
