using System;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using AudioSource = TS.Audio.Source;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void ApplyBumpCore(PacketPlayerBumped bump)
        {
            if (bump.PlayerNumber == LocalPlayerNumber && !_pitStop.IsGhosted)
                _car.Bump(bump.BumpX, bump.BumpY, bump.SpeedDeltaKph);
        }

        private void ApplyRemoteCrashCore(PacketPlayer crashed)
        {
            if (crashed.PlayerNumber == LocalPlayerNumber)
                return;
            if (crashed.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[crashed.PlayerNumber])
                return;
            if (_remotePlayers.TryGetValue(crashed.PlayerNumber, out var remote))
                remote.Player.Crash(remote.Player.PositionX, scheduleRestart: false);
        }

        private void ApplyRemoteFinishCore(byte playerNumber, byte finishOrder)
        {
            if (playerNumber == LocalPlayerNumber)
                return;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return;
            if (!_remotePlayers.TryGetValue(playerNumber, out var remote))
                return;
            if (remote.Finished)
                return;

            remote.Finished = true;
            remote.State = PlayerState.Finished;
            remote.Player.MarkFinished(GetSpatialTrackLength());
            if (finishOrder > 0)
            {
                var expectedIndex = Math.Max(0, finishOrder - 1);
                if (expectedIndex > _positionFinish)
                    _positionFinish = expectedIndex;
            }

            _progress.AnnounceRemoteFinish(playerNumber);
        }

        private void ApplyCompletedRemoteFinishes(PacketRoomRaceCompleted packet)
        {
            var results = packet?.Results ?? Array.Empty<PacketRoomRaceResultEntry>();
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result.Status != RoomRaceResultStatus.Finished)
                    continue;
                if (result.PlayerNumber == LocalPlayerNumber)
                    continue;
                if (result.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[result.PlayerNumber])
                    continue;
                if (!_remotePlayers.TryGetValue(result.PlayerNumber, out var remote))
                    continue;
                if (remote.Finished)
                    continue;

                var finishOrder = (byte)Math.Max(1, Math.Min(byte.MaxValue, i + 1));
                ApplyRemoteFinishCore(result.PlayerNumber, finishOrder);
            }
        }

        private void RemoveRemotePlayerCore(byte playerNumber, bool markDisconnected)
        {
            if (markDisconnected && playerNumber < _disconnectedPlayerSlots.Length)
                _disconnectedPlayerSlots[playerNumber] = true;

            _remoteMediaTransfers.Remove(playerNumber);
            _remoteLiveStates.Remove(playerNumber);
            if (_remotePlayers.TryGetValue(playerNumber, out var remote))
            {
                remote.Player.StopLiveStream();
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
                _remotePlayers.Remove(playerNumber);
            }

            RemovePlayerFromSnapshotFrames(playerNumber);
        }

        private void SyncParticipantsCore(PacketRoomState roomState)
        {
            if (roomState == null)
                return;

            _missingSnapshotPlayers.Clear();
            foreach (var number in _remotePlayers.Keys)
                _missingSnapshotPlayers.Add(number);

            var participants = roomState.Players ?? Array.Empty<PacketRoomPlayer>();
            for (var i = 0; i < participants.Length; i++)
            {
                var number = participants[i].PlayerNumber;
                if (number == LocalPlayerNumber)
                    continue;
                _missingSnapshotPlayers.Remove(number);
            }

            for (var i = 0; i < _missingSnapshotPlayers.Count; i++)
                RemoveRemotePlayerCore(_missingSnapshotPlayers[i], markDisconnected: false);
        }

        private bool HasPlayerInRace(int playerIndex)
        {
            if (playerIndex == LocalPlayerNumber)
                return true;
            if (playerIndex < 0 || playerIndex >= MaxPlayers)
                return false;
            return _remotePlayers.ContainsKey((byte)playerIndex);
        }

        private string GetPlayerNameForPlayer(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex < MaxPlayers)
            {
                var resolved = _resolvePlayerName((byte)playerIndex);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved.Trim();
            }

            return LocalizationService.Format(
                LocalizationService.Mark("Player {0}"),
                playerIndex + 1);
        }

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == LocalPlayerNumber)
            {
                if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                    return TopSpeed.Drive.Session.SessionText.FormatVehicleName(_car.CustomFile);
                return _car.VehicleName;
            }

            var targetNumber = (byte)playerIndex;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return VehicleCatalog.Vehicles[remote.Player.VehicleIndex].Name;

            return LocalizationService.Mark("Vehicle");
        }

        private int CalculatePlayerPerc(int player)
        {
            if (player == LocalPlayerNumber)
                return ClampPercent(_car.PositionY);

            var targetNumber = (byte)player;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
            {
                if (remote.Finished)
                    return 100;
                return ClampPercent(remote.Player.PositionY);
            }

            return 0;
        }

        private float CalculatePlayerPosition(int player)
        {
            if (player == LocalPlayerNumber)
                return _car.PositionY;

            var targetNumber = (byte)player;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return remote.Player.PositionY;

            return 0f;
        }

        private bool CalculatePlayerFinished(int player)
        {
            if (player == LocalPlayerNumber)
                return _finished;

            var targetNumber = (byte)player;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return remote.Finished;

            return false;
        }

        private int ClampPercent(float positionY)
        {
            var raceDistance = GetSpatialTrackLength();
            if (raceDistance <= 0f)
                return 0;

            var perc = (int)((positionY / raceDistance) * 100f);
            if (perc > 100)
                perc = 100;
            if (perc < 0)
                perc = 0;
            return perc;
        }

        private void AnnounceFinishOrder(int playerNumber, ref int positionFinish)
        {
            var playerSound = GetPlayerNumberInfoSoundByIndex(playerNumber);
            var finishSound = GetFinishedSoundByIndex(positionFinish);
            if (playerSound == null || finishSound == null)
                return;

            SpeakRaceInfoIfLoaded(playerSound, true);
            SpeakRaceInfoIfLoaded(finishSound, true);
            positionFinish++;
        }
    }
}
