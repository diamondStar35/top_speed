using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static byte[] WriteRoomEvent(PacketRoomEvent evt)
        {
            var payload = 4 + 4 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 4 + 4 +
                ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength
                + MeasureTrackRef(NormalizeTrackRef(evt.Track, evt.TrackName));
            var buffer = WritePacketHeader(Command.RoomEvent, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomEvent);
            writer.WriteUInt32(evt.RoomId);
            writer.WriteUInt32(evt.RoomVersion);
            writer.WriteUInt32(evt.EventSequence);
            writer.WriteUInt32(evt.RaceInstanceId);
            writer.WriteByte((byte)evt.Kind);
            writer.WriteUInt32(evt.HostPlayerId);
            writer.WriteByte((byte)evt.RoomType);
            writer.WriteByte(evt.PlayerCount);
            writer.WriteByte(evt.PlayersToStart);
            writer.WriteByte((byte)evt.RaceState);
            writer.WriteBool(evt.RacePaused);
            WriteTrackRef(ref writer, NormalizeTrackRef(evt.Track, evt.TrackName));
            writer.WriteInt32(evt.Laps);
            writer.WriteUInt32(evt.GameRulesFlags);
            writer.WriteFixedString(evt.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteUInt32(evt.SubjectPlayerId);
            writer.WriteByte(evt.SubjectPlayerNumber);
            writer.WriteByte((byte)evt.SubjectPlayerState);
            writer.WriteFixedString(evt.SubjectPlayerName ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomGet(PacketRoomGet packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 4 + 4 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength))
                + MeasureTrackRef(NormalizeTrackRef(packet.Track, packet.TrackName));
            var buffer = WritePacketHeader(Command.RoomGet, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGet);
            writer.WriteBool(packet.Found);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteUInt32(packet.HostPlayerId);
            writer.WriteFixedString(packet.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)packet.RoomType);
            writer.WriteByte(packet.PlayersToStart);
            writer.WriteByte((byte)packet.RaceState);
            WriteTrackRef(ref writer, NormalizeTrackRef(packet.Track, packet.TrackName));
            writer.WriteInt32(packet.Laps);
            writer.WriteUInt32(packet.GameRulesFlags);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = packet.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }

            return buffer;
        }

        private static TrackPackageRef NormalizeTrackRef(TrackPackageRef track, string legacyTrackName)
        {
            var normalized = TrackPackageRef.Clone(track);
            if (normalized.IsCustomPackage)
                return normalized;

            var builtIn = normalized.BuiltInTrackKey;
            if (string.IsNullOrWhiteSpace(builtIn))
                builtIn = legacyTrackName ?? string.Empty;
            return TrackPackageRef.BuiltIn(builtIn);
        }

        private static int MeasureTrackRef(TrackPackageRef track)
        {
            return 1
                + 2 + PacketWriter.MeasureString16(track.BuiltInTrackKey)
                + 2 + PacketWriter.MeasureString16(track.TrackId)
                + 2 + PacketWriter.MeasureString16(track.Version)
                + 2 + PacketWriter.MeasureString16(track.Hash);
        }

        private static void WriteTrackRef(ref PacketWriter writer, TrackPackageRef track)
        {
            writer.WriteByte((byte)track.Kind);
            writer.WriteString16(track.BuiltInTrackKey ?? string.Empty);
            writer.WriteString16(track.TrackId ?? string.Empty);
            writer.WriteString16(track.Version ?? string.Empty);
            writer.WriteString16(track.Hash ?? string.Empty);
        }
    }
}

