using System;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Drive.Single.Session.Systems
{
    internal sealed class Progress : TopSpeed.Drive.Session.Subsystem
    {
        private readonly Tracks.Track _track;
        private readonly Vehicles.ICar _car;
        private readonly DriveSettings _settings;
        private readonly int _lapLimit;
        private readonly Func<int, Source?> _getLapSound;
        private readonly Func<int> _getLap;
        private readonly Action<int> _setLap;
        private readonly Action _applyPlayerFinishState;
        private readonly Action _onPlayerFinished;
        private readonly Action<Source, bool> _speak;

        public Progress(
            string name,
            int order,
            Tracks.Track track,
            Vehicles.ICar car,
            DriveSettings settings,
            int lapLimit,
            Func<int, Source?> getLapSound,
            Func<int> getLap,
            Action<int> setLap,
            Action applyPlayerFinishState,
            Action onPlayerFinished,
            Action<Source, bool> speak)
            : base(name, order)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _lapLimit = lapLimit;
            _getLapSound = getLapSound ?? throw new ArgumentNullException(nameof(getLapSound));
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _setLap = setLap ?? throw new ArgumentNullException(nameof(setLap));
            _applyPlayerFinishState = applyPlayerFinishState ?? throw new ArgumentNullException(nameof(applyPlayerFinishState));
            _onPlayerFinished = onPlayerFinished ?? throw new ArgumentNullException(nameof(onPlayerFinished));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            var currentLap = _track.Lap(_car.PositionY);
            if (currentLap <= _getLap())
                return;

            _setLap(currentLap);
            if (currentLap > _lapLimit)
            {
                _applyPlayerFinishState();
                _onPlayerFinished();
                return;
            }

            if (_settings.AutomaticInfo != AutomaticInfoMode.Off
                && currentLap > 1
                && currentLap <= _lapLimit)
            {
                var lapSound = _getLapSound(_lapLimit - currentLap);
                if (lapSound != null)
                    _speak(lapSound, true);
            }
        }
    }
}
