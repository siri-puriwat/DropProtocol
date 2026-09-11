using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Takes world-space intent only; knows nothing about input, cameras, or networking.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class CharacterMotor : MonoBehaviour
{
    // CharacterController stops reporting isGrounded on slopes and steps unless pushed down slightly.
    private const float GroundedStickVelocity = -2f;

    [SerializeField]
    private float m_moveSpeed = 6f;

    [SerializeField]
    private float m_turnSpeed = 720f;

    [SerializeField]
    private float m_gravity = -20f;

    private CharacterController m_controller;
    private float m_verticalVelocity;

    public float MoveSpeed => m_moveSpeed;
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded => m_controller.isGrounded;
    public float Yaw => transform.eulerAngles.y;

    private void Awake()
    {
        m_controller = GetComponent<CharacterController>();
    }

    public void Move(Vector2 worldMove, Vector2 worldAim, float deltaTime)
    {
        if (m_controller.isGrounded && m_verticalVelocity < 0f)
        {
            m_verticalVelocity = GroundedStickVelocity;
        }
        else
        {
            m_verticalVelocity += m_gravity * deltaTime;
        }

        var planar = MotorMath.PlanarVelocity(worldMove, m_moveSpeed);
        Velocity = planar + Vector3.up * m_verticalVelocity;
        m_controller.Move(Velocity * deltaTime);

        float currentYaw = transform.eulerAngles.y;
        float targetYaw = MotorMath.ResolveTargetYaw(worldAim, worldMove, currentYaw);
        float nextYaw = MotorMath.StepYaw(currentYaw, targetYaw, m_turnSpeed, deltaTime);
        transform.rotation = Quaternion.Euler(0f, nextYaw, 0f);
    }
}
}
