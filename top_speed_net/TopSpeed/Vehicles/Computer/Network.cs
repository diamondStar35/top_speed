using System;
using TopSpeed.Audio;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        public void ApplyNetworkState(
            float positionX,
            float positionY,
            float speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            bool radioLoaded,
            bool radioPlaying,
            uint radioMediaId,
            byte radioVolumePercent,
            float playerX,
            float playerY,
            float trackLength)
        {
            var incomingX = positionX;
            var incomingY = Math.Max(0f, positionY);
            var incomingSpeed = Math.Max(0f, speed);
            // Captured before interpolation overwrites it: the server snaps a finisher to a
            // standstill in one packet, so this is the last speed the car was actually doing and
            // therefore the note the listener is hearing right now.
            var speedBeforeUpdateKph = _remoteNetInit ? _speed : incomingSpeed;
            _trackLength = trackLength;
            var preserveCrashState = _state == ComputerState.Crashing && !engineRunning;
            var preserveFinishStoppingState = _finished && _state == ComputerState.Stopping && !engineRunning;
            var snapToIncoming = !_remoteNetInit;

            if (!_remoteNetInit)
            {
                _remoteNetInit = true;
                _remoteTargetX = incomingX;
                _remoteTargetY = incomingY;
                _remoteTargetSpeed = incomingSpeed;
                _positionX = incomingX;
                _positionY = incomingY;
                _speed = incomingSpeed;
            }
            else
            {
                var dx = incomingX - _positionX;
                var dy = incomingY - _positionY;
                if (Math.Abs(dx) > RemoteInterpSnapLateral || Math.Abs(dy) > RemoteInterpSnapDistance)
                    snapToIncoming = true;

                _remoteTargetX = incomingX;
                _remoteTargetY = incomingY;
                _remoteTargetSpeed = incomingSpeed;
            }

            if (snapToIncoming)
            {
                _positionX = incomingX;
                _positionY = incomingY;
                _speed = incomingSpeed;
            }

            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;

            var elapsed = 0f;
            var now = _currentTime();
            if (_audioInitialized)
            {
                elapsed = now - _lastAudioUpdateTime;
                if (elapsed < 0f)
                    elapsed = 0f;
            }
            _lastAudioUpdateTime = now;
            if (snapToIncoming)
                UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);

            if (engineRunning)
            {
                var targetFrequency = frequency > 0 ? frequency : _idleFreq;
                _remoteEnginePendingFrequency = targetFrequency;
                if (!_soundEngine.IsPlaying)
                {
                    if (!_remoteEngineStartPending)
                    {
                        _soundStart.Stop();
                        _soundStart.SeekToStart();
                        _soundStart.Play(loop: false);
                        _remoteEngineStartPending = true;
                        _remoteEngineStartRemaining = Math.Max(0f, _soundStart.LengthSeconds - 0.1f);
                    }
                }
                if (_soundEngine.IsPlaying && _prevFrequency != targetFrequency)
                {
                    _soundEngine.SetFrequency(targetFrequency);
                    _prevFrequency = targetFrequency;
                }
            }
            else
            {
                _remoteEngineStartPending = false;
                _remoteEngineStartRemaining = 0f;
                // A running engine that stops is a shutdown, not a mute. Winding it down here
                // rather than at the finish event also removes the ordering hazard: the snapshot
                // that reports the engine off can arrive before the finish packet, and cutting the
                // loop on it is what made a finishing car vanish mid-note.
                if (preserveCrashState)
                {
                    CancelEngineShutdown();
                    if (_soundEngine.IsPlaying)
                        _soundEngine.Stop();
                }
                else if (_soundEngine.IsPlaying)
                {
                    BeginEngineShutdown(speedBeforeUpdateKph);
                }
            }

            if (braking)
            {
                if (!_soundBrake.IsPlaying)
                    _soundBrake.Play(loop: true);
                var speedRatio = NormalizeSpeedByTopSpeed(incomingSpeed, 1f);
                var targetBrakeFrequency = (int)(11025 + (22050 * speedRatio));
                if (_prevBrakeFrequency != targetBrakeFrequency)
                {
                    _soundBrake.SetFrequency(targetBrakeFrequency);
                    _prevBrakeFrequency = targetBrakeFrequency;
                }
            }
            else if (_soundBrake.IsPlaying)
            {
                _soundBrake.Stop();
            }

            if (horning)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else if (_soundHorn.IsPlaying)
            {
                _soundHorn.Stop();
            }

            if (backfiring && !_networkBackfireActive && _soundBackfire != null)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
                _soundBackfire.Play(loop: false);
            }
            _networkBackfireActive = backfiring;
            ApplyRadioState(radioLoaded, radioPlaying, radioMediaId, radioVolumePercent);

            if (preserveCrashState)
                _state = ComputerState.Crashing;
            else if (preserveFinishStoppingState || _engineShutdownActive)
                _state = ComputerState.Stopping;
            else if (_remoteEngineStartPending)
                _state = ComputerState.Starting;
            else if (engineRunning)
                _state = ComputerState.Running;
            else
                _state = ComputerState.Stopped;
        }

        public void UpdateRemoteAudio(float playerX, float playerY, float trackLength, float elapsed)
        {
            _trackLength = trackLength;
            AdvanceRemoteInterpolation(elapsed);
            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);
            // Covers a car told to stop without a further snapshot - a finish event, or the race
            // ending - which would otherwise sit on its last note until the sound was torn down.
            if (_state == ComputerState.Stopping)
                BeginEngineShutdown(_speed);
            AdvanceEngineShutdown(elapsed);
            if (_remoteEngineStartPending)
            {
                _remoteEngineStartRemaining -= Math.Max(0f, elapsed);
                if (_remoteEngineStartRemaining <= 0f)
                {
                    _remoteEngineStartPending = false;
                    if (!_soundEngine.IsPlaying)
                        _soundEngine.Play(loop: true);
                    if (_prevFrequency != _remoteEnginePendingFrequency)
                    {
                        _soundEngine.SetFrequency(_remoteEnginePendingFrequency);
                        _prevFrequency = _remoteEnginePendingFrequency;
                    }
                }
            }
        }

        private void AdvanceRemoteInterpolation(float elapsed)
        {
            if (!_remoteNetInit)
                return;

            var dt = Math.Max(0f, elapsed);
            if (dt <= 0f)
                return;

            var alpha = 1f - (float)Math.Exp(-RemoteInterpRate * dt);
            if (alpha <= 0f)
                return;
            if (alpha > 1f)
                alpha = 1f;

            _positionX += (_remoteTargetX - _positionX) * alpha;
            _positionY += (_remoteTargetY - _positionY) * alpha;
            if (_positionY < 0f)
                _positionY = 0f;
            _speed += (_remoteTargetSpeed - _speed) * alpha;
            if (_speed < 0f)
                _speed = 0f;
        }

        public void ApplyRadioState(bool loaded, bool playing, uint mediaId, byte radioVolumePercent)
        {
            SetRemoteRadioSenderVolumePercent(radioVolumePercent);
            _radioLoaded = loaded;
            _radioPlaying = loaded && playing;
            _radioMediaId = loaded ? mediaId : 0u;
            if (!loaded)
            {
                _liveRadio.SetPlayback(false);
                _liveRadio.Stop(0);
                _radio.ClearMedia();
                return;
            }

            if (_liveRadio.IsActive && mediaId != 0 && _liveRadio.StreamId != mediaId)
                _liveRadio.Stop(0);

            _liveRadio.SetPlayback(_radioPlaying);
            if (_liveRadio.IsActive)
            {
                _radio.SetPlayback(false);
                return;
            }

            if (_radio.HasMedia && mediaId != 0 && _radio.MediaId != mediaId)
                _radio.ClearMedia();

            _radio.SetPlayback(_radioPlaying);
        }

        public void ApplyRadioMedia(uint mediaId, string extension, byte[] data)
        {
            if (mediaId == 0 || data == null || data.Length == 0)
                return;
            if (_liveRadio.IsActive)
                return;
            if (!_radio.TryLoadFromBytes(data, extension, mediaId, preservePlaybackState: true, out _))
                return;
            _radioLoaded = true;
            _radioMediaId = mediaId;
            _radio.SetPlayback(_radioPlaying);
        }

        public bool ApplyLiveStart(uint streamId, LiveCodec codec, ushort sampleRate, byte channels, byte frameMs)
        {
            var started = _liveRadio.Start(streamId, codec, sampleRate, channels, frameMs);
            if (started)
                _liveRadio.SetPlayback(true);
            return started;
        }

        public bool ApplyLiveFrame(uint streamId, byte[] payload, uint timestamp)
        {
            return _liveRadio.PushFrame(streamId, payload, timestamp);
        }

        public void ApplyLiveStop(uint streamId)
        {
            _liveRadio.Stop(streamId);
            if (_radioLoaded)
                _radio.SetPlayback(_radioPlaying);
        }

        public void StopLiveStream()
        {
            _liveRadio.Stop(0);
        }
    }
}
