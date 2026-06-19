using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static byte[] WriteRoomListRequest()
        {
            return WriteGeneral(Command.RoomListRequest);
        }

        public static byte[] WriteRoomStateRequest()
        {
            return WriteGeneral(Command.RoomStateRequest);
        }

        public static byte[] WriteOnlinePlayersRequest()
        {
            return WriteGeneral(Command.OnlinePlayersRequest);
        }

        public static byte[] WriteRoomGetRequest(uint roomId)
        {
            var buffer = WritePacketHeader(Command.RoomGetRequest, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGetRequest);
            writer.WriteUInt32(roomId);
            return buffer;
        }

        public static byte[] WriteRoomCreate(string roomName, GameRoomType roomType, byte playersToStart)
        {
            var buffer = WritePacketHeader(Command.RoomCreate, ProtocolConstants.MaxRoomNameLength + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomCreate);
            writer.WriteFixedString(roomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)roomType);
            writer.WriteByte(playersToStart);
            return buffer;
        }

        public static byte[] WriteRoomJoin(uint roomId)
        {
            var buffer = WritePacketHeader(Command.RoomJoin, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomJoin);
            writer.WriteUInt32(roomId);
            return buffer;
        }

        public static byte[] WriteRoomLeave()
        {
            return WriteGeneral(Command.RoomLeave);
        }

        public static byte[] WriteRoomSetTrack(string trackName)
        {
            return WriteRoomSetTrack(TrackPackageRef.BuiltIn(trackName));
        }

        public static byte[] WriteRoomSetTrack(TrackPackageRef track)
        {
            var normalizedTrack = NormalizeTrackRef(track);
            var payload = 1
                + 2 + PacketWriter.MeasureString16(normalizedTrack.BuiltInTrackKey)
                + 2 + PacketWriter.MeasureString16(normalizedTrack.TrackId)
                + 2 + PacketWriter.MeasureString16(normalizedTrack.Version)
                + 2 + PacketWriter.MeasureString16(normalizedTrack.Hash);
            var buffer = WritePacketHeader(Command.RoomSetTrackV2, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetTrackV2);
            writer.WriteByte((byte)normalizedTrack.Kind);
            writer.WriteString16(normalizedTrack.BuiltInTrackKey);
            writer.WriteString16(normalizedTrack.TrackId);
            writer.WriteString16(normalizedTrack.Version);
            writer.WriteString16(normalizedTrack.Hash);
            return buffer;
        }

        public static byte[] WriteRoomSetLaps(int laps)
        {
            var buffer = WritePacketHeader(Command.RoomSetLaps, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetLaps);
            writer.WriteInt32(laps);
            return buffer;
        }

        public static byte[] WriteRoomStartRace()
        {
            return WriteGeneral(Command.RoomStartRace);
        }

        public static byte[] WriteRoomSetPlayersToStart(byte playersToStart)
        {
            var buffer = WritePacketHeader(Command.RoomSetPlayersToStart, 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetPlayersToStart);
            writer.WriteByte(playersToStart);
            return buffer;
        }

        public static byte[] WriteRoomSetGameRules(uint gameRulesFlags)
        {
            var buffer = WritePacketHeader(Command.RoomSetGameRules, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetGameRules);
            writer.WriteUInt32(gameRulesFlags);
            return buffer;
        }

        public static byte[] WriteRoomAddBot()
        {
            return WriteGeneral(Command.RoomAddBot);
        }

        public static byte[] WriteRoomRemoveBot()
        {
            return WriteGeneral(Command.RoomRemoveBot);
        }

        public static byte[] WriteRoomPlayerReady(CarType car, bool automaticTransmission)
        {
            var buffer = WritePacketHeader(Command.RoomPlayerReady, 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomPlayerReady);
            writer.WriteByte((byte)car);
            writer.WriteBool(automaticTransmission);
            return buffer;
        }

        public static byte[] WriteRoomPlayerWithdraw()
        {
            return WriteGeneral(Command.RoomPlayerWithdraw);
        }

        public static byte[] WriteRoomRaceControl(RoomRaceControlAction action)
        {
            var buffer = WritePacketHeader(Command.RoomRaceControl, 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceControl);
            writer.WriteByte((byte)action);
            return buffer;
        }

        private static TrackPackageRef NormalizeTrackRef(TrackPackageRef track)
        {
            return TrackPackageRef.Clone(track);
        }
    }
}

