using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class SessionRulesTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void CanAccept_BelowMax_AcceptsWithoutReason(int connected)
        {
            bool accepted = SessionRules.CanAccept(connected, SessionRules.MaxPlayers, out string reason);

            Assert.That(accepted, Is.True);
            Assert.That(reason, Is.Empty);
        }

        [TestCase(4)]
        [TestCase(5)]
        public void CanAccept_AtOrAboveMax_RejectsWithReason(int connected)
        {
            bool accepted = SessionRules.CanAccept(connected, SessionRules.MaxPlayers, out string reason);

            Assert.That(accepted, Is.False);
            Assert.That(reason, Is.EqualTo(SessionRules.SessionFullReason));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryParseAddress_Empty_UsesDefaults(string input)
        {
            bool ok = SessionRules.TryParseAddress(input, out string address, out ushort port);

            Assert.That(ok, Is.True);
            Assert.That(address, Is.EqualTo(SessionRules.DefaultAddress));
            Assert.That(port, Is.EqualTo(SessionRules.DefaultPort));
        }

        [Test]
        public void TryParseAddress_AddressOnly_KeepsDefaultPort()
        {
            bool ok = SessionRules.TryParseAddress(" 192.168.1.20 ", out string address, out ushort port);

            Assert.That(ok, Is.True);
            Assert.That(address, Is.EqualTo("192.168.1.20"));
            Assert.That(port, Is.EqualTo(SessionRules.DefaultPort));
        }

        [Test]
        public void TryParseAddress_AddressAndPort_ParsesBoth()
        {
            bool ok = SessionRules.TryParseAddress("10.0.0.5:9000", out string address, out ushort port);

            Assert.That(ok, Is.True);
            Assert.That(address, Is.EqualTo("10.0.0.5"));
            Assert.That(port, Is.EqualTo(9000));
        }

        [TestCase("not an address")]
        [TestCase("10.0.0.5:abc")]
        [TestCase("10.0.0.5:0")]
        [TestCase("10.0.0.5:70000")]
        [TestCase("300.1.1.1")]
        public void TryParseAddress_Invalid_ReturnsFalse(string input)
        {
            Assert.That(SessionRules.TryParseAddress(input, out _, out _), Is.False);
        }
    }
}
