using System;
using System.Text;
using UnityEngine;

namespace LastProtocol
{
    public static class ConnectionPolicy
    {
        // Approval payload identifies the wire protocol; it is NOT an authentication token.
        public static byte[] Payload => Encoding.UTF8.GetBytes("LAST_PROTOCOL:" + MultiplayerConfig.ProtocolVersion);

        public static string NormalizeJoinCode(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();

        public static bool IsValidJoinCode(string value)
        {
            string code = NormalizeJoinCode(value);
            if (code.Length != 6) return false;
            foreach (char c in code)
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
            return true;
        }

        public static string RejectionReason(bool open, int reservedPlayers, byte[] payload)
        {
            if (!open) return "Room Closed";
            if (reservedPlayers >= MultiplayerConfig.MaxPlayers) return "Room Full";
            if (payload == null || payload.Length != Payload.Length
                || Encoding.UTF8.GetString(payload) != Encoding.UTF8.GetString(Payload))
                return "Incompatible game version";
            return string.Empty;
        }

        public static bool IsFinite(Vector2 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
    }
}
