using System;
using UnityEngine;
using UnityEngine.AI;

namespace DropProtocol
{
/// <summary>
///     Host-side command source for a squad bot. Perception and the <see cref="BotBrain" /> run every
///     ThinkSeconds; steering, aiming and the line-of-fire check run every call so a squadmate stepping
///     into the line stops the shot within a frame. Paths come from the baked NavMesh because the player
///     prefab deliberately has no NavMeshAgent: bots must drive the exact character humans drive.
/// </summary>
public sealed class BotCommandSource : IPlayerCommandSource
{
    private const int MaxCorners = 16;
    private const float CornerReach = 0.3f;
    // Wide enough to snap a goal that sits on a relay post, whose collider is a hole in the NavMesh.
    private const float NavSampleRadius = 2f;
    private const float ArriveMargin = 0.3f;
    private const float AimDeadzone = 0.1f;

    private readonly NetworkPlayer m_self;
    private readonly Transform m_transform;
    private readonly Health m_health;
    private readonly WeaponController m_weapon;
    private readonly BotTuning m_tuning;
    private readonly Func<double> m_clock;
    private readonly BotBrain m_brain;
    private readonly NavMeshPath m_path = new();
    private readonly Vector3[] m_corners = new Vector3[MaxCorners];

    private double m_nextThinkAt;
    private NetworkPlayer m_leader;
    private EnemyCharacter m_enemy;
    private NetworkPlayer m_downed;
    private Vector3 m_objective;
    private bool m_hasObjective;
    private bool m_objectiveNeedsInteract;
    private Vector3 m_retreatPoint;
    private int m_cornerCount;
    private int m_cornerIndex;
    private bool m_reloadPending;

    public BotCommandSource(NetworkPlayer self, BotTuning tuning, Func<double> clock)
    {
        m_self = self;
        m_transform = self.transform;
        m_health = self.GetComponent<Health>();
        m_weapon = self.GetComponent<WeaponController>();
        m_tuning = tuning;
        m_clock = clock;
        m_brain = new BotBrain(tuning);
    }

    public BotState State => m_brain.State;

    public PlayerCommand GetCommand()
    {
        if (m_health != null && m_health.IsDowned)
        {
            return PlayerCommand.None;
        }

        double now = m_clock();
        if (now >= m_nextThinkAt)
        {
            Think();
            m_nextThinkAt = now + m_tuning.ThinkSeconds;
        }

        var command = new PlayerCommand
        {
            Move = ResolveMove(),
            Aim = ResolveAim(),
            Fire = m_brain.WantsFire && m_enemy != null && m_weapon != null && m_weapon.HasLineOfFire(m_enemy.transform),
            Reload = m_reloadPending,
            Interact = ResolveInteract()
        };

        // WeaponController reloads on the rising edge, so the request is held for a single command.
        m_reloadPending = false;
        return command;
    }

    private void Think()
    {
        Vector3 position = m_transform.position;
        m_leader = BotPerception.NearestAliveHuman(position, out float leaderDistance);
        m_enemy = BotPerception.NearestAliveEnemy(position, out float enemyDistance);
        m_downed = BotPerception.NearestDownedTeammate(position, m_self, out float downedDistance);
        m_hasObjective = BotPerception.SquadObjective(position, out m_objective, out m_objectiveNeedsInteract,
            out float objectiveDistance);
        // With no human standing the bots work the objective on their own.
        bool objectiveNearLeader = m_hasObjective && (m_leader == null ||
                                                      EnemyRules.FlatDistance(m_leader.transform.position, m_objective) <=
                                                      m_tuning.ObjectiveAssistRange);

        bool hasWeapon = m_weapon != null && m_weapon.Definition != null;
        m_brain.Update(new BotSenses
        {
            HasLeader = m_leader != null,
            LeaderDistance = leaderDistance,
            HasEnemy = m_enemy != null,
            EnemyDistance = enemyDistance,
            HasDowned = m_downed != null,
            DownedDistance = downedDistance,
            HasObjective = m_hasObjective,
            ObjectiveDistance = objectiveDistance,
            ObjectiveNearLeader = objectiveNearLeader,
            Ammo = hasWeapon ? m_weapon.Ammo.Value : 0,
            MagazineSize = hasWeapon ? m_weapon.Definition.MagazineSize : 0,
            IsReloading = hasWeapon && m_weapon.IsReloading.Value
        });
        m_reloadPending |= m_brain.ReloadTriggered;

        if (m_brain.Move == BotMove.AwayFromEnemy && m_enemy != null)
        {
            Vector3 away = EnemyRules.RetreatPoint(position, m_enemy.transform.position, m_tuning.RetreatStep);
            m_retreatPoint = NavMesh.SamplePosition(away, out NavMeshHit hit, m_tuning.RetreatStep, NavMesh.AllAreas)
                ? hit.position
                : away;
        }

        Repath(position);
    }

