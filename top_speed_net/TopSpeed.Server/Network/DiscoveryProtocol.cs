using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal static class DiscoveryProtocol
    {
        private static readonly byte[] RequestMagic =
        {
            (byte)'T', (byte)'S', (byte)'D', (byte)'I', (byte)'S', (byte)'C', (byte)'O', (byte)'V', (byte)'E', (byte)'R', (byte)'Y'
        };

        private static readonly byte[] ResponseMagic =
        {
            (byte)'T', (byte)'S', (byte)'S', (byte)'E', (byte)'R', (byte)'V', (byte)'E', (byte)'R'
        };

        public const int MaxNameLength = 32;
        public const int MaxTrackLength = 32;

        public static bool TryParseRequest(ReadOnlySpan<byte> data, out byte version)
        {
            version = 0;
            if (data.Length < RequestMagic.Length + 1)
                return false;
            if (!data.Slice(0, RequestMagic.Length).SequenceEqual(RequestMagic))
                return false;
            version = data[RequestMagic.Length];
            return true;
        }

        public static byte[] BuildResponse(ServerSnapshot snapshot)
        {
            var buffer = new byte[ResponseMagic.Length + 1 + 2 + 1 + 1 + 1 + 1 + MaxNameLength + MaxTrackLength];
            var writer = new PacketWriter(buffer);
            foreach (var b in ResponseMagic)
                writer.WriteByte(b);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteUInt16((ushort)snapshot.Port);
            writer.WriteByte((byte)Math.Min(snapshot.PlayerCount, byte.MaxValue));
            writer.WriteByte((byte)Math.Min(snapshot.MaxPlayers, byte.MaxValue));
            writer.WriteBool(snapshot.RaceStarted);
            writer.WriteBool(snapshot.TrackSelected);
            writer.WriteFixedString(snapshot.Name ?? string.Empty, MaxNameLength);
            writer.WriteFixedString(snapshot.TrackName ?? string.Empty, MaxTrackLength);
            return buffer;
        }
    }
}
