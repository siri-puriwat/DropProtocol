using TMPro;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DropProtocol
{
/// <summary>
///     F3 network overlay: the Multiplayer Tools runtime net stats monitor (RTT, bytes, deltas, RPCs) plus
///     a one-line readout of tick rate, transport RTT and object counts. Release builds need the
///     UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE define for the monitor to collect anything.
/// </summary>
public sealed class NetworkDebugOverlay : MonoBehaviour
{
    private const string ToggleActionPath = "Debug/ToggleNetStats";

    [SerializeField]
    private RuntimeNetStatsMonitor m_monitor;

    [SerializeField]
    private GameObject m_root;

    [SerializeField]
    private TMP_Text m_readout;

    private InputAction m_toggle;

    public bool IsShown { get; private set; }
    public string Readout => m_readout != null ? m_readout.text : string.Empty;

    private void Awake()
    {
        var actions = InputSystem.actions;
        m_toggle = actions != null ? actions.FindAction(ToggleActionPath, false) : null;
        Show(false);
    }

    private void OnEnable()
    {
        if (m_toggle != null)
        {
            m_toggle.performed += HandleToggle;
            m_toggle.Enable();
        }
    }

    private void OnDisable()
    {
        if (m_toggle != null)
        {
            m_toggle.performed -= HandleToggle;
        }
    }

    private void Update()
    {
        if (!IsShown || m_readout == null)
        {
            return;
        }

        m_readout.text = BuildReadout();
    }

    public void Show(bool shown)
    {
        IsShown = shown;
        if (m_monitor != null)
        {
            m_monitor.Visible = shown;
        }

        if (m_root != null)
        {
            m_root.SetActive(shown);
        }

        if (shown && m_readout != null)
        {
            m_readout.text = BuildReadout();
        }
    }

    private void HandleToggle(InputAction.CallbackContext context)
    {
        Show(!IsShown);
    }

    private static string BuildReadout()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
        {
            return "No session";
        }

        string role = manager.IsHost ? "Host" : manager.IsServer ? "Server" : "Client";
        uint tickRate = manager.NetworkConfig.TickRate;
        ulong rtt = 0;
        if (!manager.IsServer && manager.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            rtt = transport.GetCurrentRtt(NetworkManager.ServerClientId);
        }

        int clients = manager.IsServer ? manager.ConnectedClientsIds.Count : 1;
        int objects = manager.SpawnManager != null ? manager.SpawnManager.SpawnedObjects.Count : 0;
        MissionMap map = MissionMap.Instance;
        string seed = map != null && map.Layout != null ? $"  seed {map.Layout.Seed}" : string.Empty;
        return $"{role}  tick {tickRate} Hz  RTT {rtt} ms  clients {clients}  objects {objects}{seed}";
    }
}
}
