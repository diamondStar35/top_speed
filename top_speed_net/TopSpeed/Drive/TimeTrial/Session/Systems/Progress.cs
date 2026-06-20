using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Drive.TimeTrial.Session.Systems
{
    internal sealed class Progress : TopSpeed.Drive.Session.Subsystem
    {
        private readonly Tracks.Track _track;
        private readonly Vehicles.ICar _car;
        private readonly DriveSettings _settings;
        private readonly int _lapLimit;
        private readonly Func<int, Source?> _getLapSound;
        private readonly List<int> _lapTimes;
        private readonly Func<int> _getLap;
        private readonly Action<int> _setLap;
        private readonly Func<int> _getLastLapRaceTimeMs;
        private readonly Action<int> _setLastLapRaceTimeMs;
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
            List<int> lapTimes,
            Func<int> getLap,
            Action<int> setLap,
            Func<int> getLastLapRaceTimeMs,
            Action<int> setLastLapRaceTimeMs,
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
            _lapTimes = lapTimes ?? throw new ArgumentNullException(nameof(lapTimes));
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _setLap = setLap ?? throw new ArgumentNullException(nameof(setLap));
            _getLastLapRaceTimeMs = getLastLapRaceTimeMs ?? throw new ArgumentNullException(nameof(getLastLapRaceTimeMs));
            _setLastLapRaceTimeMs = setLastLapRaceTimeMs ?? throw new ArgumentNullException(nameof(setLastLapRaceTimeMs));
            _applyPlayerFinishState = applyPlayerFinishState ?? throw new ArgumentNullException(nameof(applyPlayerFinishState));
            _onPlayerFinished = onPlayerFinished ?? throw new ArgumentNullException(nameof(onPlayerFinished));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            var currentLap = _track.Lap(_car.PositionY);
            var lap = _getLap();
            if (currentLap <= lap)
                return;

            var completedLap = currentLap - 1;
            if (completedLap >= 1 && completedLap <= _lapLimit)
            {
                var lapTimeMs = context.ProgressMilliseconds - _getLastLapRaceTimeMs();
                if (lapTimeMs > 0)
                    _lapTimes.Add(lapTimeMs);

                _setLastLapRaceTimeMs(context.ProgressMilliseconds);
            }

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
