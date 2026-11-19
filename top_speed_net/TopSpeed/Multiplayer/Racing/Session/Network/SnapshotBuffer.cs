using System;
using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void ApplyRaceSnapshotCore(PacketRaceSnapshot snapshot)
        {
            if (snapshot == null)
                return;
            if (_hasRaceSnapshotSequence && snapshot.Sequence <= _lastRaceSnapshotSequence)
                return;

            _lastRaceSnapshotSequence = snapshot.Sequence;
            _lastRaceSnapshotTick = snapshot.Tick;
            _hasRaceSnapshotSequence = true;
            EnqueueRaceSnapshot(snapshot);
            ApplyBufferedRaceSnapshots(0f);
        }

        private void EnqueueRaceSnapshot(PacketRaceSnapshot snapshot)
        {
            var frame = new SnapshotFrame
            {
                Tick = snapshot.Tick,
                Players = ClonePlayers(snapshot.Players)
            };

            if (_snapshotFrames.Count > 0)
            {
                var last = _snapshotFrames[_snapshotFrames.Count - 1];
                if (frame.Tick < last.Tick)
                    return;
                if (frame.Tick == last.Tick)
                    _snapshotFrames[_snapshotFrames.Count - 1] = frame;
                else
                    _snapshotFrames.Add(frame);
            }
            else
            {
                _snapshotFrames.Add(frame);
            }

            while (_snapshotFrames.Count > SnapshotBufferMax)
                _snapshotFrames.RemoveAt(0);

            if (!_hasSnapshotTickNow)
            {
                _snapshotTickNow = frame.Tick;
                _hasSnapshotTickNow = true;
            }
            else if (frame.Tick > _snapshotTickNow)
            {
                _snapshotTickNow = frame.Tick;
            }
        }

        private static PacketPlayerData[] ClonePlayers(PacketPlayerData[]? source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<PacketPlayerData>();

            var result = new PacketPlayerData[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i];
                if (item == null)
                    continue;

                result[i] = new PacketPlayerData
                {
                    PlayerId = item.PlayerId,
                    PlayerNumber = item.PlayerNumber,
                    Car = item.Car,
                    RaceData = item.RaceData,
                    State = item.State,
                    EngineRunning = item.EngineRunning,
                    Braking = item.Braking,
                    Horning = item.Horning,
                    Backfiring = item.Backfiring,
                    MediaLoaded = item.MediaLoaded,
                    MediaPlaying = item.MediaPlaying,
                    MediaId = item.MediaId,
                    RadioVolumePercent = item.RadioVolumePercent
                };
            }

            return result;
        }

        private void RemovePlayerFromSnapshotFrames(byte playerNumber)
        {
            if (_snapshotFrames.Count == 0)
                return;

            for (var i = 0; i < _snapshotFrames.Count; i++)
            {
                var frame = _snapshotFrames[i];
                var players = frame.Players ?? Array.Empty<PacketPlayerData>();
                if (players.Length == 0)
                    continue;

                var filtered = new List<PacketPlayerData>(players.Length);
                for (var j = 0; j < players.Length; j++)
                {
                    var data = players[j];
                    if (data == null || data.PlayerNumber == playerNumber)
                        continue;
                    filtered.Add(data);
                }

                frame.Players = filtered.ToArray();
            }
        }
    }
}
