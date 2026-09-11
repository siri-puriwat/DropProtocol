namespace DropProtocol.Tests.PlayMode
{
    public sealed class TestCommandSource : IPlayerCommandSource
    {
        public PlayerCommand Command { get; set; } = PlayerCommand.None;

        public PlayerCommand GetCommand() => Command;
    }
}
