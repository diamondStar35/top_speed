using System;
using TopSpeed.Audio;
using TopSpeed.Drive.Session;
using TS.Audio;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
    {
        public void Initialize()
        {
            _track.Initialize();
            _car.Initialize();
            _car.SetOverrideController(null);
            _car.ManualTransmission = _manualTransmission;
            _panels.Resume();
            _listener.Reset();
            _trackAudio.Reset();
            _generalRequests.Reset();
            _currentRoad.Surface = _track.InitialSurface;
            _lastRecordedCarState = _car.State;
            _exitWhenQueueIdle = false;
            _requirePostFinishStopBeforeExit = false;
            _pendingResultSummary = null;
            _localCrashCount = 0;
            _lapTimes.Clear();
            _lastLapRaceTimeMs = 0;
            _soundQueue.Clear();
            _raceInfoQueue.Clear();
            _trackInfoQueue.Clear();
            _unkeyQueue = 0;
            _speakTime = 0f;
            _lap = 0;
            _raceTime = 0;
            _started = false;
            _finished = false;
            _session.Reset();
            _session.SetPhase(Phase.Countdown);
            _session.QueueEvent(new Event(Events.PlaySound, _soundStart), DefaultStartCueDelaySeconds);
            _session.QueueEvent(new Event(Events.VehicleStart), 3.0f);
            _session.QueueEvent(new Event(Events.ProgressStart), DefaultProgressStartDelaySeconds);
            if (_soundTheme != null)
                _soundTheme.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
        }

        public void FinalizeSession()
        {
            _panels.Pause();
            _car.FinalizeCar();
            _track.FinalizeTrack();
        }

        public void Pause()
        {
            _session.ApplyCommand(new Command(Commands.Pause));
        }

        public void Resume()
        {
            _session.ApplyCommand(new Command(Commands.Resume));
        }

        public void ClearPauseRequest()
        {
            _session.ApplyCommand(new Command(Commands.ClearPauseRequest));
        }

        public DriveResultSummary? ConsumeResultSummary()
        {
            var summary = _pendingResultSummary;
            _pendingResultSummary = null;
            return summary;
        }

        public void Dispose()
        {
            _soundQueue.Clear();
            _raceInfoQueue.Clear();
            _trackInfoQueue.Clear();
            _panelManager.Dispose();
            _localRadio.Dispose();
            _car.Dispose();
            _track.Dispose();
            DisposeSound(_soundStart);
            DisposeSound(_soundTheme);
            DisposeSound(_soundPause);
            DisposeSound(_soundResume);
            DisposeSound(_soundTurnEndDing);

            for (var i = 0; i < _soundUnkey.Length; i++)
                DisposeSound(_soundUnkey[i]);

            for (var i = 0; i < _soundLaps.Length; i++)
                DisposeSound(_soundLaps[i]);

            for (var i = 0; i < _randomSounds.Length; i++)
            {
                var count = _totalRandomSounds[i];
                for (var j = 0; j < count && j < _randomSounds[i].Length; j++)
                    DisposeSound(_randomSounds[i][j]);
            }
        }

        private bool UpdateExitWhenQueueIdle()
        {
            if (!_exitWhenQueueIdle)
                return false;
            if (_requirePostFinishStopBeforeExit && _car.Speed > PostFinishStopSpeedKph)
                return false;
            return _soundQueue.IsIdle && _raceInfoQueue.IsIdle && _trackInfoQueue.IsIdle;
        }

        private void RequestExitWhenQueueIdle()
        {
            _exitWhenQueueIdle = true;
        }

        private void TrackLocalCrashState()
        {
            var currentState = _car.State;
            var wasCrashing = _lastRecordedCarState == TopSpeed.Vehicles.CarState.Crashing || _lastRecordedCarState == TopSpeed.Vehicles.CarState.Crashed;
            var isCrashing = currentState == TopSpeed.Vehicles.CarState.Crashing || currentState == TopSpeed.Vehicles.CarState.Crashed;
            if (!wasCrashing && isCrashing)
                _localCrashCount++;

            _lastRecordedCarState = currentState;
        }

        private void ApplyPlayerFinishState()
        {
            _finished = true;
            PlayFinishAnnouncement();
            TopSpeed.Drive.Session.FinishVehicle.Apply(_car, _finishLockController);
            _raceTime = _session.Context.ProgressMilliseconds;
            _requirePostFinishStopBeforeExit = true;
            _session.SetPhase(Phase.Finishing);
        }

        private static void DisposeSound(Source? sound)
        {
            if (sound == null)
                return;

            sound.Stop();
            sound.Dispose();
        }
    }
}
