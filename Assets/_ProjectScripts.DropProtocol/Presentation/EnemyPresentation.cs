using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Presentation: animates an enemy from <see cref="EnemyCharacter.ReplicatedState" />, replicated health and
///     transform motion. The attack windup is the telegraph. Never writes gameplay.
/// </summary>
[RequireComponent(typeof(EnemyCharacter))]
[RequireComponent(typeof(Health))]
public sealed class EnemyPresentation : NetworkBehaviour
{
    private const int UpperBodyLayer = 1;
    private const float ChestHeight = 0.9f;

    private static readonly int MoveXId = Animator.StringToHash("MoveX");
    private static readonly int MoveYId = Animator.StringToHash("MoveY");
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");
    private static readonly int IsDeadId = Animator.StringToHash("IsDead");
    private static readonly int AttackId = Animator.StringToHash("Attack");

    [SerializeField]
    private Animator m_animator;

    [SerializeField]
    private Transform m_visual;

    [Tooltip("The clip the Attack state plays; its length sets the playback speed that matches the windup.")]
    [SerializeField]
    private AnimationClip m_attackClip;

    [Tooltip("Where in the attack clip the blow lands, as a fraction of its length.")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float m_hitFraction = 0.6f;

    [SerializeField]
    private Color m_telegraphColor = new(1f, 0.8f, 0.2f);

    [SerializeField]
    private Color m_hitColor = new(1f, 0.35f, 0.35f);

    [SerializeField]
    [Min(0f)]
    private float m_hitFlashSeconds = 0.1f;

    [SerializeField]
    [Min(0f)]
    private float m_deadzone = 0.05f;

    [SerializeField]
    [Min(0f)]
    private float m_sharpness = 12f;

    private EnemyCharacter m_enemy;
    private Health m_health;
    private LocomotionFeed m_locomotion;
    private RendererTint m_tint;
    private float m_flashUntil;
    private bool m_flashing;
    private float m_stride;

    public EnemyState ShownState { get; private set; }
    public bool IsTelegraphing => EnemyPresentationRules.IsTelegraphing(ShownState);

    private void Awake()
    {
        m_enemy = GetComponent<EnemyCharacter>();
        m_health = GetComponent<Health>();
        m_locomotion = new LocomotionFeed(m_deadzone, m_sharpness);
        m_tint = new RendererTint(m_visual != null ? m_visual.GetComponentsInChildren<Renderer>(true) : null);

        if (m_animator != null)
        {
            m_animator.SetLayerWeight(UpperBodyLayer, 0f);
            float clipSeconds = m_attackClip != null ? m_attackClip.length : 0f;
            float windup = m_enemy.Definition != null ? m_enemy.Definition.AttackWindupSeconds : 0f;
            m_animator.SetFloat(AttackSpeedId, EnemyPresentationRules.AttackSpeed(clipSeconds, m_hitFraction, windup));
        }
    }

    private void OnEnable()
    {
        m_enemy.ReplicatedState.OnValueChanged += HandleStateChanged;
        m_health.Current.OnValueChanged += HandleHealthChanged;
        m_locomotion.Reset(transform.position);
        Apply(m_enemy.ReplicatedState.Value, false);
    }

    private void OnDisable()
    {
        m_enemy.ReplicatedState.OnValueChanged -= HandleStateChanged;
        m_health.Current.OnValueChanged -= HandleHealthChanged;
        m_tint.Clear();
    }

    public override void OnNetworkSpawn()
    {
        Apply(m_enemy.ReplicatedState.Value, false);
    }

    private void Update()
    {
        float moveSpeed = m_enemy.Definition != null ? m_enemy.Definition.MoveSpeed : 0f;
        var move = m_locomotion.Step(transform.position, transform.eulerAngles.y, moveSpeed, Time.deltaTime);

        if (m_animator != null)
        {
            m_animator.SetFloat(MoveXId, move.x);
            m_animator.SetFloat(MoveYId, move.y);
            m_animator.SetFloat(SpeedId, move.magnitude);
        }

        var definition = m_enemy.Definition;
        if (definition != null && ShownState != EnemyState.Dead
            && LocomotionRules.Stride(ref m_stride, move, moveSpeed, Time.deltaTime, definition.StrideMetres))
        {
            SfxPlayer.Play(definition.FootstepCue, transform.position);
        }

        if (m_flashing && Time.time >= m_flashUntil)
        {
            m_flashing = false;
            RefreshTint();
        }
    }

    private void HandleStateChanged(EnemyState previous, EnemyState current)
    {
        Apply(current, previous != EnemyState.Attack);
    }

    private void HandleHealthChanged(int previous, int current)
    {
        if (current >= previous)
        {
            return;
        }

        var definition = m_enemy.Definition;
        var chest = transform.position + Vector3.up * ChestHeight;
        if (current > 0)
        {
            m_flashUntil = Time.time + m_hitFlashSeconds;
            m_flashing = true;
            RefreshTint();
            Vfx.Spawn(definition != null ? definition.HitVfx : null, chest, Vector3.up);
            if (definition != null)
            {
                SfxPlayer.Play(definition.HurtCue, chest);
            }
        }
        else
        {
            // Spawned unparented so it outlives the despawn that follows.
            Vfx.Spawn(definition != null ? definition.DeathVfx : null, chest, Vector3.up);
            if (definition != null)
            {
                SfxPlayer.Play(definition.DeathCue, chest);
            }
        }
    }

    private void Apply(EnemyState state, bool triggerAttack)
    {
        ShownState = state;

        if (m_animator != null)
        {
            m_animator.SetBool(IsDeadId, state == EnemyState.Dead);
            if (state == EnemyState.Attack && triggerAttack)
            {
                m_animator.SetTrigger(AttackId);
            }
        }

        if (state == EnemyState.Attack && triggerAttack && m_enemy.Definition != null)
        {
            SfxPlayer.Play(m_enemy.Definition.AttackCue, transform.position + Vector3.up * ChestHeight);
        }

        RefreshTint();
    }

    // Hit flash wins over the telegraph so damage feedback is never hidden behind the windup colour.
    private void RefreshTint()
    {
        if (m_flashing)
        {
            m_tint.Apply(m_hitColor);
        }
        else if (IsTelegraphing)
        {
            m_tint.Apply(m_telegraphColor);
        }
        else
        {
            m_tint.Clear();
        }
    }
}
}
