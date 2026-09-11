using UnityEngine;

namespace DropProtocol
{
public enum WeaponHitMode
{
    Hitscan,
    Projectile
}

/// <summary>
///     Static weapon configuration. Runtime state (ammo, cooldown, reload) lives in <see cref="WeaponState" />.
/// </summary>
[CreateAssetMenu(menuName = "DropProtocol/Weapon Definition", fileName = "NewWeapon")]
public sealed class WeaponDefinition : ScriptableObject
{
    [SerializeField]
    private string m_displayName = "Weapon";

    [SerializeField]
    private WeaponHitMode m_hitMode = WeaponHitMode.Hitscan;

    [SerializeField]
    [Min(0)]
    private int m_damage = 20;

    [SerializeField]
    [Min(0.01f)]
    private float m_roundsPerSecond = 10f;

    [SerializeField]
    [Min(1)]
    private int m_magazineSize = 30;

    [SerializeField]
    [Min(0f)]
    private float m_reloadSeconds = 2f;

    [SerializeField]
    [Range(0f, 45f)]
    private float m_spreadDegrees = 3f;

    [SerializeField]
    [Min(1f)]
    private float m_range = 60f;

    [Header("Presentation")]
    [SerializeField]
    private GameObject m_muzzleFlash;

    [SerializeField]
    private GameObject m_impactVfx;

    [SerializeField]
    private AudioClip m_fireClip;

    [SerializeField]
    private AudioClip m_reloadClip;

    public string DisplayName => m_displayName;
    public WeaponHitMode HitMode => m_hitMode;
    public int Damage => m_damage;
    public float RoundsPerSecond => m_roundsPerSecond;
    public double FireInterval => 1.0 / m_roundsPerSecond;
    public int MagazineSize => m_magazineSize;
    public float ReloadSeconds => m_reloadSeconds;
    public float SpreadDegrees => m_spreadDegrees;
    public float Range => m_range;
    public GameObject MuzzleFlash => m_muzzleFlash;
    public GameObject ImpactVfx => m_impactVfx;
    public AudioClip FireClip => m_fireClip;
    public AudioClip ReloadClip => m_reloadClip;

    public static WeaponDefinition Create(int damage, float roundsPerSecond, int magazineSize, float reloadSeconds,
        float spreadDegrees, float range)
    {
        var definition = CreateInstance<WeaponDefinition>();
        definition.m_damage = damage;
        definition.m_roundsPerSecond = roundsPerSecond;
        definition.m_magazineSize = magazineSize;
        definition.m_reloadSeconds = reloadSeconds;
        definition.m_spreadDegrees = spreadDegrees;
        definition.m_range = range;
        return definition;
    }
}
}