    private void Repath(Vector3 position)
    {
        m_cornerCount = 0;
        m_cornerIndex = 0;
        if (!TryGetGoal(out Vector3 goal, out _))
        {
            return;
        }

        // Both ends are snapped because the character stands a little above the baked surface.
        if (!NavMesh.SamplePosition(position, out NavMeshHit start, NavSampleRadius, NavMesh.AllAreas)
            || !NavMesh.SamplePosition(goal, out NavMeshHit end, NavSampleRadius, NavMesh.AllAreas)
            || !NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, m_path)
            || m_path.status == NavMeshPathStatus.PathInvalid)
        {
            return;
        }

        m_cornerCount = m_path.GetCornersNonAlloc(m_corners);
        m_cornerIndex = m_cornerCount > 1 ? 1 : 0;
    }

    private bool TryGetGoal(out Vector3 goal, out float stopDistance)
    {
        switch (m_brain.Move)
        {
            case BotMove.ToLeader when m_leader != null:
                goal = m_leader.transform.position;
                stopDistance = m_tuning.FollowStopRadius;
                return true;
            case BotMove.ToDowned when m_downed != null:
                goal = m_downed.transform.position;
                stopDistance = Mathf.Max(0f, m_tuning.ReviveReach - ArriveMargin);
                return true;
            case BotMove.AwayFromEnemy:
                goal = m_retreatPoint;
                stopDistance = ArriveMargin;
                return true;
            case BotMove.ToObjective when m_hasObjective:
                goal = m_objective;
                stopDistance = Mathf.Max(0f, m_tuning.ObjectiveReach - ArriveMargin);
                return true;
            default:
                goal = default;
                stopDistance = 0f;
                return false;
        }
    }

    // Without a path the bot walks straight at the goal: right on open ground, merely stuck against a wall.
    private Vector2 ResolveMove()
    {
        if (!TryGetGoal(out Vector3 goal, out float stopDistance))
        {
            return Vector2.zero;
        }

        Vector3 position = m_transform.position;
        if (EnemyRules.FlatDistance(position, goal) <= stopDistance)
        {
            return Vector2.zero;
        }

        Vector3 waypoint = goal;
        if (m_cornerCount > 0)
        {
            m_cornerIndex = BotRules.NextCornerIndex(m_corners, m_cornerCount, m_cornerIndex, position, CornerReach);
            if (m_cornerIndex < m_cornerCount - 1)
            {
                waypoint = m_corners[m_cornerIndex];
            }
        }

        return CommandMath.AimDirection(position, waypoint, AimDeadzone);
    }

    private Vector2 ResolveAim()
    {
        Vector3 position = m_transform.position;
        if (m_brain.AimAtEnemy && m_enemy != null)
        {
            return CommandMath.AimDirection(position, m_enemy.transform.position, AimDeadzone);
        }

        if (m_brain.State == BotState.Revive && m_downed != null)
        {
            return CommandMath.AimDirection(position, m_downed.transform.position, AimDeadzone);
        }

        return Vector2.zero;
    }

    private bool ResolveInteract()
    {
        if (!m_brain.WantsInteract)
        {
            return false;
        }

        switch (m_brain.State)
        {
            case BotState.Revive:
                return m_downed != null && DistanceTo(m_downed.transform.position) <= m_tuning.ReviveReach;
            case BotState.Objective:
                return m_hasObjective && m_objectiveNeedsInteract && DistanceTo(m_objective) <= m_tuning.ObjectiveReach;
            default:
                return false;
        }
    }

    private float DistanceTo(Vector3 point)
    {
        return EnemyRules.FlatDistance(m_transform.position, point);
    }
}
}
