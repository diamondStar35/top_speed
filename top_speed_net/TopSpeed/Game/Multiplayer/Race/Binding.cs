using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed class MultiplayerRaceBinding
    {
        public TrackData? PendingTrack;
        public string PendingTrackName = string.Empty;
        public int PendingLaps;
        public bool PendingStart;
        public uint ActiveRoomId;
        public uint RoomId;
        public uint RaceInstanceId;
        public uint EventSequence;
        public uint StateSequence;
        public int VehicleIndex;
        public bool AutomaticTransmission = true;
        // Effective game rules for the started race instance (room rules minus any host per-race
        // disable), delivered on the racing state change. Resolved into the local car at race start.
        public uint EffectiveGameRules;
        public bool HasEffectiveGameRules;

        public void ResetSession()
        {
            ActiveRoomId = 0;
            RoomId = 0;
            RaceInstanceId = 0;
            EventSequence = 0;
            StateSequence = 0;
        }

        public void ResetPending()
        {
            PendingTrack = null;
            PendingTrackName = string.Empty;
            PendingLaps = 0;
            PendingStart = false;
            VehicleIndex = 0;
            AutomaticTransmission = true;
            EffectiveGameRules = 0;
            HasEffectiveGameRules = false;
        }

        public void SetLoadout(int vehicleCount, int vehicleIndex, bool automaticTransmission)
        {
            VehicleIndex = Math.Max(0, Math.Min(vehicleCount - 1, vehicleIndex));
            AutomaticTransmission = automaticTransmission;
        }

        public void SetTrack(TrackData track, string trackName, int laps)
        {
            PendingTrack = track;
            PendingTrackName = string.IsNullOrWhiteSpace(trackName) ? "custom" : trackName;
            PendingLaps = laps;
        }

        public bool WaitForTrack()
        {
            if (PendingTrack != null)
                return false;

            PendingStart = true;
            return true;
        }

        public void ClearPendingStart()
        {
            PendingStart = false;
        }

        public void ApplyRoomState(PacketRoomState roomState)
        {
            if (IsStaleStateSequence(roomState.EventSequence))
                return;

            if (roomState.EventSequence != 0)
                StateSequence = roomState.EventSequence;
            ActiveRoomId = roomState.InRoom ? roomState.RoomId : 0;

            if (roomState.InRoom
                && roomState.RoomId != 0
                && roomState.RaceInstanceId != 0
                && (roomState.RaceState == RoomRaceState.Preparing || roomState.RaceState == RoomRaceState.Racing))
            {
                RoomId = roomState.RoomId;
                RaceInstanceId = roomState.RaceInstanceId;
            }
        }

        public bool ApplyRaceState(PacketRoomRaceStateChanged changed)
        {
            if (IsStaleEventSequence(changed.EventSequence))
                return false;

            if (changed.EventSequence != 0)
                EventSequence = changed.EventSequence;
            if (changed.State != RoomRaceState.Racing || changed.RoomId == 0 || changed.RaceInstanceId == 0)
                return true;

            RoomId = changed.RoomId;
            RaceInstanceId = changed.RaceInstanceId;
            EffectiveGameRules = changed.EffectiveGameRulesFlags;
            HasEffectiveGameRules = true;
            return true;
        }

        public void BindStartedRace()
        {
            if (RoomId != 0 && RoomId != ActiveRoomId)
                RaceInstanceId = 0;
            RoomId = ActiveRoomId;
        }

        public void ClearRaceBinding()
        {
            RoomId = 0;
            RaceInstanceId = 0;
            EventSequence = 0;
        }

        public bool AcceptRaceEvent(uint roomId, uint raceInstanceId, uint eventSequence, bool allowBindRaceInstance)
        {
            if (IsStaleEventSequence(eventSequence))
                return false;
            if (!MatchesContext(roomId, raceInstanceId, allowBindRaceInstance))
                return false;

            if (eventSequence != 0)
                EventSequence = eventSequence;
            return true;
        }

        public bool ShouldRequestResync(uint roomId, uint raceInstanceId, uint eventSequence)
        {
            if (IsStaleEventSequence(eventSequence))
                return false;
            if (RoomId != 0 && roomId != 0 && RoomId != roomId)
                return true;
            if (RaceInstanceId != 0 && raceInstanceId != 0 && RaceInstanceId != raceInstanceId)
                return true;

            return false;
        }

        public bool MatchesRoom(uint roomId)
        {
            if (roomId == 0)
                return true;
            if (RoomId == 0)
            {
                RoomId = roomId;
                return true;
            }

            return RoomId == roomId;
        }

        public bool MatchesContext(uint roomId, uint raceInstanceId, bool allowBindRaceInstance)
        {
            if (!MatchesRoom(roomId))
                return false;
            if (raceInstanceId == 0)
                return true;
            if (RaceInstanceId == 0)
            {
                if (!allowBindRaceInstance)
                    return false;
                RaceInstanceId = raceInstanceId;
                return true;
            }

            return RaceInstanceId == raceInstanceId;
        }

        private bool IsStaleEventSequence(uint eventSequence)
        {
            return PacketValidation.IsStaleSequence(EventSequence, eventSequence);
        }

        private bool IsStaleStateSequence(uint eventSequence)
        {
            return PacketValidation.IsStaleSequence(StateSequence, eventSequence);
        }
    }
}
