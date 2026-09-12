using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DropProtocol
{
/// <summary>
///     Thin application-level wrapper around <see cref="NetworkManager" />: starts and stops host/client
///     sessions, approves connections with <see cref="SessionRules" />, loads the gameplay scene for the
///     host, and returns everyone to the menu when a session ends. Lives on the persistent NetworkRoot
///     prefab next to the NetworkManager; UI talks to this, never to the NetworkManager directly.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public sealed class NetworkSession : MonoBehaviour
{
    private const string ConnectionLostReason = "Connection lost";
    private const string CouldNotConnectReason = "Could not connect to host";
    private const string TransportFailureReason = "Network transport failed";

    [Tooltip("Scene the host loads through the NetworkSceneManager once the session is up.")]
    [SerializeField]
    private string m_gameplayScene = "90_Sandbox";

    [Tooltip("Scene loaded locally when a session ends for any reason.")]
    [SerializeField]
    private string m_menuScene = "01_MainMenu";

    [SerializeField]
    private int m_maxPlayers = SessionRules.MaxPlayers;

    private NetworkManager m_networkManager;
    private bool m_leaveRequested;
    private bool m_wasConnected;
    private bool m_stopHandled;

    public static NetworkSession Instance { get; private set; }

    public bool IsInSession => m_networkManager != null && m_networkManager.IsListening;
    public bool IsHost => IsInSession && m_networkManager.IsHost;

    public int ConnectedPlayerCount =>
        IsInSession && m_networkManager.IsServer ? m_networkManager.ConnectedClientsIds.Count : 0;

    /// <summary>Why the last session ended, or empty when the local player left on purpose.</summary>
    public string LastDisconnectReason { get; private set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        m_networkManager = GetComponent<NetworkManager>();
        m_networkManager.NetworkConfig.ConnectionApproval = true;
        m_networkManager.ConnectionApprovalCallback = ApproveConnection;
        m_networkManager.OnConnectionEvent += HandleConnectionEvent;
        m_networkManager.OnClientStopped += HandleClientStopped;
        m_networkManager.OnServerStopped += HandleServerStopped;
        m_networkManager.OnTransportFailure += HandleTransportFailure;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (m_networkManager == null)
        {
            return;
        }

        m_networkManager.OnConnectionEvent -= HandleConnectionEvent;
        m_networkManager.OnClientStopped -= HandleClientStopped;
        m_networkManager.OnServerStopped -= HandleServerStopped;
        m_networkManager.OnTransportFailure -= HandleTransportFailure;
    }

    public event Action SessionStarted;
    public event Action<string> SessionEnded;

    public bool StartHost()
    {
        if (IsInSession)
        {
            return false;
        }

        ConfigureTransport(SessionRules.DefaultAddress, SessionRules.DefaultPort);
        ResetSessionFlags();

        if (!m_networkManager.StartHost())
        {
            return false;
        }

        // Only the menu hands over to the gameplay scene. Hosting straight from the sandbox or the
        // mission scene keeps the scene that is already up; Netcode treats its in-scene NetworkObjects
        // as loaded on StartHost.
        if (SceneManager.GetActiveScene().name == m_menuScene)
        {
            m_networkManager.SceneManager.LoadScene(m_gameplayScene, LoadSceneMode.Single);
        }

        SessionStarted?.Invoke();
        return true;
    }

    public bool StartClient(string addressInput)
    {
        if (IsInSession)
        {
            return false;
        }

        if (!SessionRules.TryParseAddress(addressInput, out string address, out ushort port))
        {
            return false;
        }

        ConfigureTransport(address, port);
        ResetSessionFlags();

        if (!m_networkManager.StartClient())
        {
            return false;
        }

        SessionStarted?.Invoke();
        return true;
    }

    /// <summary>Host only: reloads the gameplay scene through Netcode so every peer follows and respawns.</summary>
    public bool ReloadGameplayScene()
    {
        if (!IsHost)
        {
            return false;
        }

        SceneEventProgressStatus status = m_networkManager.SceneManager.LoadScene(m_gameplayScene, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Could not reload {m_gameplayScene}: {status}.", this);
            return false;
        }

        return true;
    }

    public void Leave()
    {
        if (!IsInSession)
        {
            return;
        }

        m_leaveRequested = true;
        m_networkManager.Shutdown();
    }

    private void ConfigureTransport(string address, ushort port)
    {
        var transport = m_networkManager.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport == null)
        {
            Debug.LogError("NetworkSession expects a UnityTransport on the NetworkManager.", this);
            return;
        }

        // Listen on all interfaces so another machine on the LAN can join; connect to the given address.
        transport.SetConnectionData(address, port, "0.0.0.0");
    }

    private void ResetSessionFlags()
    {
        m_leaveRequested = false;
        m_wasConnected = false;
        m_stopHandled = false;
        LastDisconnectReason = string.Empty;
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Player objects are spawned by the gameplay scene's PlayerSpawner, never by the NetworkManager,
        // so a client approved while the host is still in the menu does not fall through an empty scene.
        response.CreatePlayerObject = false;
        response.Pending = false;
        response.Approved =
            SessionRules.CanAccept(m_networkManager.ConnectedClientsIds.Count, m_maxPlayers, out string reason);
        response.Reason = reason;
    }

    private void HandleConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == ConnectionEvent.ClientConnected && data.ClientId == manager.LocalClientId)
        {
            m_wasConnected = true;
        }
    }

    private void HandleClientStopped(bool wasHost)
    {
        HandleSessionStopped();
    }

    private void HandleServerStopped(bool wasHost)
    {
        HandleSessionStopped();
    }

    private void HandleTransportFailure()
    {
        LastDisconnectReason = TransportFailureReason;
    }

    private void HandleSessionStopped()
    {
        // Host shutdown raises both the server and client stopped callbacks; end the session once.
        if (m_stopHandled)
        {
            return;
        }

        m_stopHandled = true;
        LastDisconnectReason = ResolveDisconnectReason();
        SessionEnded?.Invoke(LastDisconnectReason);

        if (SceneManager.GetActiveScene().name != m_menuScene)
        {
            SceneManager.LoadScene(m_menuScene, LoadSceneMode.Single);
        }
    }

    private string ResolveDisconnectReason()
    {
        if (m_leaveRequested)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(LastDisconnectReason))
        {
            return LastDisconnectReason;
        }

        if (!string.IsNullOrEmpty(m_networkManager.DisconnectReason))
        {
            return m_networkManager.DisconnectReason;
        }

        return m_wasConnected ? ConnectionLostReason : CouldNotConnectReason;
    }
}
}
