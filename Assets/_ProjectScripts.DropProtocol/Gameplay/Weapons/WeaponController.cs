using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DropProtocol
{
/// <summary>
///     Server-side weapon: consumes the character command, resolves hitscan shots, applies damage, and
///     tells every peer where the shot went. Clients only see the result.
/// </summary>
[RequireComponent(typeof(Health))]
public sealed class WeaponController : NetworkBehaviour, IShotSource
{
    private const int HitBufferSize = 8;
    private const float DefaultMuzzleHeight = 0.9f;

    [SerializeField]
    private WeaponDefinition m_definition;

    [SerializeField]
    private Transform m_muzzle;

    [SerializeField]
    private LayerMask m_hitMask = Physics.DefaultRaycastLayers;

    public NetworkVariable<int> Ammo = new();
    public NetworkVariable<bool> IsReloading = new();

    private readonly RaycastHit[] m_hits = new RaycastHit[HitBufferSize];
    private WeaponState m_state;
    private bool m_reloadHeld;
    private bool m_warnedUnsupportedMode;

    public WeaponDefinition Definition => m_definition;

    public float MuzzleHeight => m_muzzle != null ? m_muzzle.position.y - transform.position.y : DefaultMuzzleHeight;

    public event Action<Vector3, Vector3, bool> ShotFired;

    /// <summary>Owner only: a shot just damaged a living enemy. Cosmetic, for the hit marker.</summary>
    public event Action DamageConfirmed;

    public void SetDefinition(WeaponDefinition definition)
    {
        m_definition = definition;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer || m_definition == null)
        {
            return;
        }

        m_state = new WeaponState(m_definition.MagazineSize, m_definition.FireInterval, m_definition.ReloadSeconds);
        Ammo.Value = m_state.Ammo;
    }

    /// <summary>Server only. Called by <see cref="PlayerCharacter" /> with the command it just applied.</summary>
    public void Tick(PlayerCommand command, double now)
    {
        if (!IsServer || m_state == null)
        {
            return;
        }

        m_state.Update(now);

        // Buttons are held state; reload is a rising-edge action, firing is continuous.
        bool reloadPressed = command.Reload && !m_reloadHeld;
        m_reloadHeld = command.Reload;
        if (reloadPressed || (command.Fire && m_state.Ammo == 0))
        {
            m_state.TryStartReload(now);
        }

        if (command.Fire && m_state.TryFire(now))
        {
            Fire(command.Aim);
        }

        Ammo.Value = m_state.Ammo;
        IsReloading.Value = m_state.IsReloading;
    }

    /// <summary>Server only. Fills the magazine at once and cancels any reload in progress.</summary>
    public void RefillAmmo()
    {
        if (!IsServer || m_state == null)
        {
            return;
        }

        m_state.Refill();
        Ammo.Value = m_state.Ammo;
        IsReloading.Value = m_state.IsReloading;
    }

    /// <summary>
    ///     Whether a shot aimed straight at <paramref name="target" /> hits it before anything else. Bots check
    ///     this every frame so a squadmate stepping into the line stops the shot.
    /// </summary>
    public bool HasLineOfFire(Transform target)
    {
        if (m_definition == null || target == null)
        {
            return false;
        }

        var toTarget = new Vector3(target.position.x - transform.position.x, 0f, target.position.z - transform.position.z);
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        var origin = transform.position + Vector3.up * MuzzleHeight;
        return TryResolveHit(origin, toTarget.normalized, out var closest) && closest.transform.root == target.root;
    }

    private void Fire(Vector2 commandAim)
    {
        if (m_definition.HitMode != WeaponHitMode.Hitscan)
        {
            if (!m_warnedUnsupportedMode)
            {
                Debug.LogWarning($"{m_definition.name}: {m_definition.HitMode} is not implemented yet.", this);
            }

            m_warnedUnsupportedMode = true;
            return;
        }

        var aim = WeaponMath.ResolveAim(commandAim, transform.eulerAngles.y);
        var direction = WeaponMath.SpreadDirection(aim, m_definition.SpreadDegrees, Random.value);

        // Cast from the centre line of the shooter rather than the muzzle: a ray never reports the
        // collider it starts inside, which excludes the shooter, and a wall the character is pressed
        // against still blocks the shot instead of being skipped.
        var origin = transform.position + Vector3.up * MuzzleHeight;
        var end = origin + direction * m_definition.Range;

        bool hit = TryResolveHit(origin, direction, out var closest);
        if (hit)
        {
            end = closest.point;
            var target = closest.collider.GetComponentInParent<Health>();
            if (target != null)
            {
                bool confirmed = target.Current.Value > 0 && target.GetComponent<EnemyCharacter>() != null;
                target.ApplyDamage(m_definition.Damage);
                if (confirmed)
                {
                    DamageConfirmedRpc();
                }
            }
        }

        var muzzle = m_muzzle != null ? m_muzzle.position : origin;
        ShotFiredRpc(muzzle, end, hit);
    }

    private bool TryResolveHit(Vector3 origin, Vector3 direction, out RaycastHit closest)
    {
        return Hitscan.TryResolveHit(origin, direction, m_definition.Range, m_hitMask, m_hits, transform.root,
            out closest);
    }

    // Cosmetic only; a dropped tracer is harmless because damage already travelled via NetworkVariable.
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void ShotFiredRpc(Vector3 muzzle, Vector3 end, bool hit)
    {
        ShotFired?.Invoke(muzzle, end, hit);
    }

    // Cosmetic only, and only the shooter needs it; friendly fire and dead bodies never confirm.
    [Rpc(SendTo.Owner, Delivery = RpcDelivery.Unreliable)]
    private void DamageConfirmedRpc()
    {
        DamageConfirmed?.Invoke();
    }
}
}
