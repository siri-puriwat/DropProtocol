using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Launches a server-owned <see cref="SpitProjectile" /> at the target when the windup completes.</summary>
[RequireComponent(typeof(EnemyCharacter))]
public sealed class SpitAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField]
    private NetworkObject m_projectilePrefab;

    [SerializeField]
    [Min(0f)]
    private float m_muzzleHeight = 0.9f;

    [Tooltip("Spawn offset along the launch direction so the projectile starts clear of the shooter capsule.")]
    [SerializeField]
    [Min(0f)]
    private float m_muzzleForward = 0.8f;

    [SerializeField]
    [Min(0.1f)]
    private float m_projectileSpeed = 12f;

    [SerializeField]
    [Min(0.1f)]
    private float m_projectileLifetime = 1.5f;

    private EnemyCharacter m_enemy;

    public float ProjectileSpeed => m_projectileSpeed;

    private void Awake()
    {
        m_enemy = GetComponent<EnemyCharacter>();
    }

    public void Perform(Health target)
    {
        if (target == null || m_projectilePrefab == null)
        {
            return;
        }

        var origin = transform.position + Vector3.up * m_muzzleHeight;
        var direction = ProjectileMath.LaunchDirection(origin, target.transform.position);
        var muzzle = origin + direction * m_muzzleForward;

        var instance = Instantiate(m_projectilePrefab, muzzle, Quaternion.LookRotation(direction));
        // Test templates are inactive stand-ins for prefabs.
        if (!instance.gameObject.activeSelf)
        {
            instance.gameObject.SetActive(true);
        }

        instance.Spawn(true);
        instance.GetComponent<SpitProjectile>().Launch(direction, m_enemy.Definition.AttackDamage, m_projectileSpeed,
            m_projectileLifetime, transform.root);
    }

    public void SetProjectilePrefab(NetworkObject prefab)
    {
        m_projectilePrefab = prefab;
    }
}
}
