using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace DropProtocol
{
/// <summary>
///     Server-driven enemy: picks the nearest alive player, lets <see cref="EnemyBrain" /> decide, and
///     moves a NavMeshAgent accordingly. Clients only display what NetworkTransform and Health replicate,
///     so the agent stays disabled there.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyCharacter : NetworkBehaviour
{
    private const float DespawnSeconds = 1f;
    private const float RetreatStep = 3f;
    private const float StoppingMargin = 0.2f;

    private static readonly List<EnemyCharacter> m_all = new();

    [SerializeField]
    private EnemyDefinition m_definition;

    /// <summary>Brain state mirrored for presentation; the brain itself only exists on the server.</summary>
    public NetworkVariable<EnemyState> ReplicatedState = new();

    private Health m_health;
    private NavMeshAgent m_agent;
    private Collider m_collider;
    private IEnemyAttack m_attack;
    private EnemyBrain m_brain;
    private Health m_target;
    private double m_despawnAt;
    private float m_approachStop;

    /// <summary>Every spawned enemy on this peer; the director and the sandbox panel read it.</summary>
    public static IReadOnlyList<EnemyCharacter> All => m_all;

    public EnemyDefinition Definition => m_definition;
    public EnemyState State => ReplicatedState.Value;
    public bool IsDead => State == EnemyState.Dead;
    public Health Target => m_target;

    /// <summary>Server-side; raised once when an enemy reaches zero health.</summary>
    public static event Action<EnemyCharacter> Died;

    private void Awake()
    {
        m_health = GetComponent<Health>();
        m_agent = GetComponent<NavMeshAgent>();
        m_collider = GetComponent<Collider>();
        m_attack = GetComponent<IEnemyAttack>();

        if (m_definition == null)
        {
            Debug.LogError("EnemyCharacter has no definition.", this);
            enabled = false;
            return;
        }

        // Must run before Health.OnNetworkSpawn copies the maximum into the replicated value.
        m_health.SetMaxHealth(m_definition.MaxHealth);
        m_brain = new EnemyBrain(m_definition.AttackRange, m_definition.PreferredRange, m_definition.MinRange,
            m_definition.AttackCooldownSeconds, m_definition.AttackWindupSeconds);

        m_approachStop = Mathf.Max(0f, m_definition.PreferredRange - StoppingMargin);
        m_agent.speed = m_definition.MoveSpeed;
        m_agent.acceleration = m_definition.Acceleration;
        m_agent.radius = m_definition.AgentRadius;
        m_agent.stoppingDistance = m_approachStop;
        m_agent.autoBraking = true;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || m_brain == null)
        {
            return;
        }

        double now = Time.timeAsDouble;
        if (m_brain.State == EnemyState.Dead)
        {
            if (now >= m_despawnAt)
            {
                NetworkObject.Despawn();
            }

            return;
        }

        float distance;
        m_target = EnemyTargeting.NearestAlive(transform.position, out distance);
        m_brain.Update(m_target != null, distance, now);
        ReplicatedState.Value = m_brain.State;

        // The trigger lands on the update that leaves Attack, so it is checked before the state.
        if (m_brain.AttackTriggered && m_attack != null)
        {
            m_attack.Perform(m_target);
        }

        switch (m_brain.State)
        {
            case EnemyState.Chase:
                ApplyMove();
                break;
            case EnemyState.Attack:
                StopMoving();
                FaceTarget();
                break;
            default:
                StopMoving();
                break;
        }
    }

    // A host shutdown can destroy spawned objects without a despawn callback.
    public override void OnDestroy()
    {
        m_all.Remove(this);
        base.OnDestroy();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        m_all.Clear();
        Died = null;
    }

    /// <summary>Tests build enemies in code; call before the object becomes active.</summary>
    public void SetDefinition(EnemyDefinition definition)
    {
        m_definition = definition;
    }

    public override void OnNetworkSpawn()
    {
        m_all.Add(this);

        if (!IsServer)
        {
            enabled = false;
            return;
        }

        m_agent.enabled = true;
        m_health.Current.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        m_all.Remove(this);
        m_health.Current.OnValueChanged -= HandleHealthChanged;
    }

    private void ApplyMove()
    {
        switch (m_brain.Move)
        {
            case EnemyMove.Approach:
                MoveTo(m_target.transform.position, m_approachStop);
                break;
            case EnemyMove.Retreat:
                var away = EnemyRules.RetreatPoint(transform.position, m_target.transform.position, RetreatStep);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(away, out hit, RetreatStep, NavMesh.AllAreas))
                {
                    MoveTo(hit.position, 0f);
                }
                else
                {
                    StopMoving();
                }

                break;
            default:
                StopMoving();
                FaceTarget();
                break;
        }
    }

    // Retreat steps are shorter than the approach stopping distance, so each move sets its own.
    private void MoveTo(Vector3 destination, float stoppingDistance)
    {
        if (!m_agent.enabled || !m_agent.isOnNavMesh)
        {
            return;
        }

        m_agent.stoppingDistance = stoppingDistance;
        m_agent.isStopped = false;
        m_agent.SetDestination(destination);
    }

    private void StopMoving()
    {
        if (m_agent.enabled && m_agent.isOnNavMesh && m_agent.hasPath)
        {
            m_agent.ResetPath();
        }
    }

    private void FaceTarget()
    {
        if (m_target == null)
        {
            return;
        }

        var toTarget = m_target.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget);
        }
    }

    private void HandleHealthChanged(int previous, int current)
    {
        if (previous > 0 && current <= 0)
        {
            Kill();
        }
    }

    private void Kill()
    {
        m_brain.Kill();
        ReplicatedState.Value = EnemyState.Dead;
        StopMoving();
        m_agent.enabled = false;
        if (m_collider != null)
        {
            m_collider.enabled = false;
        }

        m_despawnAt = Time.timeAsDouble + DespawnSeconds;
        Died?.Invoke(this);
    }
}
}
