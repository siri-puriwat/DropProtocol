using UnityEngine;
using UnityEngine.InputSystem;

namespace DropProtocol
{
/// <summary>
///     Owner-side edge detection on the arrow keys / d-pad. Runs only for the local human so the
///     controller never touches input devices and bots never generate protocol requests.
/// </summary>
[RequireComponent(typeof(ProtocolController))]
[RequireComponent(typeof(NetworkPlayer))]
public sealed class ProtocolInput : MonoBehaviour
{
    private ProtocolController m_controller;
    private NetworkPlayer m_player;
    private InputAction m_direction;
    private Vector2 m_previous;

    private void Awake()
    {
        m_controller = GetComponent<ProtocolController>();
        m_player = GetComponent<NetworkPlayer>();

        var actions = InputSystem.actions;
        if (actions == null)
        {
            enabled = false;
            return;
        }

        m_direction = actions.FindAction("Player/ProtocolDirection", true);
    }

    private void Update()
    {
        if (NetworkPlayer.LocalPlayer != m_player)
        {
            return;
        }

        var current = m_direction.ReadValue<Vector2>();
        if (ProtocolRules.TryPressedDirection(m_previous, current, out var direction))
        {
            m_controller.RequestDirection(direction);
        }

        m_previous = current;
    }
}
}
