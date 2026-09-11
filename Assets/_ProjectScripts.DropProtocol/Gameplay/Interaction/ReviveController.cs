using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-side hold-to-revive. Progress lives on the reviver so two players reviving the same body never
///     contend over one value; the second completion is simply a no-op on an already standing target.
/// </summary>
public sealed class ReviveController : NetworkBehaviour
{
    private const int OverlapBufferSize = 8;
    private const float ProbeHeight = 0.9f;

    [SerializeField]
    [Min(0.1f)]
    private float m_radius = 2f;

    [SerializeField]
    [Min(0f)]
    private float m_reviveSeconds = 3f;

    [SerializeField]
    [Range(0.05f, 1f)]
    private float m_reviveHealthFraction = 0.5f;

    public NetworkVariable<float> Progress = new();

    private readonly Collider[] m_overlaps = new Collider[OverlapBufferSize];
    private Health m_target;

    public float ReviveSeconds => m_reviveSeconds;
    public float ReviveHealthFraction => m_reviveHealthFraction;
    public Health Target => m_target;

    /// <summary>Server only. Called by <see cref="PlayerCharacter" /> with the command it just applied.</summary>
    public void Tick(PlayerCommand command, float deltaTime)
    {
        if (!IsServer)
        {
            return;
        }

        var target = command.Interact ? FindDownedTeammate() : null;
        bool holdingOnSameTarget = target != null && target == m_target;
        float progress = ReviveRules.Step(Progress.Value, holdingOnSameTarget, deltaTime, m_reviveSeconds);
        m_target = target;

        if (target != null && ReviveRules.IsComplete(progress))
        {
            target.Revive(HealthRules.ReviveHitPoints(target.Max, m_reviveHealthFraction));
            progress = 0f;
            m_target = null;
        }

        Progress.Value = progress;
    }

    private Health FindDownedTeammate()
    {
        var centre = transform.position + Vector3.up * ProbeHeight;
        int count = Physics.OverlapSphereNonAlloc(centre, m_radius, m_overlaps, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        Health nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var candidate = m_overlaps[i].GetComponentInParent<Health>();
            if (candidate == null || candidate.gameObject == gameObject || !candidate.IsDowned)
            {
                continue;
            }

            // Only squadmates can be revived; dummies and (later) enemies carry Health but no NetworkPlayer.
            if (candidate.GetComponent<NetworkPlayer>() == null)
            {
                continue;
            }

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
}
