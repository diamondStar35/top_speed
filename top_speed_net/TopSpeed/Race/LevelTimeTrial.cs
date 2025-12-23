using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Common;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed class LevelTimeTrial : Level
    {
        private const string HighscoreFile = "highscore.cfg";
        private AudioSourceHandle? _soundVehicle;
        private bool _pauseKeyReleased = true;

        public LevelTimeTrial(
            AudioManager audio,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            JoystickDevice? joystick)
            : base(audio, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, joystick)
        {
            if (!_car.UserDefined)
            {
                var file = $"vehicles\\vehicle{(int)_car.CarType + 1}";
                _soundVehicle = LoadLanguageSound(file);
            }
            else if (!string.IsNullOrWhiteSpace(_car.CustomFile))
            {
                var file = _car.CustomFile!;
                if (!file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    file += ".wav";
                _soundVehicle = LoadCustomSound(file);
            }
        }

        public void Initialize()
        {
            InitializeLevel();
            _soundTheme4 = LoadLanguageSound("music\\theme4");
            _soundPause = LoadLanguageSound("race\\pause");
            _soundUnpause = LoadLanguageSound("race\\unpause");
            _soundTheme4.SetVolumePercent(50);
        }

        public void FinalizeLevelTimeTrial()
        {
            FinalizeLevel();
        }

        public void Run(float elapsed)
        {
            if (_elapsedTotal == 0.0f)
            {
                PushEvent(RaceEventType.CarStart, 1.5f);
                PushEvent(RaceEventType.RaceStart, 5.0f);
                _soundStart.Play(loop: false);
            }

            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time <= _elapsedTotal)
                {
                    _events.RemoveAt(i);
                    switch (e.Type)
                    {
                        case RaceEventType.CarStart:
                            _car.Start();
                            break;
                        case RaceEventType.RaceStart:
                            _raceTime = 0;
                            _stopwatch.Restart();
                            _lap = 0;
                            _started = true;
                            break;
                        case RaceEventType.RaceFinish:
                            PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundYourTime);
                            _sayTimeLength += _soundYourTime.GetLengthSeconds() + 0.5f;
                            SayTime(_raceTime);
                            _highscore = ReadHighScore();
                            if ((_raceTime < _highscore) || (_highscore == 0))
                            {
                                WriteHighScore();
                                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNewTime);
                                _sayTimeLength += _soundNewTime.GetLengthSeconds();
                            }
                            else
                            {
                                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundBestTime);
                                _sayTimeLength += _soundBestTime.GetLengthSeconds() + 0.5f;
                                SayTime(_highscore);
                            }
                            PushEvent(RaceEventType.RaceTimeFinalize, _sayTimeLength);
                            break;
                        case RaceEventType.PlaySound:
                            QueueSound(e.Sound);
                            break;
                        case RaceEventType.RaceTimeFinalize:
                            _sayTimeLength = 0.0f;
                            ExitRequested = true;
                            return;
                        case RaceEventType.PlayRadioSound:
                            _unkeyQueue--;
                            if (_unkeyQueue == 0)
                                Speak(_soundUnkey[Algorithm.RandomInt(MaxUnkeys)]);
                            break;
                        case RaceEventType.AcceptPlayerInfo:
                            _acceptPlayerInfo = true;
                            break;
                        case RaceEventType.AcceptCurrentRaceInfo:
                            _acceptCurrentRaceInfo = true;
                            break;
                    }
                }
            }

            _car.Run(elapsed);
            _track.Run(_car.PositionY);
            var road = _track.RoadAtPosition(_car.PositionY);
            _car.Evaluate(road);
            if (_track.NextRoad(_car.PositionY, _car.Speed, (int)_settings.CurveAnnouncement, out var nextRoad))
                CallNextRoad(nextRoad);

            if (_track.Lap(_car.PositionY) > _lap)
            {
                _lap = _track.Lap(_car.PositionY);
                if (_lap > _nrOfLaps)
                {
                    var finishSound = _randomSounds[(int)RandomSound.Finish][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Finish])];
                    if (finishSound != null)
                        Speak(finishSound, true);
                    _car.ManualTransmission = false;
                    _car.Quiet();
                    _car.Stop();
                    _raceTime = (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs);
                    PushEvent(RaceEventType.RaceFinish, 2.0f);
                }
                else if (_settings.AutomaticInfo != AutomaticInfoMode.Off && _lap > 1 && _lap < _nrOfLaps + 1)
                {
                    Speak(_soundLaps[_nrOfLaps - _lap], true);
                }
            }

            if (_input.GetCurrentGear() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var gear = _car.Gear;
                QueueSound(_soundNumbers[gear]);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundNumbers[gear].GetLengthSeconds());
            }

            if (_input.GetCurrentLapNr() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                QueueSound(_soundNumbers[_lap]);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundNumbers[_lap].GetLengthSeconds());
            }

            if (_input.GetCurrentRacePerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = (_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f;
                var units = (int)perc;
                var decs = (int)((perc - units) * 100.0f);
                QueueSound(_soundNumbers[units]);
                if (decs > 0)
                {
                    var time = _soundNumbers[units].GetLengthSeconds();
                    PushEvent(RaceEventType.PlaySound, time, _soundPoint);
                    time += _soundPoint.GetLengthSeconds();
                    if (decs < 10)
                    {
                        PushEvent(RaceEventType.PlaySound, time, _soundNumbers[0]);
                        time += _soundNumbers[0].GetLengthSeconds();
                        PushEvent(RaceEventType.PlaySound, time, _soundNumbers[decs]);
                        time += _soundNumbers[decs].GetLengthSeconds();
                        PushEvent(RaceEventType.PlaySound, time, _soundPercent);
                        time += _soundPercent.GetLengthSeconds();
                        PushEvent(RaceEventType.AcceptCurrentRaceInfo, time);
                    }
                    else
                    {
                        if (decs % 10 == 0)
                            decs /= 10;
                        PushEvent(RaceEventType.PlaySound, time, _soundNumbers[decs]);
                        time += _soundNumbers[decs].GetLengthSeconds();
                        PushEvent(RaceEventType.PlaySound, time, _soundPercent);
                        time += _soundPercent.GetLengthSeconds();
                        PushEvent(RaceEventType.AcceptCurrentRaceInfo, time);
                    }
                }
                else
                {
                    var time = _soundNumbers[units].GetLengthSeconds();
                    PushEvent(RaceEventType.PlaySound, time, _soundPercent);
                    time += _soundPercent.GetLengthSeconds();
                    PushEvent(RaceEventType.AcceptCurrentRaceInfo, time);
                }
            }

            if (_input.GetCurrentLapPerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = ((_car.PositionY - (_track.Length * (_lap - 1))) / (float)_track.Length) * 100.0f;
                var units = (int)perc;
                QueueSound(_soundNumbers[units]);
                var time = _soundNumbers[units].GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, time, _soundPercent);
                time += _soundPercent.GetLengthSeconds();
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, time);
            }

            if (_input.GetCurrentRaceTime() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                _sayTimeLength = 0.0f;
                SayTime((int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs), detailed: false);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _sayTimeLength);
            }

            if (_input.TryGetPlayerInfo(out var player) && _acceptPlayerInfo && player == 0 && _soundVehicle != null)
            {
                _acceptPlayerInfo = false;
                QueueSound(_soundVehicle);
                PushEvent(RaceEventType.AcceptPlayerInfo, _soundVehicle.GetLengthSeconds());
            }

            if (_input.GetTrackName() && _acceptCurrentRaceInfo && _soundTrackName != null)
            {
                _acceptCurrentRaceInfo = false;
                QueueSound(_soundTrackName);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundTrackName.GetLengthSeconds());
            }

            if (!_input.GetPause() && !_pauseKeyReleased)
            {
                _pauseKeyReleased = true;
            }
            else if (_input.GetPause() && _pauseKeyReleased && _started && _lap <= _nrOfLaps && _car.State == CarState.Running)
            {
                _pauseKeyReleased = false;
                PauseRequested = true;
            }

            _elapsedTotal += elapsed;
        }

        public void Pause()
        {
            FadeIn();
            _soundTheme4?.Play(loop: true);
            _car.Pause();
            _soundPause?.Play(loop: false);
        }

        public void Unpause()
        {
            _car.Unpause();
            FadeOut();
            _soundTheme4?.Stop();
            _soundTheme4?.SeekToStart();
            _soundUnpause?.Play(loop: false);
        }

        private int ReadHighScore()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HighscoreFile);
            if (!File.Exists(path))
                return 0;
            var key = $"{_track.TrackName};{_nrOfLaps}";
            foreach (var line in File.ReadLines(path))
            {
                var idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;
                var field = line.Substring(0, idx).Trim();
                if (!string.Equals(field, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                var valuePart = line.Substring(idx + 1).Trim();
                if (int.TryParse(valuePart, out var value))
                    return value;
            }
            return 0;
        }

        private void WriteHighScore()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HighscoreFile);
            var key = $"{_track.TrackName};{_nrOfLaps}";
            var lines = new List<string>();
            var found = false;
            if (File.Exists(path))
            {
                foreach (var line in File.ReadLines(path))
                {
                    var idx = line.IndexOf('=');
                    if (idx <= 0)
                    {
                        lines.Add(line);
                        continue;
                    }
                    var field = line.Substring(0, idx).Trim();
                    if (string.Equals(field, key, StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add($"{key}={_raceTime}");
                        found = true;
                    }
                    else
                    {
                        lines.Add(line);
                    }
                }
            }

            if (!found)
                lines.Add($"{key}={_raceTime}");

            File.WriteAllLines(path, lines);
        }

        private AudioSourceHandle LoadCustomSound(string fileName)
        {
            var path = Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
                return LoadLegacySound("error.wav");
            return _audio.CreateSource(path, streamFromDisk: true);
        }
    }
}
