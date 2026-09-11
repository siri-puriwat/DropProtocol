namespace DropProtocol
{
public enum BotState
{
    Follow,
    Combat,
    Revive,
    Objective
}

public enum BotMove
{
    Hold,
    ToLeader,
    ToDowned,
    AwayFromEnemy,
    ToObjective
}
}
