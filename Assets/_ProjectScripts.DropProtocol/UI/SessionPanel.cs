using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>
///     Host / Join card shown while disconnected so the gameplay scenes never need the menu flow. If the
///     scene was opened directly (no bootstrap), it brings up the NetworkRoot itself.
/// </summary>
public sealed class SessionPanel : MonoBehaviour
{
    [Tooltip("NetworkRoot prefab instantiated when the scene is played without passing through 00_Bootstrap.")]
    [SerializeField]
    private GameObject m_networkRootPrefab;

    [SerializeField]
    private Button m_hostButton;

    [SerializeField]
    private Button m_joinButton;

    [SerializeField]
    private TMP_InputField m_address;

    [SerializeField]
    private TMP_Text m_status;

    private void Awake()
    {
        if (NetworkManager.Singleton == null && m_networkRootPrefab != null)
        {
            Instantiate(m_networkRootPrefab);
        }

        if (m_hostButton != null)
        {
            m_hostButton.onClick.AddListener(Host);
        }

        if (m_joinButton != null)
        {
            m_joinButton.onClick.AddListener(Join);
        }

        if (m_address != null)
        {
            m_address.text = SessionRules.DefaultAddress;
        }
    }

    private void OnEnable()
    {
        var session = NetworkSession.Instance;
        SetStatus(session != null ? session.LastDisconnectReason : string.Empty);
    }

    private void OnDestroy()
    {
        if (m_hostButton != null)
        {
            m_hostButton.onClick.RemoveListener(Host);
        }

        if (m_joinButton != null)
        {
            m_joinButton.onClick.RemoveListener(Join);
        }
    }

    private void Host()
    {
        var session = NetworkSession.Instance;
        if (session != null && !session.StartHost())
        {
            SetStatus("Could not start host");
        }
    }

    private void Join()
    {
        var session = NetworkSession.Instance;
        if (session == null)
        {
            return;
        }

        string address = m_address != null ? m_address.text : SessionRules.DefaultAddress;
        if (!session.StartClient(address))
        {
            SetStatus("Invalid address");
        }
    }

    private void SetStatus(string text)
    {
        if (m_status != null)
        {
            m_status.text = text ?? string.Empty;
        }
    }
}
}
