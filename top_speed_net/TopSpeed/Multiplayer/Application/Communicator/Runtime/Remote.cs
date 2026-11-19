using System;
using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        public void ApplyRemoteVoiceStart(PacketPlayerVoiceStart start, long receivedUtcTicks)
        {
            if (start == null || !PacketValidation.IsValidVoiceStart(start))
                return;

            if (_boundSession != null && start.PlayerId == _boundSession.PlayerId)
                return;

            if (_remoteStreams.TryGetValue(start.PlayerId, out var current))
            {
                current.Dispose();
                _remoteStreams.Remove(start.PlayerId);
            }

            var stream = new RemoteVoiceStream(_audio, _settings, start, MaxQueuedRemoteFrames)
            {
                LastReceivedUtcTicks = receivedUtcTicks
            };

            var localFrequencyTenths = _multiplayer.CommunicatorEnabled
                ? _multiplayer.CommunicatorFrequencyTenths
                : (ushort)0;
            var audible = IsAudibleForLocalFrequency(start.FrequencyTenths, localFrequencyTenths);
            stream.SetAudible(audible);
            _remoteStreams[start.PlayerId] = stream;
        }

        public void ApplyRemoteVoiceFrame(PacketPlayerVoiceFrame frame, long receivedUtcTicks)
        {
            if (frame == null || !PacketValidation.IsValidVoiceFrame(frame))
                return;

            if (!_remoteStreams.TryGetValue(frame.PlayerId, out var stream))
                return;
            if (stream.StreamId != frame.StreamId)
                return;

            stream.LastReceivedUtcTicks = receivedUtcTicks;
            if (!stream.IsAudible)
                return;

            stream.PushFrame(frame);
        }

        public void ApplyRemoteVoiceStop(PacketPlayerVoiceStop stop)
        {
            if (stop == null || !PacketValidation.IsValidVoiceStop(stop))
                return;

            if (!_remoteStreams.TryGetValue(stop.PlayerId, out var stream))
                return;
            if (stream.StreamId != stop.StreamId)
                return;

            var playCue = stream.PushToTalk && stream.IsAudible;
            stream.Dispose();
            _remoteStreams.Remove(stop.PlayerId);
            if (playCue)
                PlayRemotePttCue();
        }

        private void UpdateRemoteAudibility(ushort localFrequencyTenths)
        {
            foreach (var stream in _remoteStreams.Values)
            {
                stream.RefreshVolume(_settings);
                stream.SetAudible(IsAudibleForLocalFrequency(stream.FrequencyTenths, localFrequencyTenths));
            }
        }

        private void CleanupTimedOutRemoteStreams()
        {
            if (_remoteStreams.Count == 0)
                return;

            var now = DateTime.UtcNow.Ticks;
            var timeoutTicks = TimeSpan.FromMilliseconds(ProtocolConstants.VoiceTimeoutMs).Ticks;
            var expired = new List<uint>();
            foreach (var pair in _remoteStreams)
            {
                var stream = pair.Value;
                if (stream.LastReceivedUtcTicks <= 0)
                    continue;
                if (now - stream.LastReceivedUtcTicks <= timeoutTicks)
                    continue;
                if (stream.PushToTalk && stream.IsAudible)
                    PlayRemotePttCue();
                stream.Dispose();
                expired.Add(pair.Key);
            }

            for (var i = 0; i < expired.Count; i++)
                _remoteStreams.Remove(expired[i]);
        }

        private void ClearRemoteStreams()
        {
            foreach (var stream in _remoteStreams.Values)
                stream.Dispose();
            _remoteStreams.Clear();
        }

        private static bool IsAudibleForLocalFrequency(ushort sourceFrequencyTenths, ushort localFrequencyTenths)
        {
            return sourceFrequencyTenths != 0
                && localFrequencyTenths != 0
                && sourceFrequencyTenths == localFrequencyTenths;
        }
    }
}
