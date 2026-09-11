using UnityEngine;

namespace DropProtocol
{
/// <summary>Server-side registry lookups for bots; the squad side of <see cref="EnemyTargeting" />.</summary>
public static class BotPerception
{
    /// <summary>Bots follow humans, never each other, so two bots cannot orbit one another.</summary>
    public static NetworkPlayer NearestAliveHuman(Vector3 origin, out float flatDistance)
    {
        NetworkPlayer nearest = null;
        flatDistance = float.PositiveInfinity;

        foreach (var player in NetworkPlayer.All)
        {
            if (player.IsBot || player.Health == null || player.Health.IsDowned)
            {
                continue;
            }

            float distance = EnemyRules.FlatDistance(origin, player.transform.position);
            if (distance < flatDistance)
            {
                flatDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    public static NetworkPlayer NearestDownedTeammate(Vector3 origin, NetworkPlayer self, out float flatDistance)
    {
        NetworkPlayer nearest = null;
        flatDistance = float.PositiveInfinity;

        foreach (var player in NetworkPlayer.All)
        {
            if (player == self || player.Health == null || !player.Health.IsDowned)
            {
                continue;
            }

            float distance = EnemyRules.FlatDistance(origin, player.transform.position);
            if (distance < flatDistance)
            {
                flatDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    /// <summary>The mission objective for the squad, if the scene has a mission and it has one right now.</summary>
    public static bool SquadObjective(Vector3 origin, out Vector3 position, out bool needsInteract,
        out float flatDistance)
    {
        var mission = MissionDirector.Instance;
        if (mission == null || !mission.TryGetObjective(origin, out position, out needsInteract))
        {
            position = default;
            needsInteract = false;
            flatDistance = float.PositiveInfinity;
            return false;
        }

        flatDistance = EnemyRules.FlatDistance(origin, position);
        return true;
    }

    public static EnemyCharacter NearestAliveEnemy(Vector3 origin, out float flatDistance)
    {
        EnemyCharacter nearest = null;
        flatDistance = float.PositiveInfinity;

        foreach (var enemy in EnemyCharacter.All)
        {
            if (enemy.IsDead)
            {
                continue;
            }

            float distance = EnemyRules.FlatDistance(origin, enemy.transform.position);
            if (distance < flatDistance)
            {
                flatDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
}
