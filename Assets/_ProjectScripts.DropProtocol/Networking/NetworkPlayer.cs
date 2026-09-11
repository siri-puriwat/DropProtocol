using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Network glue for a player character. Movement is server-authoritative: the owner samples its
///     <see cref="HumanCommandSource" /> and ships <see cref="PlayerCommand" />s to the host every network
///     tick, the host runs <see cref="PlayerCharacter" /> with those commands, and <c>NetworkTransform</c>
///     replicates the result. Non-server instances only display; their character logic is switched off.
///     Bots are server-spawned with a bot source bound before spawn and never become the local player.
/// </summary>
[RequireComponent(typeof(PlayerCharacter))]
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkPlayer : NetworkBehaviour
{
    private static readonly List<NetworkPlayer> m_all = new();

    /// <summary>Squad slot assigned by the server (0..MaxPlayers-1); drives spawn point and tint.</summary>
    public NetworkVariable<int> PlayerIndex = new(-1);

    /// <summary>Whether the server is applying a held Interact; the command itself never leaves the host.</summary>
    public NetworkVariable<bool> IsInteracting = new();

    /// <summary>Replicated so the squad list can label bots; gameplay still uses the server-side <see cref="IsBot" />.</summary>
    public NetworkVariable<bool> BotFlag = new();

    private PlayerCharacter m_character;
    private CharacterController m_controller;
    private HumanCommandSource m_humanSource;
    private NetworkCommandSource m_networkSource;
    private IPlayerCommandSource m_botSource;
    private Health m_health;

    public static NetworkPlayer LocalPlayer { get; private set; }

    /// <summary>Every spawned player on this peer, human or bot. Enemies, the director and bots read it on the server.</summary>
    public static IReadOnlyList<NetworkPlayer> All => m_all;

    public PlayerCharacter Character => m_character;
    public Health Health => m_health;

    /// <summary>Server-side truth only; nothing about a bot is replicated yet, so clients see an ordinary player.</summary>
    public bool IsBot => m_botSource != null;

    private void Awake()
    {
        m_character = GetComponent<PlayerCharacter>();
        m_controller = GetComponent<CharacterController>();
        m_humanSource = GetComponent<HumanCommandSource>();
        m_health = GetComponent<Health>();
        m_networkSource = new NetworkCommandSource(() => Time.timeAsDouble);
    }

    private void Update()
    {
        if (IsServer && IsSpawned)
        {
            IsInteracting.Value = m_character.LastCommand.Interact;
        }
    }

    // A host shutdown can destroy spawned objects without a despawn callback.
    public override void OnDestroy()
    {
        m_all.Remove(this);
        base.OnDestroy();
    }

    public static event Action<NetworkPlayer> LocalPlayerSpawned;
    public static event Action<NetworkPlayer> LocalPlayerDespawned;

    // Statics survive play sessions when domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        m_all.Clear();
        LocalPlayer = null;
    }

    public override void OnNetworkSpawn()
    {
        m_all.Add(this);

        if (IsServer)
        {
            m_character.SetCommandSource(ResolveServerSource());
            m_character.enabled = true;
            BotFlag.Value = IsBot;
        }
        else
        {
            // Presentation only: NetworkTransform owns this transform, so the motor must not apply gravity.
            m_character.enabled = false;
            if (m_controller != null)
            {
                m_controller.enabled = false;
            }
        }

        // A server-spawned bot is owned by the host but is never the local player.
        if (!IsOwner || IsBot)
        {
            return;
        }

        LocalPlayer = this;
        if (!IsServer)
        {
            NetworkManager.NetworkTickSystem.Tick += SendCommand;
        }

        LocalPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        m_all.Remove(this);

        if (!IsOwner || IsBot)
        {
            return;
        }

        if (NetworkManager != null && NetworkManager.NetworkTickSystem != null)
        {
            NetworkManager.NetworkTickSystem.Tick -= SendCommand;
        }

        if (LocalPlayer == this)
        {
            LocalPlayer = null;
        }

        LocalPlayerDespawned?.Invoke(this);
    }

    /// <summary>Server-side, before <c>Spawn()</c>: a host-owned bot would otherwise bind the host input and camera.</summary>
    public void ConfigureBot(IPlayerCommandSource source)
    {
        m_botSource = source;
        if (m_humanSource != null)
        {
            m_humanSource.enabled = false;
        }
    }

    // The host reads its own local input directly; remote owners arrive through the RPC.
    private IPlayerCommandSource ResolveServerSource()
    {
        if (m_botSource != null)
        {
            return m_botSource;
        }

        return IsOwner && m_humanSource != null ? m_humanSource : (IPlayerCommandSource)m_networkSource;
    }

    /// <summary>Server-side entry point for a replicated command; the RPC and tests both land here.</summary>
    public void SubmitCommand(PlayerCommand command)
    {
        if (!IsServer)
        {
            return;
        }

        m_networkSource.Push(command);
    }

    private void SendCommand()
    {
        if (m_humanSource == null)
        {
            return;
        }

        SubmitCommandRpc(m_humanSource.GetCommand());
    }

    // Unreliable: a lost sample is replaced by the next tick's, and the source goes idle if they stop.
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitCommandRpc(PlayerCommand command)
    {
        SubmitCommand(command);
    }
}
}
