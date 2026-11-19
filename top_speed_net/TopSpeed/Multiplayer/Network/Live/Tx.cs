using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Live
{
    internal sealed class Tx : IDisposable
    {
        private const float FrameSeconds = ProtocolConstants.LiveFrameMs / 1000f;

        private readonly Opus _encoder;
        private Source? _source;
        private MultiplayerSession _session;

        private uint _streamId;
        private string _mediaPath;
        private bool _loaded;
        private bool _playing;
        private bool _pausedByGame;
        private bool _started;
        private float _frameClock;
        private uint _timestampMs;

        public Tx(MultiplayerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _encoder = new Opus((byte)ProtocolConstants.LiveChannelsMax);
            _mediaPath = string.Empty;
        }

        public void ReplaceSession(MultiplayerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _started = false;
            _frameClock = 0f;
        }

        public bool SetMedia(uint mediaId, string mediaPath, out string error)
        {
            error = string.Empty;
            if (mediaId == 0 || string.IsNullOrWhiteSpace(mediaPath))
            {
                ClearSource();
                return true;
            }

            if (_streamId == mediaId && string.Equals(_mediaPath, mediaPath, StringComparison.OrdinalIgnoreCase) && _source != null)
                return true;

            if (!StopIfStarted(out error))
                return false;

            ClearSource();
            if (!Source.TryOpen(mediaPath, out var source))
            {
                error = LocalizationService.Mark("Failed to open local radio media for live streaming.");
                return false;
            }

            _source = source;
            _streamId = mediaId;
            _mediaPath = mediaPath;
            _timestampMs = 0;
            _frameClock = 0f;
            _encoder.Reset();
            return true;
        }

        public bool SetPlayback(bool loaded, bool playing, uint mediaId, out string error)
        {
            error = string.Empty;
            _loaded = loaded && mediaId != 0;
            _playing = _loaded && playing;

            if (!_loaded)
            {
                if (!StopIfStarted(out error))
                    return false;
                ClearSource();
                return true;
            }

            if (mediaId != _streamId || _source == null)
            {
                _playing = false;
                if (!StopIfStarted(out error))
                    return false;
                return true;
            }

            if (!_playing && !StopIfStarted(out error))
                return false;

            return true;
        }

        public void Pause()
        {
            _pausedByGame = true;
            if (_started)
                StopIfStarted(out _);
        }

        public void Resume()
        {
            _pausedByGame = false;
        }

        public bool Update(float elapsedSeconds, out string error)
        {
            error = string.Empty;
            if (elapsedSeconds <= 0f)
                return true;

            if (_pausedByGame || !_loaded || !_playing || _source == null || _streamId == 0)
            {
                if (!StopIfStarted(out error))
                    return false;
                return true;
            }

            if (!_started)
            {
                if (!_session.SendLiveStart(_streamId, _encoder.Profile))
                {
                    error = LocalizationService.Mark("Network send failed while sending live start.");
                    return false;
                }

                _started = true;
                _frameClock = 0f;
            }

            _frameClock += elapsedSeconds;
            while (_frameClock >= FrameSeconds)
            {
                _frameClock -= FrameSeconds;
                if (!_source.TryRead(out var samples))
                {
                    error = LocalizationService.Mark("Live streaming stopped because media decoding failed.");
                    return false;
                }

                var pcm = new LivePcmFrame(
                    _encoder.Profile.SampleRate,
                    _encoder.Profile.Channels,
                    _encoder.Profile.FrameMs,
                    samples,
                    _timestampMs);

                if (!_encoder.TryEncode(in pcm, out var frame))
                {
                    error = LocalizationService.Mark("Live streaming stopped because Opus encoding failed.");
                    return false;
                }

                if (!_session.SendLiveFrame(_streamId, in frame))
                {
                    error = LocalizationService.Mark("Network send failed while sending live frame.");
                    return false;
                }

                _timestampMs = unchecked(_timestampMs + (uint)_encoder.Profile.FrameMs);
            }

            return true;
        }

        public void Dispose()
        {
            StopIfStarted(out _);
            ClearSource();
        }

        private bool StopIfStarted(out string error)
        {
            error = string.Empty;
            if (!_started || _streamId == 0)
                return true;

            if (!_session.SendLiveStop(_streamId))
            {
                error = LocalizationService.Mark("Network send failed while sending live stop.");
                return false;
            }

            _started = false;
            _frameClock = 0f;
            return true;
        }

        private void ClearSource()
        {
            _source?.Dispose();
            _source = null;
            _streamId = 0;
            _mediaPath = string.Empty;
            _frameClock = 0f;
            _timestampMs = 0;
            _encoder.Reset();
        }
    }
}

