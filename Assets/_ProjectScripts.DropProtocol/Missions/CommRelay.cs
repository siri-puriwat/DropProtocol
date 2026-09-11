using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Communication relay objective. The server counts squadmates inside the radius holding Interact
///     and fills a shared progress that persists when they let go. It reads the command each character
///     already applied instead of adding an interaction component to the player: a revive in progress
///     keeps priority because that player is excluded while <see cref="ReviveController.Target" /> is set.
/// </summary>
public sealed class CommRelay : NetworkBehaviour
{
    [SerializeField]
    [Min(0.1f)]
    private float m_radius = 3f;

    [SerializeField]
    [Min(0f)]
    private float m_activateSeconds = 4f;

    public NetworkVariable<float> Progress = new();
    public NetworkVariable<bool> IsActivated = new();

    public float Radius => m_radius;

    /// <summary>Server-side count of players charging the relay this frame.</summary>
    public int Holders { get; private set; }

    /// <summary>Tests configure in code before the object spawns.</summary>
    public void Configure(float radius, float activateSeconds)
    {
        m_radius = radius;
        m_activateSeconds = activateSeconds;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || IsActivated.Value)
        {
            return;
        }

        // Without a mission (sandbox, tests) the relay is always live.
        var mission = MissionDirector.Instance;
        if (mission != null && mission.Phase.Value != MissionPhase.Active)
        {
            Holders = 0;
            return;
        }

        Holders = CountHolders();
        float progress = MissionRules.RelayStep(Progress.Value, Holders > 0, Time.deltaTime, m_activateSeconds);
        Progress.Value = progress;
        if (MissionRules.IsRelayActivated(progress))
        {
            IsActivated.Value = true;
        }
    }

    private int CountHolders()
    {
        int count = 0;
        foreach (var player in NetworkPlayer.All)
        {
            if (player.Health == null || player.Health.IsDowned || player.Character == null)
            {
                continue;
            }

            if (!player.Character.LastCommand.Interact ||
                !MissionRules.IsInside(player.transform.position, transform.position, m_radius))
            {
                continue;
            }

            var reviver = player.GetComponent<ReviveController>();
            if (reviver != null && reviver.Target != null)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
}
}
