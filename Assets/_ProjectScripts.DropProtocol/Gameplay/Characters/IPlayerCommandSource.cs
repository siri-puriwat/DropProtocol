namespace DropProtocol
{
/// <summary>
///     Implemented by human input, network input, bots, and tests alike; the character cannot tell them apart.
/// </summary>
public interface IPlayerCommandSource
{
    PlayerCommand GetCommand();
}
}
