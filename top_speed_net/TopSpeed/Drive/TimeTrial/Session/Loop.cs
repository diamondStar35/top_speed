using System;
using TopSpeed.Drive.Session;
using TS.Audio;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
    {
        public void Run(float elapsed)
        {
            RefreshCategoryVolumes();
            _session.Update(elapsed);
        }

        private void HandleSessionEvent(SessionContext context, Event sessionEvent)
        {
            switch (sessionEvent.Id)
            {
                case Events.VehicleStart:
                    break;
                case Events.ProgressStart:
                    _session.SetPhase(Phase.Running);
                    _raceTime = 0;
                    _lap = 0;
                    _started = true;
                    _lapTimes.Clear();
                    _lastLapRaceTimeMs = 0;
                    break;
                case Events.ProgressFinish:
                    FinalizeTimeTrialRun();
                    _session.QueueEvent(new Event(Events.FinalizeResults), 0f);
                    break;
                case Events.FinalizeResults:
                    _session.SetPhase(Phase.Finished);
                    RequestExitWhenQueueIdle();
                    break;
                case Events.PlaySound:
                    QueueSound(sessionEvent.Data as Source);
                    break;
                case Events.PlayInfoSound:
                    QueueRaceInfoSound(sessionEvent.Data as Source);
                    break;
                case Events.PlayTrackInfoSound:
                    QueueTrackInfoSound(sessionEvent.Data as Source);
                    break;
                case Events.PlayUnkey:
                    _unkeyQueue--;
                    if (_unkeyQueue == 0)
                        Speak(_soundUnkey[TopSpeed.Common.Algorithm.RandomInt(MaxUnkeys)]);
                    break;
            }
        }

        private void HandlePhaseEvent(SessionContext context, Event sessionEvent)
        {
            if (sessionEvent.Id != Events.PhaseChanged || sessionEvent.Data is not PhaseChanged phaseChanged)
                return;

            _panels.ApplyInputPolicy(context.InputPolicy);

            if (phaseChanged.Current == Phase.Paused)
            {
                _soundQueue.Pause();
                _raceInfoQueue.Pause();
                _trackInfoQueue.Pause();
                _track.PauseAudio();
                _soundTheme?.Play(loop: true);
                FadeInTheme();
                _panels.Pause();
                _car.Pause();
                _soundPause?.Play(loop: false);
                return;
            }

            if (phaseChanged.Previous == Phase.Paused)
            {
                _soundQueue.Resume();
                _raceInfoQueue.Resume();
                _trackInfoQueue.Resume();
                _track.ResumeAudio();
                _car.Unpause();
                _panels.Resume();
                FadeOutTheme();
                _soundTheme?.Stop();
                _soundTheme?.SeekToStart();
                _soundResume?.Play(loop: false);
            }
        }

        private void FinalizeTimeTrialRun()
        {
            var previous = _scores.Read(_trackId, _nrOfLaps);
            var beatRecord = previous.RunBestMs <= 0 || _raceTime < previous.RunBestMs;
            var currentBestLap = _lapTimes.Count == 0 ? 0 : System.Linq.Enumerable.Min(_lapTimes);
            var snapshot = _scores.RecordRun(_trackId, _track.TrackName, _nrOfLaps, _raceTime, _lapTimes.ToArray());

            _pendingResultSummary = new DriveResultSummary
            {
                Mode = DriveResultMode.TimeTrial,
                IsMultiplayer = false,
                LocalPosition = 1,
                LocalCrashCount = _localCrashCount,
                TimeTrialBeatRecord = beatRecord,
                TimeTrialLapCount = _nrOfLaps,
                TimeTrialCurrentRunMs = _raceTime,
                TimeTrialBestRunMs = snapshot.RunBestMs,
                TimeTrialAverageRunMs = snapshot.RunAverageMs,
                TimeTrialBestLapThisRunMs = currentBestLap,
                TimeTrialBestLapMs = snapshot.LapBestMs,
                TimeTrialAverageLapMs = snapshot.LapAverageMs,
                Entries = Array.Empty<DriveResultEntry>()
            };
        }

        private uint NextLocalMediaId()
        {
            _nextMediaId++;
            if (_nextMediaId == 0)
                _nextMediaId = 1;
            return _nextMediaId;
        }
    }
}
