using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-side spawner living in the gameplay scene. It gives every connected client a player
///     character at a free spawn point, both for clients already present when the scene comes up and
///     for late joiners, and can keep the remaining squad slots filled with bots. Living in the scene
///     (rather than using NetworkConfig.PlayerPrefab) guarantees there is ground under the player when
///     it appears.
/// </summary>
public sealed class PlayerSpawner : NetworkBehaviour
{
    [SerializeField]
    private NetworkObject m_playerPrefab;

    [Tooltip("One per squad slot; the slot index doubles as the player's PlayerIndex.")]
    [SerializeField]
    private Transform[] m_spawnPoints = new Transform[0];

    [Tooltip("Keep every slot without a human filled by a bot. A joining human evicts the last bot.")]
    [SerializeField]
    private bool m_fillWithBots;

    [SerializeField]
    private BotTuning m_botTuning = BotTuning.Default;

    private readonly Dictionary<ulong, int> m_slotsByClient = new();
    private readonly Dictionary<int, NetworkObject> m_botsBySlot = new();
    private readonly List<int> m_takenSlots = new();

    public int BotCount => m_botsBySlot.Count;

    public bool FillWithBots
    {
        get => m_fillWithBots;
        set
        {
            m_fillWithBots = value;
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            if (value)
            {
                FillBots();
            }
            else
            {
                RemoveBots();
            }
        }
    }

    /// <summary>Tests configure in code before the object spawns.</summary>
    public void Configure(NetworkObject playerPrefab, Transform[] spawnPoints, BotTuning botTuning, bool fillWithBots)
    {
        m_playerPrefab = playerPrefab;
        m_spawnPoints = spawnPoints;
        m_botTuning = botTuning;
        m_fillWithBots = fillWithBots;
    }

    /// <summary>The mission map hands over the anchors of the assembled layout before this object spawns.</summary>
    public void SetSpawnPoints(Transform[] spawnPoints)
    {
        m_spawnPoints = spawnPoints;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        NetworkManager.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        // Clients that connected while the host was still in the menu are waiting for a character.
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            SpawnFor(clientId);
        }

        if (m_fillWithBots)
        {
            FillBots();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Netcode tears the spawned objects down with the session; only the bookkeeping is ours.
        m_slotsByClient.Clear();
        m_botsBySlot.Clear();

        if (NetworkManager == null)
        {
            return;
        }

        NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        SpawnFor(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        m_slotsByClient.Remove(clientId);
        if (m_fillWithBots)
        {
            FillBots();
        }
    }

    private void SpawnFor(ulong clientId)
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject != null)
        {
            return;
        }

        int slot = SpawnSlots.NextFree(TakenSlots(), m_spawnPoints.Length);
        if (slot < 0 && m_botsBySlot.Count > 0)
        {
            // Connection approval only counts humans, so a joining human always outranks a bot.
            EvictBot(SpawnSlots.SlotToEvict(m_botsBySlot.Keys));
            slot = SpawnSlots.NextFree(TakenSlots(), m_spawnPoints.Length);
        }

        if (slot < 0)
        {
            Debug.LogWarning($"No free spawn slot for client {clientId}; connection approval should have refused it.",
                this);
            return;
        }

        var instance = InstantiateAt(slot);
        instance.SpawnAsPlayerObject(clientId, true);
        instance.GetComponent<NetworkPlayer>().PlayerIndex.Value = slot;
        m_slotsByClient[clientId] = slot;
    }

    private void SpawnBot(int slot)
    {
        var instance = InstantiateAt(slot);
        instance.gameObject.name = "Bot " + slot;
        var player = instance.GetComponent<NetworkPlayer>();
        player.ConfigureBot(new BotCommandSource(player, m_botTuning, () => Time.timeAsDouble));
        // A plain server-owned spawn: SpawnAsPlayerObject would replace the host's own player object.
        instance.Spawn(true);
        player.PlayerIndex.Value = slot;
        m_botsBySlot[slot] = instance;
    }

    private NetworkObject InstantiateAt(int slot)
    {
        var point = m_spawnPoints[slot];
        // Position must be right at instantiation: a CharacterController caches its position on creation.
        var instance = Instantiate(m_playerPrefab, point.position, point.rotation);
        // Test templates are inactive stand-ins for prefabs.
        if (!instance.gameObject.activeSelf)
        {
            instance.gameObject.SetActive(true);
        }

        return instance;
    }

    private void FillBots()
    {
        int slot;
        while ((slot = SpawnSlots.NextFree(TakenSlots(), m_spawnPoints.Length)) >= 0)
        {
            SpawnBot(slot);
        }
    }

    private void RemoveBots()
    {
        foreach (int slot in new List<int>(m_botsBySlot.Keys))
        {
            EvictBot(slot);
        }
    }

    private void EvictBot(int slot)
    {
        if (!m_botsBySlot.TryGetValue(slot, out var bot))
        {
            return;
        }

        m_botsBySlot.Remove(slot);
        if (bot != null && bot.IsSpawned)
        {
            bot.Despawn();
        }
    }

    private List<int> TakenSlots()
    {
        m_takenSlots.Clear();
        m_takenSlots.AddRange(m_slotsByClient.Values);
        m_takenSlots.AddRange(m_botsBySlot.Keys);
        return m_takenSlots;
    }
}
}
