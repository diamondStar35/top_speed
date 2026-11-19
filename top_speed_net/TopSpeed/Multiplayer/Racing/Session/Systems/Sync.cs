using System;
using System.Collections.Generic;
using TopSpeed.Network.Live;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer.Session.Systems
{
    internal sealed class Sync : TopSpeed.Drive.Session.Subsystem
    {
        private readonly Tx _liveTx;
        private readonly IDictionary<byte, LiveState> _remoteLiveStates;
        private readonly IDictionary<byte, RemotePlayer> _remotePlayers;
        private readonly List<byte> _expiredLivePlayers;
        private readonly Func<bool> _isHostPaused;
        private readonly Func<bool> _isServerStopReceived;
        private readonly Func<bool> _isSendFailureAnnounced;
        private readonly Action<bool> _setSendFailureAnnounced;
        private readonly Func<bool> _isLiveFailureAnnounced;
        private readonly Action<bool> _setLiveFailureAnnounced;
        private readonly Func<float> _getLocalPositionX;
        private readonly Func<float> _getLocalPositionY;
        private readonly Action<float> _applySnapshots;
        private readonly Func<bool> _sendPlayerData;
        private readonly Action<string> _speakText;
        private float _sendAccumulator;

        public Sync(
            string name,
            int order,
            Tx liveTx,
            IDictionary<byte, LiveState> remoteLiveStates,
            IDictionary<byte, RemotePlayer> remotePlayers,
            List<byte> expiredLivePlayers,
            Func<bool> isHostPaused,
            Func<bool> isServerStopReceived,
            Func<bool> isSendFailureAnnounced,
            Action<bool> setSendFailureAnnounced,
            Func<bool> isLiveFailureAnnounced,
            Action<bool> setLiveFailureAnnounced,
            Func<float> getLocalPositionX,
            Func<float> getLocalPositionY,
            Action<float> applySnapshots,
            Func<bool> sendPlayerData,
            Action<string> speakText)
            : base(name, order)
        {
            _liveTx = liveTx ?? throw new ArgumentNullException(nameof(liveTx));
            _remoteLiveStates = remoteLiveStates ?? throw new ArgumentNullException(nameof(remoteLiveStates));
            _remotePlayers = remotePlayers ?? throw new ArgumentNullException(nameof(remotePlayers));
            _expiredLivePlayers = expiredLivePlayers ?? throw new ArgumentNullException(nameof(expiredLivePlayers));
            _isHostPaused = isHostPaused ?? throw new ArgumentNullException(nameof(isHostPaused));
            _isServerStopReceived = isServerStopReceived ?? throw new ArgumentNullException(nameof(isServerStopReceived));
            _isSendFailureAnnounced = isSendFailureAnnounced ?? throw new ArgumentNullException(nameof(isSendFailureAnnounced));
            _setSendFailureAnnounced = setSendFailureAnnounced ?? throw new ArgumentNullException(nameof(setSendFailureAnnounced));
            _isLiveFailureAnnounced = isLiveFailureAnnounced ?? throw new ArgumentNullException(nameof(isLiveFailureAnnounced));
            _setLiveFailureAnnounced = setLiveFailureAnnounced ?? throw new ArgumentNullException(nameof(setLiveFailureAnnounced));
            _getLocalPositionX = getLocalPositionX ?? throw new ArgumentNullException(nameof(getLocalPositionX));
            _getLocalPositionY = getLocalPositionY ?? throw new ArgumentNullException(nameof(getLocalPositionY));
            _applySnapshots = applySnapshots ?? throw new ArgumentNullException(nameof(applySnapshots));
            _sendPlayerData = sendPlayerData ?? throw new ArgumentNullException(nameof(sendPlayerData));
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            _applySnapshots(_isHostPaused() ? 0f : elapsed);

            if (_isServerStopReceived())
            {
                foreach (var remote in _remotePlayers.Values)
                {
                    if (!remote.Finished && remote.State != PlayerState.Finished)
                        remote.Player.Stop();
                    remote.Player.Run(elapsed, _getLocalPositionX(), _getLocalPositionY());
                }
            }

            DrainRemoteLiveFrames();

            if (!_liveTx.Update(_isHostPaused() ? 0f : elapsed, out var liveError) && !_isLiveFailureAnnounced())
            {
                _setLiveFailureAnnounced(true);
                _speakText(liveError);
            }

            _sendAccumulator += elapsed;
            if (_sendAccumulator < 1f / 60f)
                return;

            _sendAccumulator = 0f;
            if (!_sendPlayerData() && !_isSendFailureAnnounced())
            {
                _setSendFailureAnnounced(true);
                _speakText(TopSpeed.Localization.LocalizationService.Mark("Network send failed. Please check your connection."));
            }
        }

        public void Reset()
        {
            _sendAccumulator = 0f;
        }

        private void DrainRemoteLiveFrames()
        {
            if (_remoteLiveStates.Count == 0)
                return;

            var nowTicks = DateTime.UtcNow.Ticks;
            var timeoutTicks = TimeSpan.FromMilliseconds(ProtocolConstants.LiveTimeoutMs).Ticks;
            _expiredLivePlayers.Clear();

            foreach (var pair in _remoteLiveStates)
            {
                if (!_remotePlayers.TryGetValue(pair.Key, out var remote))
                    continue;

                var live = pair.Value;
                if (nowTicks - live.LastReceivedUtcTicks > timeoutTicks)
                {
                    remote.Player.ApplyLiveStop(live.StreamId);
                    _expiredLivePlayers.Add(pair.Key);
                    continue;
                }

                remote.Player.ApplyLiveStart(live.StreamId, live.Codec, live.SampleRate, live.Channels, live.FrameMs);
                while (live.Frames.Count > 0)
                {
                    var frame = live.Frames.Dequeue();
                    if (remote.Player.ApplyLiveFrame(live.StreamId, frame.Payload, frame.Timestamp))
                        live.MarkForwarded();
                    else
                        live.MarkDecodeDropped();
                }
            }

            for (var i = 0; i < _expiredLivePlayers.Count; i++)
                _remoteLiveStates.Remove(_expiredLivePlayers[i]);
        }
    }
}
