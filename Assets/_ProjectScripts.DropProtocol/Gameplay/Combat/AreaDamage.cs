using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-side radial damage. Walks the player and enemy registries instead of a physics overlap so
///     nothing depends on collider layout, and dead enemies (collider already off) are skipped explicitly.
/// </summary>
public static class AreaDamage
{
    public static int Apply(Vector3 centre, float radius, int damage)
    {
        int hit = 0;

        var players = NetworkPlayer.All;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player.Health == null || !MissionRules.IsInside(player.transform.position, centre, radius))
            {
                continue;
            }

            player.Health.ApplyDamage(damage);
            hit++;
        }

        var enemies = EnemyCharacter.All;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy.IsDead || !MissionRules.IsInside(enemy.transform.position, centre, radius))
            {
                continue;
            }

            var health = enemy.GetComponent<Health>();
            if (health == null)
            {
                continue;
            }

            health.ApplyDamage(damage);
            hit++;
        }

        return hit;
    }
}
}
