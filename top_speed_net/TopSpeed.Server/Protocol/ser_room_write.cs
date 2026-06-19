using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static byte[] WriteLoadCustomTrack(PacketLoadCustomTrack track)
        {
            var maxLength = Math.Min(track.TrackLength, (ushort)ProtocolConstants.MaxMultiTrackLength);
            var definitionCount = Math.Min(track.Definitions.Length, maxLength);
            var weatherProfiles = NormalizeWeatherProfiles(track.WeatherProfiles, track.DefaultWeatherProfileId);
            var profileCount = Math.Min(weatherProfiles.Count, byte.MaxValue);
            var payload = 4 + 12 + 1 + 2 + 2 + PacketWriter.MeasureString16(track.DefaultWeatherProfileId) + 1;
            for (var i = 0; i < profileCount; i++)
                payload += MeasureWeatherProfile(weatherProfiles[i]);
            for (var i = 0; i < definitionCount; i++)
                payload += MeasureDefinition(track.Definitions[i]);
            var buffer = WritePacketHeader(Command.LoadCustomTrack, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.LoadCustomTrack);
            writer.WriteInt32(track.NrOfLaps);
            writer.WriteFixedString(track.TrackName, 12);
            writer.WriteByte((byte)track.TrackAmbience);
            writer.WriteUInt16(maxLength);
            writer.WriteString16(track.DefaultWeatherProfileId ?? string.Empty);
            writer.WriteByte((byte)profileCount);
            for (var i = 0; i < profileCount; i++)
                WriteWeatherProfile(ref writer, weatherProfiles[i]);
            for (var i = 0; i < definitionCount; i++)
                WriteDefinition(ref writer, track.Definitions[i]);
            return buffer;
        }

        private static int MeasureWeatherProfile(TrackWeatherProfile profile)
        {
            return 2 + PacketWriter.MeasureString16(profile.Id) + 1 + (11 * 4);
        }

        private static int MeasureDefinition(TrackDefinition definition)
        {
            return 1 + 1 + 1 + 4 + 2 + PacketWriter.MeasureString16(definition.WeatherProfileId ?? string.Empty) + 4;
        }

        private static void WriteWeatherProfile(ref PacketWriter writer, TrackWeatherProfile profile)
        {
            writer.WriteString16(profile.Id);
            writer.WriteByte((byte)profile.Kind);
            writer.WriteSingle(profile.LongitudinalWindMps);
            writer.WriteSingle(profile.LateralWindMps);
            writer.WriteSingle(profile.AirDensityKgPerM3);
            writer.WriteSingle(profile.DraftingFactor);
            writer.WriteSingle(profile.TemperatureC);
            writer.WriteSingle(profile.Humidity);
            writer.WriteSingle(profile.PressureKpa);
            writer.WriteSingle(profile.VisibilityM);
            writer.WriteSingle(profile.RainGain);
            writer.WriteSingle(profile.WindGain);
            writer.WriteSingle(profile.StormGain);
        }

        private static void WriteDefinition(ref PacketWriter writer, TrackDefinition definition)
        {
            writer.WriteByte((byte)definition.Type);
            writer.WriteByte((byte)definition.Surface);
            writer.WriteByte((byte)definition.Noise);
            writer.WriteSingle(definition.Length);
            writer.WriteString16(definition.WeatherProfileId ?? string.Empty);
            writer.WriteSingle(definition.WeatherTransitionSeconds);
        }

        private static IReadOnlyList<TrackWeatherProfile> NormalizeWeatherProfiles(
            IReadOnlyDictionary<string, TrackWeatherProfile> weatherProfiles,
            string defaultWeatherProfileId)
        {
            var list = new List<TrackWeatherProfile>(weatherProfiles?.Count ?? 0);
            if (weatherProfiles != null)
            {
                foreach (var pair in weatherProfiles)
                    list.Add(pair.Value);
            }

            var defaultProfileId = string.IsNullOrWhiteSpace(defaultWeatherProfileId)
                ? TrackWeatherProfile.DefaultProfileId
                : defaultWeatherProfileId;
            if (list.FindIndex(profile => string.Equals(profile.Id, defaultProfileId, StringComparison.OrdinalIgnoreCase)) < 0)
                list.Insert(0, TrackWeatherProfile.CreatePreset(defaultProfileId, TrackWeather.Sunny));

            if (list.Count == 0)
            {
                list.Add(TrackWeatherProfile.CreatePreset(
                    defaultProfileId,
                    TrackWeather.Sunny));
            }

            return list;
        }

        public static byte[] WritePlayerJoined(PacketPlayerJoined joined)
        {
            var buffer = WritePacketHeader(Command.PlayerJoined, 4 + 1 + ProtocolConstants.MaxPlayerNameLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerJoined);
            writer.WriteUInt32(joined.PlayerId);
            writer.WriteByte(joined.PlayerNumber);
            writer.WriteFixedString(joined.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomList(PacketRoomList list)
        {
            var count = Math.Min(list.Rooms.Length, ProtocolConstants.MaxRoomListEntries);
            var payload = 1;
            for (var i = 0; i < count; i++)
            {
                var room = list.Rooms[i];
                payload += 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1;
                payload += MeasureTrackRef(NormalizeTrackRef(room.Track, room.TrackName));
            }
            var buffer = WritePacketHeader(Command.RoomList, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomList);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var room = list.Rooms[i];
                writer.WriteUInt32(room.RoomId);
                writer.WriteFixedString(room.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
                writer.WriteByte((byte)room.RoomType);
                writer.WriteByte(room.PlayerCount);
                writer.WriteByte(room.PlayersToStart);
                writer.WriteByte((byte)room.RaceState);
                WriteTrackRef(ref writer, NormalizeTrackRef(room.Track, room.TrackName));
            }
            return buffer;
        }

        public static byte[] WriteRoomState(PacketRoomState state)
        {
            var count = Math.Min(state.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 4 + 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 1 + 4 + 4 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength))
                + MeasureTrackRef(NormalizeTrackRef(state.Track, state.TrackName));
            var buffer = WritePacketHeader(Command.RoomState, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomState);
            writer.WriteUInt32(state.RoomVersion);
            writer.WriteUInt32(state.EventSequence);
            writer.WriteUInt32(state.RoomId);
            writer.WriteUInt32(state.RaceInstanceId);
            writer.WriteUInt32(state.HostPlayerId);
            writer.WriteFixedString(state.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)state.RoomType);
            writer.WriteByte(state.PlayersToStart);
            writer.WriteByte((byte)state.RaceState);
            writer.WriteBool(state.InRoom);
            writer.WriteBool(state.IsHost);
            writer.WriteBool(state.RacePaused);
            WriteTrackRef(ref writer, NormalizeTrackRef(state.Track, state.TrackName));
            writer.WriteInt32(state.Laps);
            writer.WriteUInt32(state.GameRulesFlags);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = state.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }
            return buffer;
        }

        public static byte[] WriteRoomGet(PacketRoomGet packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + 4 + 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 4 + 4 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength))
                + MeasureTrackRef(NormalizeTrackRef(packet.Track, packet.TrackName));
            var buffer = WritePacketHeader(Command.RoomGet, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGet);
            writer.WriteBool(packet.Found);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.EventSequence);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteUInt32(packet.HostPlayerId);
            writer.WriteFixedString(packet.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)packet.RoomType);
            writer.WriteByte(packet.PlayersToStart);
            writer.WriteByte((byte)packet.RaceState);
            writer.WriteBool(packet.RacePaused);
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

        public static byte[] WriteRoomRaceStateChanged(PacketRoomRaceStateChanged packet)
        {
            var payload = 4 + 4 + 4 + 4 + 1;
            var buffer = WritePacketHeader(Command.RoomRaceStateChanged, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceStateChanged);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.EventSequence);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteByte((byte)packet.State);
            return buffer;
        }

        public static byte[] WriteRoomRacePlayerFinished(PacketRoomRacePlayerFinished packet)
        {
            var payload = 4 + 4 + 4 + 4 + 4 + 1 + 1 + 4;
            var buffer = WritePacketHeader(Command.RoomRacePlayerFinished, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRacePlayerFinished);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.EventSequence);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteByte(packet.FinishOrder);
            writer.WriteInt32(packet.TimeMs);
            return buffer;
        }

        public static byte[] WriteRoomRaceCompleted(PacketRoomRaceCompleted packet)
        {
            var count = Math.Min(packet.Results.Length, ProtocolConstants.MaxPlayers);
            var payload = 4 + 4 + 4 + 4 + 1 + (count * (4 + 1 + 1 + 4 + 1));
            var buffer = WritePacketHeader(Command.RoomRaceCompleted, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceCompleted);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.EventSequence);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var result = packet.Results[i];
                writer.WriteUInt32(result.PlayerId);
                writer.WriteByte(result.PlayerNumber);
                writer.WriteByte(result.FinishOrder);
                writer.WriteInt32(result.TimeMs);
                writer.WriteByte((byte)result.Status);
            }
            return buffer;
        }

        public static byte[] WriteRoomRaceAborted(PacketRoomRaceAborted packet)
        {
            var payload = 4 + 4 + 4 + 4 + 1;
            var buffer = WritePacketHeader(Command.RoomRaceAborted, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceAborted);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.EventSequence);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteByte((byte)packet.Reason);
            return buffer;
        }

        public static byte[] WriteOnlinePlayers(PacketOnlinePlayers packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxRoomListEntries);
            var payload = 1 + (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength + ProtocolConstants.MaxRoomNameLength));
            var buffer = WritePacketHeader(Command.OnlinePlayers, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.OnlinePlayers);
            writer.WriteByte((byte)count);

            for (var i = 0; i < count; i++)
            {
                var player = packet.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.PresenceState);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
                writer.WriteFixedString(player.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            }

            return buffer;
        }

    }
}
