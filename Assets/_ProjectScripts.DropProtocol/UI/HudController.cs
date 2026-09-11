using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Root of the uGUI HUD. Shows the session card while disconnected and the gameplay views while in a
///     session, and hands the local player to every view when it spawns. Views bind to replicated state
///     themselves; this only routes lifecycle.
/// </summary>
public sealed class HudController : MonoBehaviour
{
    [SerializeField]
    private GameObject m_sessionRoot;

    [SerializeField]
    private GameObject m_gameplayRoot;

    private HudView[] m_views = System.Array.Empty<HudView>();
    private NetworkSession m_session;

    public NetworkPlayer LocalPlayer { get; private set; }

    private void Awake()
    {
        m_views = GetComponentsInChildren<HudView>(true);
    }

    private void OnEnable()
    {
        NetworkPlayer.LocalPlayerSpawned += HandleLocalPlayerSpawned;
        NetworkPlayer.LocalPlayerDespawned += HandleLocalPlayerDespawned;
    }

    private void Start()
    {
        // The session object may be created by a sibling's Awake, so resolve it after every Awake ran.
        m_session = NetworkSession.Instance;
        if (m_session != null)
        {
            m_session.SessionStarted += HandleSessionStarted;
            m_session.SessionEnded += HandleSessionEnded;
        }

        ApplySessionState();
        if (NetworkPlayer.LocalPlayer != null)
        {
            HandleLocalPlayerSpawned(NetworkPlayer.LocalPlayer);
        }
    }

    private void OnDisable()
    {
        NetworkPlayer.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        NetworkPlayer.LocalPlayerDespawned -= HandleLocalPlayerDespawned;
        if (m_session != null)
        {
            m_session.SessionStarted -= HandleSessionStarted;
            m_session.SessionEnded -= HandleSessionEnded;
        }

        HandleLocalPlayerDespawned(LocalPlayer);
    }

    private void HandleSessionStarted()
    {
        ApplySessionState();
    }

    private void HandleSessionEnded(string reason)
    {
        ApplySessionState();
    }

    private void ApplySessionState()
    {
        bool inSession = m_session != null && m_session.IsInSession;
        if (m_sessionRoot != null)
        {
            m_sessionRoot.SetActive(!inSession);
        }

        if (m_gameplayRoot != null)
        {
            m_gameplayRoot.SetActive(inSession);
        }
    }

    private void HandleLocalPlayerSpawned(NetworkPlayer player)
    {
        LocalPlayer = player;
        foreach (var view in m_views)
        {
            view.Bind(player);
        }
    }

    private void HandleLocalPlayerDespawned(NetworkPlayer player)
    {
        if (player == null || player != LocalPlayer)
        {
            return;
        }

        LocalPlayer = null;
        foreach (var view in m_views)
        {
            view.Unbind();
        }
    }
}
}
