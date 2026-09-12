using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace DropProtocol.Tests.PlayMode
{
    /// <summary>
    /// Picks a free loopback UDP port near the one a fixture prefers. An aborted PlayMode run can leave
    /// Unity Transport's socket open inside the editor process, which would otherwise fail every host
    /// start on that port until the editor restarts.
    /// </summary>
    public static class TestPorts
    {
        private const int Attempts = 16;

        public static ushort Free(ushort preferred)
        {
            for (int offset = 0; offset < Attempts; offset++)
            {
                var port = (ushort)(preferred + offset);
                if (IsFree(port))
                    return port;
            }

            Assert.Fail($"no free UDP port in {preferred}-{preferred + Attempts - 1}");
            return preferred;
        }

        private static bool IsFree(ushort port)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.ExclusiveAddressUse = true;
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
