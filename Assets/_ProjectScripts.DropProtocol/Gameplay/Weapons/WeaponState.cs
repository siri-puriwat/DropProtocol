namespace DropProtocol
{
/// <summary>
///     Mutable per-weapon runtime state. Time is passed in so fire rate and reload are testable without a scene.
/// </summary>
public sealed class WeaponState
{
    private readonly int m_magazineSize;
    private readonly double m_fireInterval;
    private readonly double m_reloadSeconds;

    private double m_nextFireTime;
    private double m_reloadEndsAt;

    public WeaponState(int magazineSize, double fireInterval, double reloadSeconds)
    {
        m_magazineSize = magazineSize;
        m_fireInterval = fireInterval;
        m_reloadSeconds = reloadSeconds;
        Ammo = magazineSize;
    }

    public int Ammo { get; private set; }
    public bool IsReloading { get; private set; }

    public void Update(double now)
    {
        if (!IsReloading || now < m_reloadEndsAt)
        {
            return;
        }

        IsReloading = false;
        Ammo = m_magazineSize;
    }

    public bool CanFire(double now)
    {
        return !IsReloading && Ammo > 0 && now >= m_nextFireTime;
    }

    public bool TryFire(double now)
    {
        if (!CanFire(now))
        {
            return false;
        }

        Ammo--;
        m_nextFireTime = now + m_fireInterval;
        return true;
    }

    public void Refill()
    {
        Ammo = m_magazineSize;
        IsReloading = false;
    }

    public bool TryStartReload(double now)
    {
        if (IsReloading || Ammo >= m_magazineSize)
        {
            return false;
        }

        IsReloading = true;
        m_reloadEndsAt = now + m_reloadSeconds;
        return true;
    }
}
}
