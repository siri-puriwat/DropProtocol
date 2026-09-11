using UnityEngine;
using UnityEngine.InputSystem;

namespace DropProtocol
{
/// <summary>
///     Camera-relative movement and ground-plane cursor aim are resolved here so the character
///     never touches input devices or cameras.
/// </summary>
public sealed class HumanCommandSource : MonoBehaviour, IPlayerCommandSource
{
    [Tooltip("Camera used for camera-relative movement and cursor aiming. Falls back to Camera.main.")]
    [SerializeField]
    private Camera m_camera;

    [Tooltip("A cursor closer than this to the character (in metres) does not change its facing.")]
    [SerializeField]
    private float m_aimDeadzoneRadius = 0.3f;

    private InputAction m_move;
    private InputAction m_aimPointer;
    private InputAction m_aimStick;
    private InputAction m_fire;
    private InputAction m_reload;
    private InputAction m_interact;

    private void Awake()
    {
        var actions = InputSystem.actions;
        if (actions == null)
        {
            Debug.LogError(
                "HumanCommandSource requires project-wide input actions (Project Settings, Input System Package).",
                this);
            enabled = false;
            return;
        }

        m_move = actions.FindAction("Player/Move", true);
        m_aimPointer = actions.FindAction("Player/AimPointer", true);
        m_aimStick = actions.FindAction("Player/AimStick", true);
        m_fire = actions.FindAction("Player/Fire", true);
        m_reload = actions.FindAction("Player/Reload", true);
        m_interact = actions.FindAction("Player/Interact", true);
    }

    public PlayerCommand GetCommand()
    {
        if (m_move == null)
        {
            return PlayerCommand.None;
        }

        var camera = ResolveCamera();
        float cameraYaw = camera != null ? camera.transform.eulerAngles.y : 0f;

        return new PlayerCommand
        {
            Move = CommandMath.InputToWorldXZ(m_move.ReadValue<Vector2>(), cameraYaw),
            Aim = ResolveAim(camera, cameraYaw),
            Fire = m_fire.IsPressed(),
            Reload = m_reload.IsPressed(),
            Interact = m_interact.IsPressed()
        };
    }

    private Vector2 ResolveAim(Camera camera, float cameraYaw)
    {
        // Stick wins over pointer so gamepad players are not fighting a parked mouse.
        var stick = m_aimStick.ReadValue<Vector2>();
        if (stick.sqrMagnitude > 0f)
        {
            return CommandMath.InputToWorldXZ(stick, cameraYaw).normalized;
        }

        if (camera == null)
        {
            return Vector2.zero;
        }

        var ray = camera.ScreenPointToRay(m_aimPointer.ReadValue<Vector2>());
        if (!CommandMath.TryProjectToPlane(ray, transform.position.y, out var target))
        {
            return Vector2.zero;
        }

        return CommandMath.AimDirection(transform.position, target, m_aimDeadzoneRadius);
    }

    private Camera ResolveCamera()
    {
        if (m_camera == null)
        {
            m_camera = Camera.main;
        }

        return m_camera;
    }
}
}
