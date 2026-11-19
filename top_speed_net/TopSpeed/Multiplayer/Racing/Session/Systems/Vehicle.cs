using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Multiplayer.Session.Systems
{
    internal sealed class Vehicle : TopSpeed.Drive.Session.Subsystem
    {
        private readonly AudioManager _audio;
        private readonly ICar _car;
        private readonly DriveInput _input;
        private readonly Track _track;
        private readonly DriveSettings _settings;
        private readonly TopSpeed.Drive.Session.Systems.TrackAudio _trackAudio;
        private readonly VehicleRadioController _localRadio;
        private readonly IDictionary<byte, RemotePlayer> _remotePlayers;
        private readonly Func<Track.Road> _getCurrentRoad;
        private readonly Action<Track.Road> _setCurrentRoad;
        private readonly Func<bool> _isStarted;
        private readonly Func<bool> _isFinished;
        private readonly Func<bool> _isHostPaused;
        private readonly Func<float> _getSpatialTrackLength;
        private readonly Action _trackCrashState;
        private readonly Action<string> _speakText;
        private readonly Func<bool>? _skipCrashEval;
        private readonly Func<float?>? _getListenerXOverride;
        private Vector3 _lastListenerPosition;
        private bool _listenerInitialized;

        public Vehicle(
            string name,
            int order,
            AudioManager audio,
            ICar car,
            DriveInput input,
            Track track,
            DriveSettings settings,
            TopSpeed.Drive.Session.Systems.TrackAudio trackAudio,
            VehicleRadioController localRadio,
            IDictionary<byte, RemotePlayer> remotePlayers,
            Func<Track.Road> getCurrentRoad,
            Action<Track.Road> setCurrentRoad,
            Func<bool> isStarted,
            Func<bool> isFinished,
            Func<bool> isHostPaused,
            Func<float> getSpatialTrackLength,
            Action trackCrashState,
            Action<string> speakText,
            Func<bool>? skipCrashEval = null,
            Func<float?>? getListenerXOverride = null)
            : base(name, order)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _trackAudio = trackAudio ?? throw new ArgumentNullException(nameof(trackAudio));
            _localRadio = localRadio ?? throw new ArgumentNullException(nameof(localRadio));
            _remotePlayers = remotePlayers ?? throw new ArgumentNullException(nameof(remotePlayers));
            _getCurrentRoad = getCurrentRoad ?? throw new ArgumentNullException(nameof(getCurrentRoad));
            _setCurrentRoad = setCurrentRoad ?? throw new ArgumentNullException(nameof(setCurrentRoad));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _isFinished = isFinished ?? throw new ArgumentNullException(nameof(isFinished));
            _isHostPaused = isHostPaused ?? throw new ArgumentNullException(nameof(isHostPaused));
            _getSpatialTrackLength = getSpatialTrackLength ?? throw new ArgumentNullException(nameof(getSpatialTrackLength));
            _trackCrashState = trackCrashState ?? throw new ArgumentNullException(nameof(trackCrashState));
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
            _skipCrashEval = skipCrashEval;
            _getListenerXOverride = getListenerXOverride;
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            if (_isHostPaused() && !_isFinished())
                RunPausedStep(elapsed);
            else
                RunActiveStep(elapsed);
        }

        public void Reset()
        {
            _lastListenerPosition = Vector3.Zero;
            _listenerInitialized = false;
        }

        private void RunActiveStep(float elapsed)
        {
            var previousGear = _car.Gear;
            _car.Run(elapsed);
            TryAnnounceGearShift(previousGear);
            _track.Run(_car.PositionY);

            var spatialTrackLength = _getSpatialTrackLength();
            foreach (var remote in _remotePlayers.Values)
                remote.Player.UpdateRemoteAudio(_car.PositionX, _car.PositionY, spatialTrackLength, elapsed);

            var road = _track.RoadAtPosition(_car.PositionY);
            _trackAudio.HandleRoad(road);
            if (_skipCrashEval == null || !_skipCrashEval())
                _car.Evaluate(road);
            _trackCrashState();
            UpdateListener(elapsed);

            if (_track.NextRoad(
                _car.PositionY,
                _car.Speed,
                (int)_settings.CurveAnnouncement,
                _settings.CurveAnnouncementLeadTimeSeconds,
                out var nextRoad))
                _setCurrentRoad(_trackAudio.AnnounceNextRoad(_getCurrentRoad(), nextRoad));
        }

        private void RunPausedStep(float elapsed)
        {
            _car.StopMotionImmediately();
            UpdateListener(elapsed);
        }

        private void TryAnnounceGearShift(int previousGear)
        {
            if (!_isStarted() || _isFinished())
                return;
            if (!TopSpeed.Drive.Session.Systems.GearAnnouncements.ShouldAnnounceUserShift(_car, _input, previousGear))
                return;

            _speakText(TopSpeed.Drive.Session.SessionText.FormatGearCode(_car));
        }

        private void UpdateListener(float elapsed)
        {
            var driverOffsetX = -_car.WidthM * 0.25f;
            var driverOffsetZ = _car.LengthM * 0.1f;
            var overrideX = _getListenerXOverride?.Invoke();
            var rawX = overrideX.HasValue ? overrideX.Value : _car.PositionX;
            var worldPosition = new Vector3(rawX + driverOffsetX, 0f, _car.PositionY + driverOffsetZ);
            var worldVelocity = Vector3.Zero;
            if (_listenerInitialized && elapsed > 0f)
                worldVelocity = (worldPosition - _lastListenerPosition) / elapsed;

            _lastListenerPosition = worldPosition;
            _listenerInitialized = true;

            var forward = new Vector3(0f, 0f, 1f);
            var up = new Vector3(0f, 1f, 0f);
            _audio.UpdateListener(AudioWorld.ToMeters(worldPosition), forward, up, AudioWorld.ToMeters(worldVelocity));
            _localRadio.UpdateSpatial(worldPosition.X, worldPosition.Z, worldVelocity);
        }
    }
}
