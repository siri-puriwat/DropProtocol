namespace DropProtocol
{
/// <summary>What a bot knows at one think. Distances are flat and positive infinity when nothing was found.</summary>
public struct BotSenses
{
    public bool HasLeader;
    public float LeaderDistance;
    public bool HasEnemy;
    public float EnemyDistance;
    public bool HasDowned;
    public float DownedDistance;
    public bool HasObjective;
    public float ObjectiveDistance;
    public bool ObjectiveNearLeader;
    public int Ammo;
    public int MagazineSize;
    public bool IsReloading;
}

/// <summary>
///     Squad bot state machine: revive beats combat beats objective beats follow. The brain decides;
///     <see cref="BotCommandSource" /> checks the line of fire and turns the decision into a command.
/// </summary>
public sealed class BotBrain
{
    private readonly BotTuning m_tuning;
    private bool m_movingToLeader;
    private bool m_reloadRequested;

    public BotBrain(BotTuning tuning)
    {
        m_tuning = tuning;
    }

    public BotState State { get; private set; }
    public BotMove Move { get; private set; }
    public bool AimAtEnemy { get; private set; }
    public bool WantsFire { get; private set; }
    public bool WantsInteract { get; private set; }

    /// <summary>True for exactly one update: the one that decides to top up the magazine.</summary>
    public bool ReloadTriggered { get; private set; }

    public void Update(BotSenses senses)
    {
        // A revive already under way is only abandoned for an enemy much closer than one that stops it starting.
        float dangerRange = State == BotState.Revive ? m_tuning.ReviveAbortRange : m_tuning.ReviveDangerRange;
        bool enemyClose = senses.HasEnemy && senses.EnemyDistance <= dangerRange;

        if (senses.HasDowned && senses.DownedDistance <= m_tuning.ReviveSearchRange && !enemyClose)
        {
            State = BotState.Revive;
            Move = BotMove.ToDowned;
            AimAtEnemy = senses.HasEnemy && senses.EnemyDistance <= m_tuning.EngageRange;
            WantsFire = AimAtEnemy;
            WantsInteract = true;
        }
        else if (senses.HasEnemy && BotRules.ShouldEngage(senses.EnemyDistance, State == BotState.Combat,
                     m_tuning.EngageRange, m_tuning.DisengageRange))
        {
            State = BotState.Combat;
            Move = senses.EnemyDistance < m_tuning.RetreatRange ? BotMove.AwayFromEnemy : FollowMove(senses);
            AimAtEnemy = true;
            WantsFire = true;
            WantsInteract = false;
        }
        else if (senses.HasObjective && senses.ObjectiveNearLeader)
        {
            State = BotState.Objective;
            Move = BotMove.ToObjective;
            AimAtEnemy = false;
            WantsFire = false;
            WantsInteract = true;
        }
        else
        {
            State = BotState.Follow;
            Move = FollowMove(senses);
            AimAtEnemy = false;
            WantsFire = false;
            WantsInteract = false;
        }

        UpdateReload(senses);
    }

    private BotMove FollowMove(BotSenses senses)
    {
        m_movingToLeader = senses.HasLeader && BotRules.ShouldMoveToLeader(senses.LeaderDistance, m_movingToLeader,
            m_tuning.FollowStopRadius, m_tuning.FollowResumeRadius);
        return m_movingToLeader ? BotMove.ToLeader : BotMove.Hold;
    }

    private void UpdateReload(BotSenses senses)
    {
        if (senses.IsReloading || senses.Ammo >= senses.MagazineSize)
        {
            m_reloadRequested = false;
        }

        bool wantsReload = BotRules.ShouldReload(senses.Ammo, senses.MagazineSize, senses.IsReloading, senses.HasEnemy);
        ReloadTriggered = wantsReload && !m_reloadRequested;
        m_reloadRequested |= wantsReload;
    }
}
}
