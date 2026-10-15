using NUnit.Framework;
using UnityEngine;

namespace LastProtocol.Tests
{
    public sealed class ConnectionPolicyTests
    {
        [TestCase(" abc123 ", "ABC123")]
        [TestCase(null, "")]
        public void NormalizeCode(string input, string expected) => Assert.AreEqual(expected, ConnectionPolicy.NormalizeJoinCode(input));

        [TestCase("ABC123", true)]
        [TestCase("abc123", true)]
        [TestCase("ABCDE", false)]
        [TestCase("ABCDEFG", false)]
        [TestCase("AB!123", false)]
        [TestCase("AB 123", false)]
        [TestCase("ÁBC123", false)]
        public void ValidateCode(string input, bool expected) => Assert.AreEqual(expected, ConnectionPolicy.IsValidJoinCode(input));

        [Test]
        public void LastAvailableSlotAcceptedFifthPlayerRejected()
        {
            Assert.AreEqual("", ConnectionPolicy.RejectionReason(true, MultiplayerConfig.MaxPlayers - 1, ConnectionPolicy.Payload));
            Assert.AreEqual("Room Full", ConnectionPolicy.RejectionReason(true, MultiplayerConfig.MaxPlayers, ConnectionPolicy.Payload));
        }
        [Test]
        public void ClosedRoomRejectsEvenWithSpace() => Assert.AreEqual("Room Closed", ConnectionPolicy.RejectionReason(false, 1, ConnectionPolicy.Payload));
        [Test]
        public void IncompatibleProtocolRejected() => Assert.AreEqual("Incompatible game version", ConnectionPolicy.RejectionReason(true, 1, new byte[] { 1, 2 }));
        [Test]
        public void RejectNonFiniteMovement()
        {
            Assert.IsFalse(ConnectionPolicy.IsFinite(new Vector2(float.NaN, 0)));
            Assert.IsFalse(ConnectionPolicy.IsFinite(new Vector2(0, float.PositiveInfinity)));
            Assert.IsTrue(ConnectionPolicy.IsFinite(Vector2.one));
        }
        [Test]
        public void ConfigRejectsImpossibleStartRequirement()
        {
            var config = ScriptableObject.CreateInstance<MultiplayerConfig>();
            try { config.minimumPlayersToStart = MultiplayerConfig.MaxPlayers + 1; Assert.IsFalse(config.IsValid); }
            finally { Object.DestroyImmediate(config); }
        }
    }
}
