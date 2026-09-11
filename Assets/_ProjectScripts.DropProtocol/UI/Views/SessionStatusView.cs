using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>Small in-session strip: host or client, player count, and Leave.</summary>
public sealed class SessionStatusView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_label;

    [SerializeField]
    private Button m_leaveButton;

    private void Awake()
    {
        if (m_leaveButton != null)
        {
            m_leaveButton.onClick.AddListener(Leave);
        }
    }

    private void OnDestroy()
    {
        if (m_leaveButton != null)
        {
            m_leaveButton.onClick.RemoveListener(Leave);
        }
    }

    private void Update()
    {
        var session = NetworkSession.Instance;
        if (m_label == null || session == null)
        {
            return;
        }

        string text = session.IsHost ? $"Hosting · {session.ConnectedPlayerCount} connected" : "Client";
        if (m_label.text != text)
        {
            m_label.text = text;
        }
    }

    private static void Leave()
    {
        var session = NetworkSession.Instance;
        if (session != null)
        {
            session.Leave();
        }
    }
}
}
