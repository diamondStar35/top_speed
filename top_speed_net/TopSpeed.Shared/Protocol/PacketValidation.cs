using System;

namespace TopSpeed.Protocol
{
    public static partial class PacketValidation
    {
        public static bool IsValidPlayerId(uint playerId)
        {
            return playerId != 0;
        }

        public static bool IsValidPlayerNumber(byte playerNumber)
        {
            return playerNumber < ProtocolConstants.MaxPlayers;
        }

        public static bool IsValidRoomId(uint roomId)
        {
            return roomId != 0;
        }

        public static bool IsValidRaceInstance(uint raceInstanceId)
        {
            return raceInstanceId != 0;
        }

        public static bool IsValidPlayerState(PlayerState state)
        {
            return state >= PlayerState.Undefined && state <= PlayerState.Finished;
        }

        public static bool IsValidRoomRaceState(RoomRaceState state)
        {
            return state >= RoomRaceState.Lobby && state <= RoomRaceState.Aborted;
        }

        public static bool IsValidRoomEventKind(RoomEventKind kind)
        {
            return kind >= RoomEventKind.None && kind <= RoomEventKind.RaceResumed;
        }

        public static bool IsValidRoomRaceControlAction(RoomRaceControlAction action)
        {
            return action >= RoomRaceControlAction.CancelPrepare && action <= RoomRaceControlAction.Stop;
        }

        public static bool IsValidCarType(CarType car)
        {
            return car >= CarType.Vehicle1 && car <= CarType.CustomVehicle;
        }

        public static bool IsValidRoomCreate(PacketRoomCreate packet)
        {
            return packet != null
                && IsValidRoomType(packet.RoomType)
                && IsValidPlayersToStart(packet.RoomType, packet.PlayersToStart);
        }

        public static bool IsValidRoomJoin(PacketRoomJoin packet)
        {
            return packet != null && IsValidRoomId(packet.RoomId);
        }

        public static bool IsValidRoomGetRequest(PacketRoomGetRequest packet)
        {
            return packet != null && IsValidRoomId(packet.RoomId);
        }

        public static bool IsValidRoomSetTrack(PacketRoomSetTrack packet)
        {
            return packet != null && IsValidTrackPackageRef(packet.Track);
        }

        public static bool IsValidRoomSetLaps(PacketRoomSetLaps packet)
        {
            return packet != null && packet.Laps >= 1 && packet.Laps <= 16;
        }

        public static bool IsValidRoomSetPlayersToStart(PacketRoomSetPlayersToStart packet, GameRoomType roomType)
        {
            return packet != null && IsValidPlayersToStart(roomType, packet.PlayersToStart);
        }

        public static bool IsValidRoomPlayerReady(PacketRoomPlayerReady packet)
        {
            return packet != null
                && IsValidCarType(packet.Car)
                && IsValidVehiclePackageRef(packet.Vehicle);
        }

        public static bool IsValidRoomRaceControl(PacketRoomRaceControl packet)
        {
            return packet != null && IsValidRoomRaceControlAction(packet.Action);
        }

        public static bool IsValidProtocolMessage(PacketProtocolMessage packet)
        {
            return packet != null
                && packet.Code >= ProtocolMessageCode.Ok
                && packet.Code <= ProtocolMessageCode.RoomChat
                && !string.IsNullOrWhiteSpace(packet.Message)
                && packet.Message.Length <= ProtocolConstants.MaxProtocolMessageLength;
        }

        public static bool IsValidRoomEvent(PacketRoomEvent packet)
        {
            return packet != null
                && IsValidRoomId(packet.RoomId)
                && IsValidRoomEventKind(packet.Kind)
                && IsValidRoomRaceState(packet.RaceState)
                && IsValidRoomType(packet.RoomType)
                && IsValidPlayerNumber(packet.SubjectPlayerNumber);
        }

        public static bool IsValidRoomType(GameRoomType roomType)
        {
            return roomType >= GameRoomType.BotsRace && roomType <= GameRoomType.PlayersRace;
        }

        public static bool IsValidPlayersToStart(GameRoomType roomType, byte playersToStart)
        {
            if (!IsValidRoomType(roomType))
                return false;
            if (roomType == GameRoomType.OneOnOne)
                return playersToStart == 2;
            return playersToStart >= 2 && playersToStart <= ProtocolConstants.MaxRoomPlayersToStart;
        }

        public static bool IsFinitePosition(float x, float y)
        {
            return !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y);
        }

        public static bool IsValidMediaBegin(PacketPlayerMediaBegin begin)
        {
            return begin != null
                && IsValidPlayerId(begin.PlayerId)
                && IsValidPlayerNumber(begin.PlayerNumber)
                && begin.MediaId != 0
                && begin.TransferId != 0
                && begin.TotalBytes > 0
                && begin.TotalBytes <= ProtocolConstants.MaxMediaBytes;
        }

