namespace DropProtocol
{
/// <summary>
///     Per-enemy state machine. Time is passed in so windup and cooldown are testable without a scene.
///     The brain decides; <see cref="EnemyCharacter" /> moves the agent and performs the attack.
/// </summary>
public sealed class EnemyBrain
{
    private readonly float m_attackRange;
    private readonly float m_preferredRange;
    private readonly float m_minRange;
    private readonly double m_cooldownSeconds;
    private readonly double m_windupSeconds;

    private double m_nextAttackAt;
    private double m_windupEndsAt;

    public EnemyBrain(float attackRange, float preferredRange, float minRange, double cooldownSeconds,
        double windupSeconds)
    {
        m_attackRange = attackRange;
        m_preferredRange = preferredRange;
        m_minRange = minRange;
        m_cooldownSeconds = cooldownSeconds;
        m_windupSeconds = windupSeconds;
    }

    public EnemyState State { get; private set; }
    public EnemyMove Move { get; private set; }

    /// <summary>True for exactly one update: the one in which the windup completes.</summary>
    public bool AttackTriggered { get; private set; }

    public void Update(bool hasTarget, float distanceToTarget, double now)
    {
        AttackTriggered = false;
        if (State == EnemyState.Dead)
        {
            return;
        }

        if (!hasTarget)
        {
            State = EnemyState.Idle;
            Move = EnemyMove.Hold;
            return;
        }

        if (State == EnemyState.Attack)
        {
            if (now < m_windupEndsAt)
            {
                return;
            }

            AttackTriggered = true;
            m_nextAttackAt = now + m_cooldownSeconds;
            State = EnemyState.Chase;
            Move = EnemyRules.ResolveMove(distanceToTarget, m_preferredRange, m_minRange);
            return;
        }

        if (EnemyRules.InAttackRange(distanceToTarget, m_attackRange) && now >= m_nextAttackAt)
        {
            State = EnemyState.Attack;
            Move = EnemyMove.Hold;
            m_windupEndsAt = now + m_windupSeconds;
            return;
        }

        State = EnemyState.Chase;
        Move = EnemyRules.ResolveMove(distanceToTarget, m_preferredRange, m_minRange);
    }

    public void Kill()
    {
        State = EnemyState.Dead;
        Move = EnemyMove.Hold;
        AttackTriggered = false;
    }
}
}
