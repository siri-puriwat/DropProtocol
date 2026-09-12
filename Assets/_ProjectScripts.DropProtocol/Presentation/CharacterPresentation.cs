using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Presentation: drives the player animator and hit flash from replicated state. Locomotion comes from
///     the transform NetworkTransform moves, so host and clients animate identically without NetworkAnimator.
///     Never writes gameplay.
/// </summary>
public sealed class CharacterPresentation : NetworkBehaviour
{
    private static readonly int MoveXId = Animator.StringToHash("MoveX");
    private static readonly int MoveYId = Animator.StringToHash("MoveY");
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int IsDownedId = Animator.StringToHash("IsDowned");
    private static readonly int IsReloadingId = Animator.StringToHash("IsReloading");
    private static readonly int IsInteractingId = Animator.StringToHash("IsInteracting");
    private static readonly int FireId = Animator.StringToHash("Fire");
    private static readonly int ThrowId = Animator.StringToHash("Throw");
    private static readonly int GetUpId = Animator.StringToHash("GetUp");

    [SerializeField]
    private Animator m_animator;

    [SerializeField]
    private Transform m_visual;

    [Tooltip("Where the flash appears on this peer. The replicated muzzle stays the gameplay point.")]
    [SerializeField]
    private Transform m_muzzleVisual;

    [SerializeField]
    [Min(0f)]
    private float m_deadzone = 0.05f;

    [SerializeField]
    [Min(0f)]
    private float m_sharpness = 12f;

    [SerializeField]
    [Min(0f)]
    private float m_hitFlashSeconds = 0.1f;

    [SerializeField]
    private Color m_hitColor = new(1f, 0.35f, 0.35f);

    [SerializeField]
    private GameObject m_reviveVfx;

    [SerializeField]
    private SfxCue m_hurtCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_downedCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_reviveCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_footstepCue = SfxCue.Default;

    [SerializeField]
    [Min(0.2f)]
    private float m_strideMetres = 1.6f;

    private CharacterMotor m_motor;
    private Health m_health;
    private WeaponController m_weapon;
    private NetworkPlayer m_player;
    private ProtocolController m_protocols;
    private LocomotionFeed m_locomotion;
    private RendererTint m_tint;
    private float m_flashUntil;
    private bool m_flashing;
    private float m_stride;

    public PresentationSnapshot Snapshot { get; private set; }

    private void Awake()
    {
        m_motor = GetComponent<CharacterMotor>();
        m_health = GetComponent<Health>();
        m_weapon = GetComponent<WeaponController>();
        m_player = GetComponent<NetworkPlayer>();
        m_protocols = GetComponent<ProtocolController>();
        m_locomotion = new LocomotionFeed(m_deadzone, m_sharpness);
        m_tint = new RendererTint(m_visual != null ? m_visual.GetComponentsInChildren<Renderer>(true) : null);
    }

    private void OnEnable()
    {
        if (m_health != null)
        {
            m_health.Current.OnValueChanged += HandleHealthChanged;
        }

        if (m_weapon != null)
        {
            m_weapon.IsReloading.OnValueChanged += HandleReloadingChanged;
            m_weapon.ShotFired += HandleShotFired;
        }

        if (m_player != null)
        {
            m_player.IsInteracting.OnValueChanged += HandleInteractingChanged;
        }

        if (m_protocols != null)
        {
            m_protocols.Called += HandleProtocolCalled;
        }

        ApplyReplicatedState();
        m_locomotion.Reset(transform.position);
    }

    private void OnDisable()
    {
        if (m_health != null)
        {
            m_health.Current.OnValueChanged -= HandleHealthChanged;
        }

        if (m_weapon != null)
        {
            m_weapon.IsReloading.OnValueChanged -= HandleReloadingChanged;
            m_weapon.ShotFired -= HandleShotFired;
        }

        if (m_player != null)
        {
            m_player.IsInteracting.OnValueChanged -= HandleInteractingChanged;
        }

        if (m_protocols != null)
        {
            m_protocols.Called -= HandleProtocolCalled;
        }

        m_flashing = false;
        m_tint.Clear();
    }

    // NGO writes the initial NetworkVariable values without OnValueChanged, and a client has run OnEnable by then.
    public override void OnNetworkSpawn()
    {
        ApplyReplicatedState();
    }

