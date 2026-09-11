using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>
///     Glue between the 01_MainMenu widgets and <see cref="NetworkSession" />. It only forwards clicks and
///     shows status; session rules and scene flow live in the session.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    private const string HostingStatus = "Starting host...";
    private const string JoiningStatus = "Connecting...";
    private const string InvalidAddressStatus = "Enter an IP address, optionally with :port";
    private const string NoSessionStatus = "Networking is not initialised. Start from 00_Bootstrap.";
    private const string SandboxScene = "90_Sandbox";

    [SerializeField]
    private Button m_hostButton;

    [SerializeField]
    private Button m_joinButton;

    [SerializeField]
    private Button m_sandboxButton;

    [SerializeField]
    private TMP_InputField m_addressInput;

    [SerializeField]
    private TMP_Text m_statusText;

    [SerializeField]
    private TMP_Text m_versionLabel;

    private void Awake()
    {
        m_hostButton.onClick.AddListener(Host);
        m_joinButton.onClick.AddListener(Join);
        if (m_sandboxButton != null)
        {
            m_sandboxButton.onClick.AddListener(OpenSandbox);
        }

        m_addressInput.text = SessionRules.DefaultAddress;
        if (m_versionLabel != null)
        {
            m_versionLabel.text = $"v{Application.version}  ·  Unity {Application.unityVersion}";
        }
    }

    private void Start()
    {
        var session = NetworkSession.Instance;
        if (session == null)
        {
            SetStatus(NoSessionStatus);
            SetInteractable(false);
            return;
        }

        // Arriving here after a session ended: tell the player why.
        SetStatus(session.LastDisconnectReason);
        SetInteractable(true);
    }

    private void OnDestroy()
    {
        m_hostButton.onClick.RemoveListener(Host);
        m_joinButton.onClick.RemoveListener(Join);
        if (m_sandboxButton != null)
        {
            m_sandboxButton.onClick.RemoveListener(OpenSandbox);
        }
    }

    private void Host()
    {
        var session = NetworkSession.Instance;
        if (session == null)
        {
            return;
        }

        SetStatus(HostingStatus);
        SetInteractable(false);

        if (!session.StartHost())
        {
            SetStatus("Could not start host");
            SetInteractable(true);
        }
    }

    private void Join()
    {
        var session = NetworkSession.Instance;
        if (session == null)
        {
            return;
        }

        if (!SessionRules.TryParseAddress(m_addressInput.text, out _, out _))
        {
            SetStatus(InvalidAddressStatus);
            return;
        }

        SetStatus(JoiningStatus);
        SetInteractable(false);

        if (!session.StartClient(m_addressInput.text))
        {
            SetStatus("Could not start client");
            SetInteractable(true);
        }
    }

    private static void OpenSandbox()
    {
        SceneManager.LoadScene(SandboxScene);
    }

    private void SetInteractable(bool interactable)
    {
        m_hostButton.interactable = interactable;
        m_joinButton.interactable = interactable;
        m_addressInput.interactable = interactable;
    }

    private void SetStatus(string text)
    {
        m_statusText.text = text ?? string.Empty;
    }
}
}
