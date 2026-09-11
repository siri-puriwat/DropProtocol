using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Must not know whether its command source is a human, a network client, a bot, or a test.
///     Forwards each command to its optional sub-systems so all of them act on the same sample.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
public sealed class PlayerCharacter : MonoBehaviour
{
    private CharacterMotor m_motor;
    private Health m_health;
    private WeaponController m_weapon;
    private ReviveController m_reviver;
    private IPlayerCommandSource m_commandSource;

    public CharacterMotor Motor => m_motor;

    public PlayerCommand LastCommand { get; private set; }

    public IPlayerCommandSource CommandSource => m_commandSource;

    private void Awake()
    {
        m_motor = GetComponent<CharacterMotor>();
        m_health = GetComponent<Health>();
        m_weapon = GetComponent<WeaponController>();
        m_reviver = GetComponent<ReviveController>();

        // Scene-authored characters carry a sibling source; spawners, bots, and tests inject one instead.
        m_commandSource ??= GetComponent<IPlayerCommandSource>();
    }

    private void Update()
    {
        var command = m_commandSource?.GetCommand() ?? PlayerCommand.None;
        if (m_health != null && m_health.IsDowned)
        {
            command = PlayerCommand.None;
        }

        LastCommand = command;
        m_motor.Move(command.Move, command.Aim, Time.deltaTime);

        if (m_weapon != null)
        {
            m_weapon.Tick(command, Time.timeAsDouble);
        }

        if (m_reviver != null)
        {
            m_reviver.Tick(command, Time.deltaTime);
        }
    }

    public void SetCommandSource(IPlayerCommandSource source)
    {
        m_commandSource = source;
    }
}
}
