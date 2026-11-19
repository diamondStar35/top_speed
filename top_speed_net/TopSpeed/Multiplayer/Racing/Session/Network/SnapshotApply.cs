using System;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void ApplyBufferedRaceSnapshots(float elapsed)
        {
            if (_snapshotFrames.Count == 0)
                return;

            if (!_hasSnapshotTickNow)
            {
                _snapshotTickNow = _snapshotFrames[_snapshotFrames.Count - 1].Tick;
                _hasSnapshotTickNow = true;
            }

            if (elapsed > 0f)
                _snapshotTickNow += elapsed * ServerTickRate;

            var latestTick = (float)_snapshotFrames[_snapshotFrames.Count - 1].Tick;
            var maxTickNow = latestTick + SnapshotDelayTicks;
            if (_snapshotTickNow > maxTickNow)
                _snapshotTickNow = maxTickNow;

            var renderTick = _snapshotTickNow - SnapshotDelayTicks;
            if (renderTick < 0f)
                renderTick = 0f;

            while (_snapshotFrames.Count > 2 && renderTick >= _snapshotFrames[1].Tick)
                _snapshotFrames.RemoveAt(0);

            if (_snapshotFrames.Count == 1)
            {
                ApplySnapshotFrame(_snapshotFrames[0]);
                return;
            }

            var from = _snapshotFrames[0];
            var to = _snapshotFrames[1];
            if (renderTick <= from.Tick)
            {
                ApplySnapshotFrame(from);
                return;
            }

            if (renderTick >= to.Tick)
            {
                ApplySnapshotFrame(to);
                return;
            }

            var span = (float)(to.Tick - from.Tick);
            if (span <= 0f)
            {
                ApplySnapshotFrame(to);
                return;
            }

            var alpha = (renderTick - from.Tick) / span;
            if (alpha < 0f)
                alpha = 0f;
            else if (alpha > 1f)
                alpha = 1f;

            ApplyInterpolatedSnapshotFrame(from, to, alpha);
        }

        private void ApplySnapshotFrame(SnapshotFrame frame)
        {
            var players = frame.Players ?? Array.Empty<PacketPlayerData>();
            _missingSnapshotPlayers.Clear();
            foreach (var key in _remotePlayers.Keys)
                _missingSnapshotPlayers.Add(key);

            for (var i = 0; i < players.Length; i++)
            {
                var data = players[i];
                if (data == null)
                    continue;
                _missingSnapshotPlayers.Remove(data.PlayerNumber);
                ApplyRemoteData(data);
            }

            RemoveMissingSnapshotPlayers();
        }

        private void ApplyInterpolatedSnapshotFrame(SnapshotFrame from, SnapshotFrame to, float alpha)
        {
            var players = to.Players ?? Array.Empty<PacketPlayerData>();
            _missingSnapshotPlayers.Clear();
            foreach (var key in _remotePlayers.Keys)
                _missingSnapshotPlayers.Add(key);

            for (var i = 0; i < players.Length; i++)
            {
                var target = players[i];
                if (target == null)
                    continue;

                _missingSnapshotPlayers.Remove(target.PlayerNumber);

                if (!TryGetPlayerFrameData(from, target.PlayerNumber, out var source) || source == null)
                {
                    ApplyRemoteData(target);
                    continue;
                }

                var posX = Lerp(source.RaceData.PositionX, target.RaceData.PositionX, alpha);
                var posY = Lerp(source.RaceData.PositionY, target.RaceData.PositionY, alpha);
                var speed = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, (int)Math.Round(Lerp(source.RaceData.Speed, target.RaceData.Speed, alpha))));
                var freq = (int)Math.Round(Lerp(source.RaceData.Frequency, target.RaceData.Frequency, alpha));

                ApplyRemoteDataCore(
                    target.PlayerNumber,
                    target.Car,
                    target.State,
                    posX,
                    posY,
                    speed,
                    freq,
                    target.EngineRunning,
                    target.Braking,
                    target.Horning,
                    target.Backfiring,
                    target.MediaLoaded,
                    target.MediaPlaying,
                    target.MediaId,
                    target.RadioVolumePercent);
            }

            RemoveMissingSnapshotPlayers();
        }

        private void RemoveMissingSnapshotPlayers()
        {
            if (_missingSnapshotPlayers.Count == 0)
                return;

            for (var i = 0; i < _missingSnapshotPlayers.Count; i++)
            {
                var number = _missingSnapshotPlayers[i];
                if (number == LocalPlayerNumber)
                    continue;
                RemoveRemotePlayerCore(number, markDisconnected: false);
            }
        }

        private static bool TryGetPlayerFrameData(SnapshotFrame frame, byte playerNumber, out PacketPlayerData? data)
        {
            var players = frame.Players ?? Array.Empty<PacketPlayerData>();
            for (var i = 0; i < players.Length; i++)
            {
                var item = players[i];
                if (item == null || item.PlayerNumber != playerNumber)
                    continue;
                data = item;
                return true;
            }

            data = null;
            return false;
        }

        private static float Lerp(float a, float b, float alpha)
        {
            return a + ((b - a) * alpha);
        }
    }
}
