using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-side support-protocol slots for one character. Directions arrive one per reliable RPC rather
///     than inside <see cref="PlayerCommand" />: that stream is unreliable and latest-only, so a dropped tick
///     would silently eat a key press. The host matches, gates and spawns; clients only request.
/// </summary>
[RequireComponent(typeof(Health))]
public sealed class ProtocolController : NetworkBehaviour
{
    [SerializeField]
    private ProtocolDefinition[] m_loadout = Array.Empty<ProtocolDefinition>();

    [SerializeField]
    private ProtocolTuning m_tuning = ProtocolTuning.Default;

    /// <summary>Directions entered so far, packed by <see cref="ProtocolRules.Pack" />; the HUD unpacks it.</summary>
    public NetworkVariable<int> Entered = new();

    public NetworkVariable<ProtocolCooldowns> Cooldowns = new();

    private readonly List<ProtocolDirection[]> m_sequences = new();
    private Health m_health;
    private ProtocolState m_state;

    public IReadOnlyList<ProtocolDefinition> Loadout => m_loadout;
    public ProtocolTuning Tuning => m_tuning;

    /// <summary>Raised on every peer with the slot that just spawned its payload; presentation only.</summary>
    public event Action<int> Called;

    private void Awake()
    {
        m_health = GetComponent<Health>();
    }

    /// <summary>Test seam, before <c>Spawn()</c>.</summary>
    public void Configure(ProtocolDefinition[] loadout, ProtocolTuning tuning)
    {
        m_loadout = loadout;
        m_tuning = tuning;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        m_sequences.Clear();
        foreach (var definition in m_loadout)
        {
            m_sequences.Add(definition != null ? definition.Sequence : Array.Empty<ProtocolDirection>());
        }

        m_state = new ProtocolState(m_sequences, m_tuning);
    }

    private void Update()
    {
        if (!IsServer || m_state == null)
        {
            return;
        }

        m_state.Tick(Time.timeAsDouble, Time.deltaTime);
        Publish();
    }

    /// <summary>Owner-side entry; the host's own player skips the RPC.</summary>
    public void RequestDirection(ProtocolDirection direction)
    {
        if (IsServer)
        {
            SubmitDirection(direction);
        }
        else
        {
            SubmitDirectionRpc((byte)direction);
        }
    }

    /// <summary>Server-side entry point for one direction; the RPC and tests both land here.</summary>
    public void SubmitDirection(ProtocolDirection direction)
    {
        if (!IsServer || m_state == null)
        {
            return;
        }

        var result = m_state.Push(direction, Time.timeAsDouble, out int slot);
        if (result == ProtocolMatch.Matched)
        {
            TryCall(slot);
        }

        Publish();
    }

    private void TryCall(int slot)
    {
        var definition = m_loadout[slot];
        if (definition == null || definition.Payload == null)
        {
            return;
        }

        if (!ProtocolRules.CanCall(m_health.IsDowned, m_state.CooldownRemaining(slot), IsMissionOpen()))
        {
            return;
        }

        var forward = transform.forward;
        var point = ProtocolRules.TargetPoint(transform.position, forward, m_tuning.ThrowDistance);
        var flat = new Vector3(forward.x, 0f, forward.z);
        var rotation = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat) : Quaternion.identity;

        var instance = Instantiate(definition.Payload, point, rotation);
        // Test templates are inactive stand-ins for prefabs.
        if (!instance.gameObject.activeSelf)
        {
            instance.gameObject.SetActive(true);
        }

        instance.Spawn(true);
        m_state.StartCooldown(slot, definition.CooldownSeconds);
        CalledRpc((byte)slot);
    }

    // No director (sandbox, tests) leaves protocols always available.
    private static bool IsMissionOpen()
    {
        var mission = MissionDirector.Instance;
        return mission == null || MissionRules.DirectorShouldRun(mission.Phase.Value);
    }

    private void Publish()
    {
        Entered.Value = ProtocolRules.Pack(m_state.Entered);

        var cooldowns = new ProtocolCooldowns();
        int count = Mathf.Min(m_state.SlotCount, ProtocolCooldowns.SlotCount);
        for (int i = 0; i < count; i++)
        {
            cooldowns[i] = m_state.CooldownRemaining(i);
        }

        Cooldowns.Value = cooldowns;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitDirectionRpc(byte direction)
    {
        SubmitDirection((ProtocolDirection)direction);
    }

    // Cosmetic only; the payload itself is a spawned NetworkObject, so a dropped notification loses nothing.
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void CalledRpc(byte slot)
    {
        Called?.Invoke(slot);
    }
}
}
