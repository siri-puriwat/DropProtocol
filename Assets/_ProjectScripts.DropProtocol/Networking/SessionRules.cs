using System.Net;

namespace DropProtocol
{
/// <summary>
///     Pure session rules shared by the host's connection approval and the join UI. Kept free of Unity
///     types so they are trivially unit-tested.
/// </summary>
public static class SessionRules
{
    public const int MaxPlayers = 4;
    public const ushort DefaultPort = 7777;
    public const string DefaultAddress = "127.0.0.1";
    public const string SessionFullReason = "Session is full";

    /// <summary>Whether a host with <paramref name="connectedCount" /> clients may accept one more.</summary>
    public static bool CanAccept(int connectedCount, int maxPlayers, out string reason)
    {
        if (connectedCount < maxPlayers)
        {
            reason = string.Empty;
            return true;
        }

        reason = SessionFullReason;
        return false;
    }

    /// <summary>
    ///     Parses user-entered join text: empty means the defaults, otherwise an IP address with an
    ///     optional <c>:port</c> suffix. Host names are not resolved; the transport wants an address.
    /// </summary>
    public static bool TryParseAddress(string input, out string address, out ushort port)
    {
        address = DefaultAddress;
        port = DefaultPort;

        string trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return true;
        }

        string hostPart = trimmed;
        int lastColon = trimmed.LastIndexOf(':');
        bool looksLikeIPv6 = trimmed.IndexOf(':') != lastColon;
        if (lastColon >= 0 && !looksLikeIPv6)
        {
            hostPart = trimmed.Substring(0, lastColon);
            string portPart = trimmed.Substring(lastColon + 1);
            if (!ushort.TryParse(portPart, out port) || port == 0)
            {
                return false;
            }
        }

        if (!IPAddress.TryParse(hostPart, out var parsed))
        {
            return false;
        }

        address = parsed.ToString();
        return true;
    }
}
}
