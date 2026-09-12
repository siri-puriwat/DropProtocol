using System;
using System.Collections.Generic;
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

    [Tooltip("Weapons this character can be switched to besides the starting one.")]
    [SerializeField]
    private WeaponDefinition[] m_arsenal = Array.Empty<WeaponDefinition>();

    [SerializeField]
    private Transform m_muzzle;

    [SerializeField]
    private LayerMask m_hitMask = Physics.DefaultRaycastLayers;

    public NetworkVariable<int> Ammo = new();
    public NetworkVariable<bool> IsReloading = new();

    /// <summary>Index into <see cref="Weapons" />; server-written, so every peer resolves the same definition.</summary>
    public NetworkVariable<int> WeaponIndex = new();

    private readonly RaycastHit[] m_hits = new RaycastHit[HitBufferSize];
    private WeaponDefinition[] m_weapons;
    private WeaponState m_state;
    private bool m_reloadHeld;
    private bool m_warnedUnsupportedMode;

    /// <summary>The starting weapon followed by the arsenal, nulls skipped.</summary>
    public IReadOnlyList<WeaponDefinition> Weapons
    {
        get
        {
            if (m_weapons == null)
            {
                BuildWeapons();
            }

            return m_weapons;
        }
    }

    public WeaponDefinition Definition
    {
        get
        {
            var weapons = Weapons;
            if (weapons.Count == 0)
            {
                return null;
            }

            int index = WeaponIndex.Value;
            return weapons[index >= 0 && index < weapons.Count ? index : 0];
        }
    }

    public float MuzzleHeight => m_muzzle != null ? m_muzzle.position.y - transform.position.y : DefaultMuzzleHeight;

    public event Action<Vector3, Vector3, bool> ShotFired;

    /// <summary>Owner only: a shot just damaged a living enemy. Cosmetic, for the hit marker.</summary>
    public event Action DamageConfirmed;

    /// <summary>Raised on every peer when the equipped definition changes.</summary>
    public event Action<WeaponDefinition> WeaponChanged;

    public void SetDefinition(WeaponDefinition definition, params WeaponDefinition[] arsenal)
    {
        m_definition = definition;
        m_arsenal = arsenal ?? Array.Empty<WeaponDefinition>();
        m_weapons = null;
    }

    public override void OnNetworkSpawn()
    {
        WeaponIndex.OnValueChanged += HandleWeaponIndexChanged;
        if (!IsServer || Definition == null)
        {
            return;
        }

        ResetState();
    }

    public override void OnNetworkDespawn()
    {
        WeaponIndex.OnValueChanged -= HandleWeaponIndexChanged;
    }

    /// <summary>Server only. Switches to <see cref="Weapons" />[<paramref name="index" />] with a full magazine.</summary>
    public bool Equip(int index)
    {
        if (!IsServer || index < 0 || index >= Weapons.Count || index == WeaponIndex.Value)
        {
            return false;
        }

        WeaponIndex.Value = index;
        ResetState();
        return true;
    }

    /// <summary>Server only. Cycles through <see cref="Weapons" />.</summary>
    public bool EquipNext()
    {
        return Weapons.Count > 1 && Equip((WeaponIndex.Value + 1) % Weapons.Count);
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
        if (Definition == null || target == null)
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
        var definition = Definition;
        if (definition.HitMode != WeaponHitMode.Hitscan)
        {
            if (!m_warnedUnsupportedMode)
            {
                Debug.LogWarning($"{definition.name}: {definition.HitMode} is not implemented yet.", this);
            }

            m_warnedUnsupportedMode = true;
            return;
        }

        var aim = WeaponMath.ResolveAim(commandAim, transform.eulerAngles.y);

        // Cast from the centre line of the shooter rather than the muzzle: a ray never reports the
        // collider it starts inside, which excludes the shooter, and a wall the character is pressed
        // against still blocks the shot instead of being skipped.
        var origin = transform.position + Vector3.up * MuzzleHeight;
        var muzzle = m_muzzle != null ? m_muzzle.position : origin;

        bool confirmed = false;
        for (int pellet = 0; pellet < definition.PelletCount; pellet++)
        {
            var direction = WeaponMath.SpreadDirection(aim, definition.SpreadDegrees, Random.value);
            var end = origin + direction * definition.Range;

            bool hit = TryResolveHit(origin, direction, out var closest);
            if (hit)
            {
                end = closest.point;
                var target = closest.collider.GetComponentInParent<Health>();
                if (target != null)
                {
                    confirmed |= target.Current.Value > 0 && target.GetComponent<EnemyCharacter>() != null;
                    target.ApplyDamage(definition.Damage);
                }
            }

            ShotFiredRpc(muzzle, end, hit);
        }

        if (confirmed)
        {
            DamageConfirmedRpc();
        }
    }

    private bool TryResolveHit(Vector3 origin, Vector3 direction, out RaycastHit closest)
    {
        return Hitscan.TryResolveHit(origin, direction, Definition.Range, m_hitMask, m_hits, transform.root,
            out closest);
    }

    private void ResetState()
    {
        var definition = Definition;
        m_state = new WeaponState(definition.MagazineSize, definition.FireInterval, definition.ReloadSeconds);
        m_warnedUnsupportedMode = false;
        Ammo.Value = m_state.Ammo;
        IsReloading.Value = m_state.IsReloading;
    }

    private void BuildWeapons()
    {
        var weapons = new List<WeaponDefinition>(1 + m_arsenal.Length);
        if (m_definition != null)
        {
            weapons.Add(m_definition);
        }

        foreach (WeaponDefinition definition in m_arsenal)
        {
            if (definition != null)
            {
                weapons.Add(definition);
            }
        }

        m_weapons = weapons.ToArray();
    }

    private void HandleWeaponIndexChanged(int previous, int current)
    {
        WeaponChanged?.Invoke(Definition);
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
