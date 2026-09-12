using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Static enemy archetype configuration. The three archetypes differ only in these numbers;
///     behaviour lives in <see cref="EnemyBrain" /> and <see cref="EnemyCharacter" />.
/// </summary>
[CreateAssetMenu(menuName = "DropProtocol/Enemy Definition", fileName = "NewEnemy")]
public sealed class EnemyDefinition : ScriptableObject
{
    [SerializeField]
    private string m_displayName = "Enemy";

    [SerializeField]
    [Min(1)]
    private int m_maxHealth = 60;

    [SerializeField]
    [Min(1)]
    private int m_threatCost = 1;

    [SerializeField]
    [Min(0.1f)]
    private float m_moveSpeed = 5f;

    [SerializeField]
    [Min(0.1f)]
    private float m_acceleration = 16f;

    [SerializeField]
    [Min(0.1f)]
    private float m_agentRadius = 0.4f;

    [SerializeField]
    [Min(0)]
    private int m_attackDamage = 10;

    [SerializeField]
    [Min(0.1f)]
    private float m_attackRange = 1.6f;

    [Tooltip("Approach until this close to the target.")]
    [SerializeField]
    [Min(0f)]
    private float m_preferredRange = 1.6f;

    [Tooltip("Retreat when the target is closer than this. Zero means never retreat.")]
    [SerializeField]
    [Min(0f)]
    private float m_minRange;

    [SerializeField]
    [Min(0f)]
    private float m_attackCooldownSeconds = 1f;

    [SerializeField]
    [Min(0f)]
    private float m_attackWindupSeconds = 0.25f;

    [Header("Presentation")]
    [SerializeField]
    private GameObject m_hitVfx;

    [SerializeField]
    private GameObject m_deathVfx;

    [SerializeField]
    private SfxCue m_attackCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_hurtCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_deathCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_footstepCue = SfxCue.Default;

    [SerializeField]
    [Min(0.2f)]
    private float m_strideMetres = 1.4f;

    public string DisplayName => m_displayName;
    public int MaxHealth => m_maxHealth;
    public int ThreatCost => m_threatCost;
    public float MoveSpeed => m_moveSpeed;
    public float Acceleration => m_acceleration;
    public float AgentRadius => m_agentRadius;
    public int AttackDamage => m_attackDamage;
    public float AttackRange => m_attackRange;
    public float PreferredRange => m_preferredRange;
    public float MinRange => m_minRange;
    public float AttackCooldownSeconds => m_attackCooldownSeconds;
    public float AttackWindupSeconds => m_attackWindupSeconds;
    public GameObject HitVfx => m_hitVfx;
    public GameObject DeathVfx => m_deathVfx;
    public SfxCue AttackCue => m_attackCue;
    public SfxCue HurtCue => m_hurtCue;
    public SfxCue DeathCue => m_deathCue;
    public SfxCue FootstepCue => m_footstepCue;
    public float StrideMetres => m_strideMetres;

    public static EnemyDefinition Create(int maxHealth, int threatCost, float moveSpeed, float agentRadius,
        int attackDamage, float attackRange, float preferredRange, float minRange,
        float attackCooldownSeconds, float attackWindupSeconds)
    {
        var definition = CreateInstance<EnemyDefinition>();
        definition.m_maxHealth = maxHealth;
        definition.m_threatCost = threatCost;
        definition.m_moveSpeed = moveSpeed;
        definition.m_agentRadius = agentRadius;
        definition.m_attackDamage = attackDamage;
        definition.m_attackRange = attackRange;
        definition.m_preferredRange = preferredRange;
        definition.m_minRange = minRange;
        definition.m_attackCooldownSeconds = attackCooldownSeconds;
        definition.m_attackWindupSeconds = attackWindupSeconds;
        return definition;
    }
}
}