        public static bool IsValidCommunicatorMediaBegin(PacketPlayerMediaBegin begin)
        {
            return IsValidMediaBegin(begin)
                && IsValidVoiceFrequencyTenths(begin.FrequencyTenths);
        }

        public static bool IsValidMediaChunk(PacketPlayerMediaChunk chunk)
        {
            return chunk != null
                && IsValidPlayerId(chunk.PlayerId)
                && IsValidPlayerNumber(chunk.PlayerNumber)
                && chunk.MediaId != 0
                && chunk.TransferId != 0
                && chunk.Data != null
                && chunk.Data.Length > 0
                && chunk.Data.Length <= ProtocolConstants.MaxMediaChunkBytes;
        }

        public static bool IsValidMediaEnd(PacketPlayerMediaEnd end)
        {
            return end != null
                && IsValidPlayerId(end.PlayerId)
                && IsValidPlayerNumber(end.PlayerNumber)
                && end.MediaId != 0
                && end.TransferId != 0;
        }

        public static bool IsValidCommunicatorMediaState(PacketPlayerCommunicatorMediaState state)
        {
            return state != null
                && IsValidPlayerId(state.PlayerId)
                && IsValidPlayerNumber(state.PlayerNumber)
                && IsValidVoiceFrequencyTenths(state.FrequencyTenths)
                && (!state.MediaLoaded || state.MediaId != 0)
                && state.VolumePercent <= 100;
        }

        public static bool IsValidLiveStart(PacketPlayerLiveStart start)
        {
            return start != null
                && IsValidPlayerId(start.PlayerId)
                && IsValidPlayerNumber(start.PlayerNumber)
                && start.StreamId != 0
                && start.Codec == LiveCodec.Opus
                && start.SampleRate == ProtocolConstants.LiveSampleRate
                && start.FrameMs == ProtocolConstants.LiveFrameMs
                && start.Channels >= ProtocolConstants.LiveChannelsMin
                && start.Channels <= ProtocolConstants.LiveChannelsMax;
        }

        public static bool IsValidLiveFrame(PacketPlayerLiveFrame frame)
        {
            return frame != null
                && IsValidPlayerId(frame.PlayerId)
                && IsValidPlayerNumber(frame.PlayerNumber)
                && frame.StreamId != 0
                && frame.Data != null
                && frame.Data.Length > 0
                && frame.Data.Length <= ProtocolConstants.MaxLiveFrameBytes;
        }

        public static bool IsValidLiveStop(PacketPlayerLiveStop stop)
        {
            return stop != null
                && IsValidPlayerId(stop.PlayerId)
                && IsValidPlayerNumber(stop.PlayerNumber)
                && stop.StreamId != 0;
        }

        public static bool IsValidVoiceFrequencyTenths(int value)
        {
            return value >= ProtocolConstants.VoiceFrequencyTenthsMin
                   && value <= ProtocolConstants.VoiceFrequencyTenthsMax;
        }

        public static bool IsValidVoiceStart(PacketPlayerVoiceStart start)
        {
            return start != null
                && IsValidPlayerId(start.PlayerId)
                && IsValidPlayerNumber(start.PlayerNumber)
                && start.StreamId != 0
                && start.Codec == LiveCodec.Opus
                && start.SampleRate == ProtocolConstants.VoiceSampleRate
                && start.FrameMs == ProtocolConstants.VoiceFrameMs
                && start.Channels >= ProtocolConstants.VoiceChannelsMin
                && start.Channels <= ProtocolConstants.VoiceChannelsMax
                && IsValidVoiceFrequencyTenths(start.FrequencyTenths);
        }

        public static bool IsValidVoiceFrame(PacketPlayerVoiceFrame frame)
        {
            return frame != null
                && IsValidPlayerId(frame.PlayerId)
                && IsValidPlayerNumber(frame.PlayerNumber)
                && frame.StreamId != 0
                && frame.Data != null
                && frame.Data.Length > 0
                && frame.Data.Length <= ProtocolConstants.MaxVoiceFrameBytes;
        }

        public static bool IsValidVoiceStop(PacketPlayerVoiceStop stop)
        {
            return stop != null
                && IsValidPlayerId(stop.PlayerId)
                && IsValidPlayerNumber(stop.PlayerNumber)
                && stop.StreamId != 0;
        }

        public static bool IsStaleSequence(uint currentSequence, uint incomingSequence)
        {
            return currentSequence != 0 && incomingSequence != 0 && currentSequence >= incomingSequence;
        }
    }
}
