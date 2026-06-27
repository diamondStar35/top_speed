using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadPlayerJoined(byte[] data, out PacketPlayerJoined packet)
        {
            packet = new PacketPlayerJoined();
            if (data.Length < 2 + 4 + 1 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.PlayerJoined)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return true;
        }

        public static bool TryReadRoomList(byte[] data, out PacketRoomList packet)
        {
            packet = new PacketRoomList();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomList)
                return false;
            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var count = reader.ReadByte();
                var rooms = new List<PacketRoomSummary>(count);
                for (var i = 0; i < count; i++)
                {
                    var roomId = reader.ReadUInt32();
                    var roomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
                    var roomType = (GameRoomType)reader.ReadByte();
                    var playerCount = reader.ReadByte();
                    var playersToStart = reader.ReadByte();
                    var raceState = (RoomRaceState)reader.ReadByte();
                    var track = ReadTrackRef(ref reader);
                    rooms.Add(new PacketRoomSummary
                    {
                        RoomId = roomId,
                        RoomName = roomName,
                        RoomType = roomType,
                        PlayerCount = playerCount,
                        PlayersToStart = playersToStart,
                        RaceState = raceState,
                        Track = track,
                        TrackName = track.IsBuiltIn ? track.BuiltInTrackKey : track.TrackId
                    });
                }

                packet.Rooms = rooms.ToArray();
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                packet = new PacketRoomList();
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                packet = new PacketRoomList();
                return false;
            }
        }

        public static bool TryReadRoomState(byte[] data, out PacketRoomState packet)
        {
            packet = new PacketRoomState();
            if (data.Length < 2 + 4 + 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 4 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomState)
                return false;
            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.RoomVersion = reader.ReadUInt32();
                packet.EventSequence = reader.ReadUInt32();
                packet.RoomId = reader.ReadUInt32();
                packet.RaceInstanceId = reader.ReadUInt32();
                packet.HostPlayerId = reader.ReadUInt32();
                packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
                packet.RoomType = (GameRoomType)reader.ReadByte();
                packet.PlayersToStart = reader.ReadByte();
                packet.RaceState = (RoomRaceState)reader.ReadByte();
                packet.InRoom = reader.ReadBool();
                packet.IsHost = reader.ReadBool();
                packet.RacePaused = reader.ReadBool();
                packet.Track = ReadTrackRef(ref reader);
                packet.TrackName = packet.Track.IsBuiltIn ? packet.Track.BuiltInTrackKey : packet.Track.TrackId;
                packet.Laps = reader.ReadByte();
                packet.GameRulesFlags = reader.ReadUInt32();
                var count = reader.ReadByte();
                var players = new PacketRoomPlayer[count];
                for (var i = 0; i < count; i++)
                {
                    players[i] = new PacketRoomPlayer
                    {
                        PlayerId = reader.ReadUInt32(),
                        PlayerNumber = reader.ReadByte(),
                        State = (PlayerState)reader.ReadByte(),
                        Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength)
                    };
                }

                packet.Players = players;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                packet = new PacketRoomState();
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                packet = new PacketRoomState();
                return false;
            }
        }

        public static bool TryReadRoomEvent(byte[] data, out PacketRoomEvent packet)
        {
            packet = new PacketRoomEvent();
            if (data.Length < 2 + 4 + 4 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomEvent)
                return false;
            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.RoomId = reader.ReadUInt32();
                packet.RoomVersion = reader.ReadUInt32();
                packet.EventSequence = reader.ReadUInt32();
                packet.RaceInstanceId = reader.ReadUInt32();
                packet.Kind = (RoomEventKind)reader.ReadByte();
                packet.HostPlayerId = reader.ReadUInt32();
                packet.RoomType = (GameRoomType)reader.ReadByte();
                packet.PlayerCount = reader.ReadByte();
                packet.PlayersToStart = reader.ReadByte();
                packet.RaceState = (RoomRaceState)reader.ReadByte();
                packet.RacePaused = reader.ReadBool();
                packet.Track = ReadTrackRef(ref reader);
                packet.TrackName = packet.Track.IsBuiltIn ? packet.Track.BuiltInTrackKey : packet.Track.TrackId;
                packet.Laps = reader.ReadByte();
                packet.GameRulesFlags = reader.ReadUInt32();
                packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
                packet.SubjectPlayerId = reader.ReadUInt32();
                packet.SubjectPlayerNumber = reader.ReadByte();
                packet.SubjectPlayerState = (PlayerState)reader.ReadByte();
                packet.SubjectPlayerName = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                packet = new PacketRoomEvent();
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                packet = new PacketRoomEvent();
                return false;
            }
        }

        public static bool TryReadRoomGet(byte[] data, out PacketRoomGet packet)
        {
            packet = new PacketRoomGet();
            if (data.Length < 2 + 1 + 4 + 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 4 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomGet)
                return false;
            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Found = reader.ReadBool();
                packet.RoomVersion = reader.ReadUInt32();
                packet.EventSequence = reader.ReadUInt32();
                packet.RoomId = reader.ReadUInt32();
                packet.RaceInstanceId = reader.ReadUInt32();
                packet.HostPlayerId = reader.ReadUInt32();
                packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
                packet.RoomType = (GameRoomType)reader.ReadByte();
                packet.PlayersToStart = reader.ReadByte();
                packet.RaceState = (RoomRaceState)reader.ReadByte();
                packet.RacePaused = reader.ReadBool();
                packet.Track = ReadTrackRef(ref reader);
                packet.TrackName = packet.Track.IsBuiltIn ? packet.Track.BuiltInTrackKey : packet.Track.TrackId;
                packet.Laps = reader.ReadByte();
                packet.GameRulesFlags = reader.ReadUInt32();
                var count = reader.ReadByte();
                var players = new PacketRoomPlayer[count];
                for (var i = 0; i < count; i++)
                {
                    players[i] = new PacketRoomPlayer
                    {
                        PlayerId = reader.ReadUInt32(),
                        PlayerNumber = reader.ReadByte(),
                        State = (PlayerState)reader.ReadByte(),
                        Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength)
                    };
                }

                packet.Players = players;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                packet = new PacketRoomGet();
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                packet = new PacketRoomGet();
                return false;
            }
        }

        public static bool TryReadRoomRaceStateChanged(byte[] data, out PacketRoomRaceStateChanged packet)
        {
            packet = new PacketRoomRaceStateChanged();
            if (data.Length < 2 + 4 + 4 + 4 + 4 + 1 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomRaceStateChanged)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.RoomVersion = reader.ReadUInt32();
            packet.EventSequence = reader.ReadUInt32();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.State = (RoomRaceState)reader.ReadByte();
            packet.EffectiveGameRulesFlags = reader.ReadUInt32();
            return true;
        }

        public static bool TryReadRoomRacePlayerFinished(byte[] data, out PacketRoomRacePlayerFinished packet)
        {
            packet = new PacketRoomRacePlayerFinished();
            if (data.Length < 2 + 4 + 4 + 4 + 4 + 4 + 1 + 1 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomRacePlayerFinished)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.RoomVersion = reader.ReadUInt32();
            packet.EventSequence = reader.ReadUInt32();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.FinishOrder = reader.ReadByte();
            packet.TimeMs = reader.ReadInt32();
            return true;
        }

        public static bool TryReadRoomRaceCompleted(byte[] data, out PacketRoomRaceCompleted packet)
        {
            packet = new PacketRoomRaceCompleted();
            const int headerSize = 2 + 4 + 4 + 4 + 4 + 1;
            if (data.Length < headerSize)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomRaceCompleted)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.RoomVersion = reader.ReadUInt32();
            packet.EventSequence = reader.ReadUInt32();
            packet.RaceInstanceId = reader.ReadUInt32();
            var count = reader.ReadByte();
            var stride = 4 + 1 + 1 + 4 + 1;
            var available = Math.Max(0, (data.Length - headerSize) / stride);
            var actualCount = Math.Min(count, available);
            var results = new PacketRoomRaceResultEntry[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                results[i] = new PacketRoomRaceResultEntry
                {
                    PlayerId = reader.ReadUInt32(),
                    PlayerNumber = reader.ReadByte(),
                    FinishOrder = reader.ReadByte(),
                    TimeMs = reader.ReadInt32(),
                    Status = (RoomRaceResultStatus)reader.ReadByte()
                };
            }

            packet.Results = results;
            return true;
        }

        public static bool TryReadRoomRaceAborted(byte[] data, out PacketRoomRaceAborted packet)
        {
            packet = new PacketRoomRaceAborted();
            if (data.Length < 2 + 4 + 4 + 4 + 4 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomRaceAborted)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.RoomVersion = reader.ReadUInt32();
            packet.EventSequence = reader.ReadUInt32();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.Reason = (RoomRaceAbortReason)reader.ReadByte();
            return true;
        }

        public static bool TryReadRoomRaceControl(byte[] data, out PacketRoomRaceControl packet)
        {
            packet = new PacketRoomRaceControl();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomRaceControl)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Action = (RoomRaceControlAction)reader.ReadByte();
            return true;
        }

        public static bool TryReadOnlinePlayers(byte[] data, out PacketOnlinePlayers packet)
        {
            packet = new PacketOnlinePlayers();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.OnlinePlayers)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            var count = reader.ReadByte();
            var stride = 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength + ProtocolConstants.MaxRoomNameLength;
            var available = Math.Max(0, (data.Length - 3) / stride);
            var actualCount = Math.Min(count, available);
            var players = new PacketOnlinePlayer[actualCount];

            for (var i = 0; i < actualCount; i++)
            {
                players[i] = new PacketOnlinePlayer
                {
                    PlayerId = reader.ReadUInt32(),
                    PlayerNumber = reader.ReadByte(),
                    PresenceState = (OnlinePresenceState)reader.ReadByte(),
                    Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength),
                    RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength)
                };
            }

            packet.Players = players;
            return true;
        }

        private static TrackPackageRef ReadTrackRef(ref PacketReader reader)
        {
            return new TrackPackageRef
            {
                Kind = (RoomTrackSelectionKind)reader.ReadByte(),
                BuiltInTrackKey = reader.ReadString16(),
                TrackId = reader.ReadString16(),
                Version = reader.ReadString16(),
                Hash = TrackPackageRef.NormalizeHash(reader.ReadString16())
            };
        }
    }
}

