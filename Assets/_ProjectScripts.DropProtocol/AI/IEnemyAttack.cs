namespace DropProtocol
{
/// <summary>What an enemy does when its windup completes. Timing belongs to <see cref="EnemyBrain" />.</summary>
public interface IEnemyAttack
{
    void Perform(Health target);
}
}
