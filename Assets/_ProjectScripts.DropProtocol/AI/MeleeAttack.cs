using UnityEngine;

namespace DropProtocol
{
[RequireComponent(typeof(EnemyCharacter))]
public sealed class MeleeAttack : MonoBehaviour, IEnemyAttack
{
    private const float ReachGrace = 0.3f;

    private EnemyCharacter m_enemy;

    private void Awake()
    {
        m_enemy = GetComponent<EnemyCharacter>();
    }

    public void Perform(Health target)
    {
        if (target == null)
        {
            return;
        }

        // The windup already elapsed, so a target that stepped away makes the swing miss.
        float distance = EnemyRules.FlatDistance(transform.position, target.transform.position);
        if (!EnemyRules.InAttackRange(distance, m_enemy.Definition.AttackRange + ReachGrace))
        {
            return;
        }

        target.ApplyDamage(m_enemy.Definition.AttackDamage);
    }
}
}
