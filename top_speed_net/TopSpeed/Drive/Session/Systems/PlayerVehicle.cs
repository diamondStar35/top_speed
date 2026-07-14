using System;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class PlayerVehicle : Subsystem
    {
        private readonly ICar _car;
        private readonly DriveInput _input;
        private readonly Track _track;
        private readonly DriveSettings _settings;
        private readonly TrackAudio _trackAudio;
        private readonly Func<Track.Road> _getCurrentRoad;
        private readonly Action<Track.Road> _setCurrentRoad;
        private readonly Func<bool> _isStarted;
        private readonly Func<bool> _isFinished;
        private readonly Action _trackCrashState;
        private readonly Action<string> _speakText;
        private readonly Func<bool>? _skipCrashEval;

        public PlayerVehicle(
            string name,
            int order,
            ICar car,
            DriveInput input,
            Track track,
            DriveSettings settings,
            TrackAudio trackAudio,
            Func<Track.Road> getCurrentRoad,
            Action<Track.Road> setCurrentRoad,
            Func<bool> isStarted,
            Func<bool> isFinished,
            Action trackCrashState,
            Action<string> speakText,
            Func<bool>? skipCrashEval = null)
            : base(name, order)
        {
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _trackAudio = trackAudio ?? throw new ArgumentNullException(nameof(trackAudio));
            _getCurrentRoad = getCurrentRoad ?? throw new ArgumentNullException(nameof(getCurrentRoad));
            _setCurrentRoad = setCurrentRoad ?? throw new ArgumentNullException(nameof(setCurrentRoad));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _isFinished = isFinished ?? throw new ArgumentNullException(nameof(isFinished));
            _trackCrashState = trackCrashState ?? throw new ArgumentNullException(nameof(trackCrashState));
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
            _skipCrashEval = skipCrashEval;
        }

        public override void Update(SessionContext context, float elapsed)
        {
            var previousGear = _car.Gear;
            _car.Run(elapsed);
            TryAnnounceGearShift(previousGear);

            _track.Run(_car.PositionY);
            var road = _track.RoadAtPosition(_car.PositionY);
            _trackAudio.HandleRoad(road);
            if (_skipCrashEval == null || !_skipCrashEval())
                _car.Evaluate(road);
            _trackCrashState();

            if (_track.NextRoad(
                _car.PositionY,
                _car.Speed,
                (int)_settings.CurveAnnouncement,
                _settings.CurveAnnouncementLeadTimeSeconds,
                out var nextRoad))
                _setCurrentRoad(_trackAudio.AnnounceNextRoad(_getCurrentRoad(), nextRoad));
        }

        private void TryAnnounceGearShift(int previousGear)
        {
            if (!_isStarted() || _isFinished())
                return;

            if (!GearAnnouncements.ShouldAnnounceUserShift(_car, _input, previousGear))
                return;

            _speakText(SessionText.FormatGearCode(_car));
        }
    }
}