    private void ApplyReplicatedState()
    {
        if (m_health != null)
        {
            SetBool(IsDownedId, m_health.IsDowned);
        }

        if (m_weapon != null)
        {
            SetBool(IsReloadingId, m_weapon.IsReloading.Value);
        }

        if (m_player != null)
        {
            SetBool(IsInteractingId, m_player.IsInteracting.Value);
        }
    }

    private void Update()
    {
        float moveSpeed = m_motor != null ? m_motor.MoveSpeed : 0f;
        var move = m_locomotion.Step(transform.position, transform.eulerAngles.y, moveSpeed, Time.deltaTime);

        if (m_animator != null)
        {
            m_animator.SetFloat(MoveXId, move.x);
            m_animator.SetFloat(MoveYId, move.y);
            m_animator.SetFloat(SpeedId, move.magnitude);
        }

        var snapshot = Snapshot;
        snapshot.Move = move;
        Snapshot = snapshot;

        bool downed = m_health != null && m_health.IsDowned;
        if (!downed && LocomotionRules.Stride(ref m_stride, move, moveSpeed, Time.deltaTime, m_strideMetres))
        {
            SfxPlayer.Play(m_footstepCue, transform.position);
        }
        else if (downed)
        {
            m_stride = 0f;
        }

        if (m_flashing && Time.time >= m_flashUntil)
        {
            m_flashing = false;
            m_tint.Clear();
        }
    }

    private void HandleHealthChanged(int previous, int current)
    {
        SetBool(IsDownedId, current <= 0);

        if (previous <= 0 && current > 0)
        {
            SetTrigger(GetUpId);
            Vfx.Spawn(m_reviveVfx, transform.position, Vector3.up);
            SfxPlayer.Play(m_reviveCue, transform.position);
        }
        else if (current < previous)
        {
            m_flashUntil = Time.time + m_hitFlashSeconds;
            m_flashing = true;
            m_tint.Apply(m_hitColor);
            SfxPlayer.Play(current <= 0 ? m_downedCue : m_hurtCue, transform.position);
        }
    }

    private void HandleReloadingChanged(bool previous, bool current)
    {
        SetBool(IsReloadingId, current);
        if (current && !previous && m_weapon.Definition != null)
        {
            SfxPlayer.Play(m_weapon.Definition.ReloadCue, transform.position);
        }
    }

    private void HandleInteractingChanged(bool previous, bool current)
    {
        SetBool(IsInteractingId, current);
    }

    private void HandleShotFired(Vector3 muzzle, Vector3 end, bool hit)
    {
        SetTrigger(FireId);

        var definition = m_weapon.Definition;
        if (definition == null)
        {
            return;
        }

        // The RPC carries the server muzzle; the flash sits on the barrel this peer is drawing.
        var flashOrigin = m_muzzleVisual != null ? m_muzzleVisual.position : muzzle;
        Vfx.Spawn(definition.MuzzleFlash, flashOrigin, end - flashOrigin);
        SfxPlayer.Play(definition.FireCue, flashOrigin);
        if (hit)
        {
            Vfx.Spawn(definition.ImpactVfx, end, muzzle - end);
            SfxPlayer.Play(definition.ImpactCue, end);
        }
    }

    private void HandleProtocolCalled(int slot)
    {
        SetTrigger(ThrowId);
        var loadout = m_protocols.Loadout;
        if (slot >= 0 && slot < loadout.Count && loadout[slot] != null)
        {
            SfxPlayer.Play(loadout[slot].CallCue, transform.position);
        }
    }

    private void SetBool(int id, bool value)
    {
        if (m_animator != null)
        {
            m_animator.SetBool(id, value);
        }

        var snapshot = Snapshot;
        if (id == IsDownedId)
        {
            snapshot.IsDowned = value;
        }
        else if (id == IsReloadingId)
        {
            snapshot.IsReloading = value;
        }
        else if (id == IsInteractingId)
        {
            snapshot.IsInteracting = value;
        }

        Snapshot = snapshot;
    }

    private void SetTrigger(int id)
    {
        if (m_animator != null)
        {
            m_animator.SetTrigger(id);
        }
    }
}
}
