using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Tracks.Map;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal abstract class Level : IDisposable
    {
        protected const int MaxLaps = 16;
        protected const int MaxUnkeys = 12;
        protected const int RandomSoundGroups = 16;
        protected const int RandomSoundMax = 32;
        protected const float AdventureLaneWidth = 80.0f;
        private const float KmToMiles = 0.621371f;
        private const float MetersPerMile = 1609.344f;
        private const float MetersToFeet = 3.28084f;

        public enum RandomSound
        {
            EasyLeft = 0,
            Left = 1,
            HardLeft = 2,
            HairpinLeft = 3,
            EasyRight = 4,
            Right = 5,
            HardRight = 6,
            HairpinRight = 7,
            Asphalt = 8,
            Gravel = 9,
            Water = 10,
            Sand = 11,
            Snow = 12,
            Finish = 13,
            Front = 14,
            Tail = 15
        }

        protected readonly AudioManager _audio;
        protected readonly SpeechService _speech;
        protected readonly RaceSettings _settings;
        protected readonly RaceInput _input;
        protected readonly IVibrationDevice? _vibrationDevice;
        protected readonly MapTrack _track;
        protected readonly Car _car;
        protected readonly List<RaceEvent> _events;
        protected readonly Stopwatch _stopwatch;
        protected readonly AudioSourceHandle[] _soundNumbers;
        protected readonly AudioSourceHandle?[][] _randomSounds;
        protected readonly int[] _totalRandomSounds;
        private readonly SoundQueue _soundQueue;
        private readonly List<RaceEvent> _dueEvents;
        private long _eventSequence;

        protected bool _manualTransmission;
        protected int _nrOfLaps;
        protected int _lap;
        protected float _elapsedTotal;
        protected int _raceTime;
        protected int _highscore;
        protected bool _started;
        protected bool _finished;
        protected bool _engineStarted;
        protected bool _acceptPlayerInfo;
        protected bool _acceptCurrentRaceInfo;
        protected float _sayTimeLength;
        protected float _speakTime;
        protected int _unkeyQueue;
        protected TrackRoad _currentRoad;
        protected long _oldStopwatchMs;
        protected long _stopwatchDiffMs;
        private Vector3 _lastListenerPosition;
        private bool _listenerInitialized;

        protected AudioSourceHandle _soundStart;
        protected AudioSourceHandle[] _soundLaps;
        protected AudioSourceHandle _soundBestTime;
        protected AudioSourceHandle _soundNewTime;
        protected AudioSourceHandle _soundYourTime;
        protected AudioSourceHandle _soundMinute;
        protected AudioSourceHandle _soundMinutes;
        protected AudioSourceHandle _soundSecond;
        protected AudioSourceHandle _soundSeconds;
        protected AudioSourceHandle _soundPoint;
        protected AudioSourceHandle _soundPercent;
        protected AudioSourceHandle[] _soundUnkey;
        protected AudioSourceHandle? _soundTheme4;
        protected AudioSourceHandle? _soundPause;
        protected AudioSourceHandle? _soundUnpause;
        protected AudioSourceHandle? _soundTrackName;

        protected bool ExitRequested { get; set; }
        protected bool PauseRequested { get; set; }
        private bool _exitWhenQueueIdle;

        protected Level(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
            : this(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, null, userDefined: false)
        {
        }

        protected Level(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            TrackData? trackData,
            bool userDefined)
        {
            _audio = audio;
            _speech = speech;
            _settings = settings;
            _input = input;
            _vibrationDevice = vibrationDevice;
            _events = new List<RaceEvent>();
            _stopwatch = new Stopwatch();
            _soundQueue = new SoundQueue();
            _dueEvents = new List<RaceEvent>();

            _manualTransmission = !automaticTransmission;
            _nrOfLaps = nrOfLaps;
            _lap = 0;
            _speakTime = 0.0f;
            _unkeyQueue = 0;
            _highscore = 0;
            _sayTimeLength = 0.0f;
            _acceptPlayerInfo = true;
            _acceptCurrentRaceInfo = true;

            _track = MapTrack.Load(track, audio);
            _car = new Car(audio, _track, input, settings, vehicle, vehicleFile, () => _elapsedTotal, () => _started, _vibrationDevice);

            if (!string.IsNullOrWhiteSpace(track) &&
                track.IndexOf("adv", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _track.SetLaneWidth(AdventureLaneWidth);
                _nrOfLaps = 1;
            }

            _soundNumbers = new AudioSourceHandle[101];
            for (var i = 0; i <= 100; i++)
            {
                _soundNumbers[i] = LoadLanguageSound($"numbers\\{i}");
            }

            _soundStart = LoadLanguageSound("race\\start321");
            _soundBestTime = LoadLanguageSound("race\\time\\trackrecord");
            _soundNewTime = LoadLanguageSound("race\\time\\newrecord");
            _soundYourTime = LoadLanguageSound("race\\time\\yourtime");
            _soundMinute = LoadLanguageSound("race\\time\\minute");
            _soundMinutes = LoadLanguageSound("race\\time\\minutes");
            _soundSecond = LoadLanguageSound("race\\time\\second");
            _soundSeconds = LoadLanguageSound("race\\time\\seconds");
            _soundPoint = LoadLanguageSound("race\\time\\point");
            _soundPercent = LoadLanguageSound("race\\time\\percent");

            _soundUnkey = new AudioSourceHandle[MaxUnkeys];
            for (var i = 0; i < MaxUnkeys; i++)
            {
                var file = $"unkey{i + 1}.wav";
                _soundUnkey[i] = LoadLegacySound(file);
            }

            _randomSounds = new AudioSourceHandle?[RandomSoundGroups][];
            _totalRandomSounds = new int[RandomSoundGroups];
            for (var i = 0; i < RandomSoundGroups; i++)
                _randomSounds[i] = new AudioSourceHandle?[RandomSoundMax];

            LoadRandomSounds(RandomSound.EasyLeft, "race\\copilot\\easyleft");
            LoadRandomSounds(RandomSound.Left, "race\\copilot\\left");
            LoadRandomSounds(RandomSound.HardLeft, "race\\copilot\\hardleft");
            LoadRandomSounds(RandomSound.HairpinLeft, "race\\copilot\\hairpinleft");
            LoadRandomSounds(RandomSound.EasyRight, "race\\copilot\\easyright");
            LoadRandomSounds(RandomSound.Right, "race\\copilot\\right");
            LoadRandomSounds(RandomSound.HardRight, "race\\copilot\\hardright");
            LoadRandomSounds(RandomSound.HairpinRight, "race\\copilot\\hairpinright");
            LoadRandomSounds(RandomSound.Asphalt, "race\\copilot\\asphalt");
            LoadRandomSounds(RandomSound.Gravel, "race\\copilot\\gravel");
            LoadRandomSounds(RandomSound.Water, "race\\copilot\\water");
            LoadRandomSounds(RandomSound.Sand, "race\\copilot\\sand");
            LoadRandomSounds(RandomSound.Snow, "race\\copilot\\snow");
            LoadRandomSounds(RandomSound.Finish, "race\\info\\finish");

            _soundLaps = new AudioSourceHandle[MaxLaps - 1];
            for (var i = 0; i < MaxLaps - 1; i++)
            {
                _soundLaps[i] = LoadLanguageSound($"race\\info\\laps2go{i + 1}");
            }

            _soundTrackName = LoadTrackNameSound(_track.TrackName);
        }

        public bool Started => _started;
        public bool ManualTransmission => _manualTransmission;
        public bool WantsExit => ExitRequested;
        public bool WantsPause => PauseRequested;

        public void ClearPauseRequest()
        {
            PauseRequested = false;
        }

        public void StartStopwatchDiff()
        {
            _oldStopwatchMs = _stopwatch.ElapsedMilliseconds;
        }

        public void StopStopwatchDiff()
        {
            var now = _stopwatch.ElapsedMilliseconds;
            _stopwatchDiffMs += (now - _oldStopwatchMs);
        }

        protected void InitializeLevel()
        {
            _track.Initialize();
            _car.Initialize();
            _elapsedTotal = 0.0f;
            _oldStopwatchMs = 0;
            _stopwatchDiffMs = 0;
            _started = false;
            _finished = false;
            _engineStarted = false;
            _currentRoad.Surface = _track.InitialSurface;
            _car.ManualTransmission = _manualTransmission;
            _listenerInitialized = false;
            _lastListenerPosition = Vector3.Zero;
        }

        protected void FinalizeLevel()
        {
            _car.FinalizeCar();
            _track.FinalizeTrack();
        }

        protected void RequestExitWhenQueueIdle()
        {
            _exitWhenQueueIdle = true;
        }

        protected bool UpdateExitWhenQueueIdle()
        {
            if (!_exitWhenQueueIdle)
                return false;
            if (!_soundQueue.IsIdle)
                return false;
            ExitRequested = true;
            return true;
        }

        protected void HandleEngineStartRequest()
        {
            if (_input.GetStartEngine() && _started && !_finished)
            {
                var canStart = !_engineStarted || _car.State == CarState.Crashed;
                if (canStart)
                {
                    _engineStarted = true;
                    if (_car.State == CarState.Crashed)
                        _car.RestartAfterCrash();
                    else
                        _car.Start();
                }
            }
        }

        protected void HandleCurrentGearRequest()
        {
            if (_input.GetCurrentGear() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var gear = _car.Gear;
                SpeakText($"Gear {gear}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentLapNumberRequest()
        {
            if (_input.GetCurrentLapNr() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText($"Lap {_lap}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentRacePercentageRequest()
        {
            if (_input.GetCurrentRacePerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = (_car.DistanceMeters / (float)(_track.Length * _nrOfLaps)) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Race percentage", units));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentLapPercentageRequest()
        {
            if (_input.GetCurrentLapPerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = ((_car.DistanceMeters - (_track.Length * (_lap - 1))) / _track.Length) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Lap percentage", units));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentRaceTimeRequestActiveOnly()
        {
            if (_input.GetCurrentRaceTime() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var text = FormatTimeText((int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs), detailed: false);
                SpeakText($"Race time {text}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentRaceTimeRequestWithFinish()
        {
            if (_input.GetCurrentRaceTime() && _started && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                var timeMs = _lap <= _nrOfLaps
                    ? (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs)
                    : _raceTime;
                var text = FormatTimeText(timeMs, detailed: false);
                SpeakText($"Race time {text}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleTrackNameRequest()
        {
            if (_input.GetTrackName() && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText(FormatTrackName(_track.TrackName));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleSpeedReportRequest()
        {
            if (_input.GetSpeedReport() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var speedKmh = _car.SpeedKmh;
                var rpm = _car.EngineRpm;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var speedMph = speedKmh * KmToMiles;
                    SpeakText($"{speedMph:F0} miles per hour, {rpm:F0} RPM");
                }
                else
                {
                    SpeakText($"{speedKmh:F0} kilometers per hour, {rpm:F0} RPM");
                }
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleDistanceReportRequest()
        {
            if (_input.GetDistanceReport() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var distanceM = _car.DistanceMeters;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var distanceMiles = distanceM / MetersPerMile;
                    if (distanceMiles >= 1f)
                        SpeakText($"{distanceMiles:F1} miles traveled");
                    else
                        SpeakText($"{distanceM * MetersToFeet:F0} feet traveled");
                }
                else
                {
                    var distanceKm = distanceM / 1000f;
                    if (distanceKm >= 1f)
                        SpeakText($"{distanceKm:F1} kilometers traveled");
                    else
                        SpeakText($"{distanceM:F0} meters traveled");
                }
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleWheelAngleReportRequest()
        {
            if (_input.GetWheelAngleReport() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText($"Wheel angle {_car.FrontWheelAngleDegrees:F1} degrees");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleHeadingReportRequest()
        {
            if (_input.GetHeadingReport() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var headingText = CompassHeading.FormatHeading(_car.HeadingDegrees);
                SpeakText($"Heading {headingText}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandlePauseRequest(ref bool pauseKeyReleased)
        {
            if (!_input.GetPause() && !pauseKeyReleased)
            {
                pauseKeyReleased = true;
            }
            else if (_input.GetPause() && pauseKeyReleased && _started && _lap <= _nrOfLaps && _car.State == CarState.Running)
            {
                pauseKeyReleased = false;
                PauseRequested = true;
            }
        }

        protected void SayTime(int raceTime, bool detailed = true)
        {
            var minutes = raceTime / 60000;
            var seconds = (raceTime % 60000) / 1000;

            if (minutes != 0)
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[minutes]);
                _sayTimeLength += _soundNumbers[minutes].GetLengthSeconds();
                if (minutes == 1)
                {
                    PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundMinute);
                    _sayTimeLength += _soundMinute.GetLengthSeconds();
                }
                else
                {
                    PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundMinutes);
                    _sayTimeLength += _soundMinutes.GetLengthSeconds();
                }
            }

            PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[seconds]);
            _sayTimeLength += _soundNumbers[seconds].GetLengthSeconds();

            if (detailed)
            {
                var tens = ((raceTime % 60000) / 100) % 10;
                var hundreds = ((raceTime % 60000) / 10) % 10;
                var thousands = (raceTime % 60000) % 10;

                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundPoint);
                _sayTimeLength += _soundPoint.GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[tens]);
                _sayTimeLength += _soundNumbers[tens].GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[hundreds]);
                _sayTimeLength += _soundNumbers[hundreds].GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[thousands]);
                _sayTimeLength += _soundNumbers[thousands].GetLengthSeconds();
            }

            if (!detailed && seconds == 1)
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundSecond);
                _sayTimeLength += _soundSecond.GetLengthSeconds();
            }
            else
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundSeconds);
                _sayTimeLength += _soundSeconds.GetLengthSeconds();
            }
        }

        protected void CallNextRoad(TrackRoad nextRoad)
        {
            if ((int)_settings.Copilot > 0 && nextRoad.Type != TrackType.Straight)
            {
                var index = (int)nextRoad.Type - 1;
                if (index >= 0 && index < RandomSoundGroups && _totalRandomSounds[index] > 0)
                {
                    var sound = _randomSounds[index][Algorithm.RandomInt(_totalRandomSounds[index])];
                    QueueSound(sound);
                }
            }

            if ((int)_settings.Copilot > 1 && nextRoad.Surface != _currentRoad.Surface)
            {
                var index = (int)nextRoad.Surface + 8;
                if (index >= 0 && index < RandomSoundGroups && _totalRandomSounds[index] > 0)
                {
                    var sound = _randomSounds[index][Algorithm.RandomInt(_totalRandomSounds[index])];
                    PushEvent(RaceEventType.PlaySound, 1.0f, sound);
                }
            }

            _currentRoad = nextRoad;
        }

        protected void PushEvent(RaceEventType type, float time, AudioSourceHandle? sound = null)
        {
            _events.Add(new RaceEvent
            {
                Type = type,
                Time = _elapsedTotal + time,
                Sound = sound,
                Sequence = _eventSequence++
            });
        }

        protected List<RaceEvent> CollectDueEvents()
        {
            _dueEvents.Clear();
            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time <= _elapsedTotal)
                {
                    _events.RemoveAt(i);
                    _dueEvents.Add(e);
                }
            }

            if (_dueEvents.Count > 1)
            {
                _dueEvents.Sort((a, b) =>
                {
                    var timeCompare = a.Time.CompareTo(b.Time);
                    return timeCompare != 0 ? timeCompare : a.Sequence.CompareTo(b.Sequence);
                });
            }

            return _dueEvents;
        }

        protected void Speak(AudioSourceHandle sound, bool unKey = false)       
        {
            if (sound == null)
                return;

            var length = Math.Max(0.05f, sound.GetLengthSeconds());
            _speakTime = Math.Max(_speakTime, _elapsedTotal) + length;
            QueueSound(sound);

            if (unKey)
            {
                _unkeyQueue++;
                PushEvent(RaceEventType.PlayRadioSound, length);
            }
        }

        protected void SpeakText(string text)
        {
            _speech.Speak(text);
        }

        protected void UpdateAudioListener(float elapsed)
        {
            var forward = _car.WorldForward;
            var up = _car.WorldUp;
            if (forward.LengthSquared() < 0.0001f)
                forward = new Vector3(0f, 0f, 1f);
            if (up.LengthSquared() < 0.0001f)
                up = Vector3.UnitY;
            forward = Vector3.Normalize(forward);
            up = Vector3.Normalize(up);
            var right = Vector3.Cross(up, forward);
            if (right.LengthSquared() < 0.0001f)
                right = Vector3.UnitX;
            else
                right = Vector3.Normalize(right);

            var driverOffsetX = -_car.WidthM * 0.25f;
            var driverOffsetZ = _car.LengthM * 0.1f;
            var worldPosition = _car.WorldPosition + (right * driverOffsetX) + (forward * driverOffsetZ);

            var worldVelocity = _car.WorldVelocity;
            if (_listenerInitialized && elapsed > 0f)
            {
                worldVelocity = (worldPosition - _lastListenerPosition) / elapsed;
            }
            _lastListenerPosition = worldPosition;
            _listenerInitialized = true;

            var position = AudioWorld.ToMeters(worldPosition);
            var velocity = AudioWorld.ToMeters(worldVelocity);
            _audio.UpdateListener(position, forward, up, velocity);
        }

        protected float GetRelativeTrackDelta(float otherDistanceMeters)
        {
            return otherDistanceMeters - _car.DistanceMeters;
        }

        protected static string FormatTimeText(int raceTimeMs, bool detailed)
        {
            if (raceTimeMs < 0)
                raceTimeMs = 0;
            var minutes = raceTimeMs / 60000;
            var seconds = (raceTimeMs % 60000) / 1000;
            var parts = new List<string>();
            if (minutes > 0)
                parts.Add($"{minutes} {(minutes == 1 ? "minute" : "minutes")}");
            parts.Add($"{seconds} {(seconds == 1 ? "second" : "seconds")}");
            if (detailed)
            {
                var millis = raceTimeMs % 1000;
                parts.Add($"point {millis:D3}");
            }
            return string.Join(" ", parts);
        }

        protected static string FormatPercentageText(string label, int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            return string.IsNullOrWhiteSpace(label)
                ? $"{clamped} percent"
                : $"{label} {clamped} percent";
        }

        protected static string FormatVehicleName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Vehicle";
            return name!.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        protected static string FormatTrackName(string trackName)
        {
            if (TrackList.TryGetDisplayName(trackName, out var display))
                return display;

            if (string.Equals(trackName, "custom", StringComparison.OrdinalIgnoreCase))
                return "Custom track";

            var baseName = trackName;
            if (trackName.IndexOfAny(new[] { '\\', '/' }) >= 0)
                baseName = Path.GetFileNameWithoutExtension(trackName) ?? trackName;
            else if (trackName.Length > 4)
                baseName = Path.GetFileNameWithoutExtension(trackName) ?? trackName;
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "Track";
            return FormatVehicleName(baseName);
        }

        protected void LoadRandomSounds(RandomSound pos, string baseName)
        {
            var first = $"{baseName}1";
            _randomSounds[(int)pos][0] = LoadLanguageSound(first);
            _totalRandomSounds[(int)pos] = 1;

            for (var i = 1; i < RandomSoundMax; i++)
            {
                var name = $"{baseName}{i + 1}";
                var sound = TryLoadLanguageSound(name, allowFallback: false);
                _randomSounds[(int)pos][i] = sound;
                if (sound == null)
                {
                    _totalRandomSounds[(int)pos] = i;
                    break;
                }
            }
        }

        protected void FlushPendingSounds()
        {
            for (var i = _events.Count - 1; i >= 0; i--)
            {
                if (_events[i].Sound != null)
                {
                    _events.RemoveAt(i);
                }
            }
            _soundQueue.Clear();
        }

        protected void FadeIn()
        {
            if (_soundTheme4 == null)
                return;
            var target = (int)Math.Round(_settings.MusicVolume * 100f);
            var volume = 0;
            _soundTheme4.SetVolumePercent(volume);
            for (var i = 0; i < 10; i++)
            {
                volume = Math.Min(target, volume + Math.Max(1, target / 10));
                _soundTheme4.SetVolumePercent(volume);
                Thread.Sleep(25);
            }
        }

        protected void FadeOut()
        {
            if (_soundTheme4 == null)
                return;
            var volume = (int)Math.Round(_settings.MusicVolume * 100f);
            for (var i = 0; i < 10; i++)
            {
                volume = Math.Max(0, volume - Math.Max(1, volume / 10));
                _soundTheme4.SetVolumePercent(volume);
                Thread.Sleep(25);
            }
        }

        protected AudioSourceHandle LoadLanguageSound(string key, bool streamFromDisk = true)
        {
            var sound = TryLoadLanguageSound(key, allowFallback: true, streamFromDisk: streamFromDisk);
            if (sound != null)
                return sound;
            var errorPath = GetLegacySoundPath("error.wav");
            if (errorPath != null)
                return _audio.CreateSource(errorPath, streamFromDisk: true);
            throw new FileNotFoundException($"Missing language sound {key}.");
        }

        protected AudioSourceHandle? TryLoadLanguageSound(string key, bool allowFallback, bool streamFromDisk = true)
        {
            var path = ResolveLanguageSoundPath(_settings.Language, key);
            if (path != null)
                return streamFromDisk
                    ? _audio.CreateSource(path, streamFromDisk: true)
                    : _audio.CreateLoopingSource(path);

            if (allowFallback && !string.Equals(_settings.Language, "en", StringComparison.OrdinalIgnoreCase))
            {
                path = ResolveLanguageSoundPath("en", key);
                if (path != null)
                    return streamFromDisk
                        ? _audio.CreateSource(path, streamFromDisk: true)
                        : _audio.CreateLoopingSource(path);
            }
            return null;
        }

        protected AudioSourceHandle LoadLegacySound(string fileName)
        {
            var path = GetLegacySoundPath(fileName);
            if (path == null)
                throw new FileNotFoundException($"Missing legacy sound {fileName}.");
            return _audio.CreateSource(path, streamFromDisk: true);
        }

        private string? ResolveLanguageSoundPath(string language, string key)
        {
            var relative = key.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(Path.GetExtension(relative)))
                relative += ".ogg";
            var path = Path.Combine(AssetPaths.SoundsRoot, language, relative);
            return File.Exists(path) ? path : null;
        }

        private string? GetLegacySoundPath(string fileName)
        {
            var path = Path.Combine(AssetPaths.SoundsRoot, "Legacy", fileName);
            return File.Exists(path) ? path : null;
        }

        private AudioSourceHandle? LoadTrackNameSound(string trackName)
        {
            switch (trackName)
            {
                case "america":
                    return LoadLanguageSound("tracks\\america");
                case "austria":
                    return LoadLanguageSound("tracks\\austria");
                case "belgium":
                    return LoadLanguageSound("tracks\\belgium");
                case "brazil":
                    return LoadLanguageSound("tracks\\brazil");
                case "china":
                    return LoadLanguageSound("tracks\\china");
                case "england":
                    return LoadLanguageSound("tracks\\england");
                case "finland":
                    return LoadLanguageSound("tracks\\finland");
                case "france":
                    return LoadLanguageSound("tracks\\france");
                case "germany":
                    return LoadLanguageSound("tracks\\germany");
                case "ireland":
                    return LoadLanguageSound("tracks\\ireland");
                case "italy":
                    return LoadLanguageSound("tracks\\italy");
                case "netherlands":
                    return LoadLanguageSound("tracks\\netherlands");
                case "portugal":
                    return LoadLanguageSound("tracks\\portugal");
                case "russia":
                    return LoadLanguageSound("tracks\\russia");
                case "spain":
                    return LoadLanguageSound("tracks\\spain");
                case "sweden":
                    return LoadLanguageSound("tracks\\sweden");
                case "switserland":
                    return LoadLanguageSound("tracks\\switserland");
                case "advHills":
                    return LoadLanguageSound("tracks\\rallyhills");
                case "advCoast":
                    return LoadLanguageSound("tracks\\frenchcoast");
                case "advCountry":
                    return LoadLanguageSound("tracks\\englishcountry");
                case "advAirport":
                    return LoadLanguageSound("tracks\\rideairport");
                case "advDesert":
                    return LoadLanguageSound("tracks\\rallydesert");
                case "advRush":
                    return LoadLanguageSound("tracks\\rushhour");
                case "advEscape":
                    return LoadLanguageSound("tracks\\polarescape");
                case "custom":
                    return LoadLanguageSound("menu\\customtrack");
            }

            var baseName = trackName;
            var directory = string.Empty;
            if (trackName.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                directory = Path.GetDirectoryName(trackName) ?? string.Empty;
                baseName = Path.GetFileNameWithoutExtension(trackName);
            }
            else if (trackName.Length > 4)
            {
                baseName = trackName.Substring(0, trackName.Length - 4);
            }

            if (!baseName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                baseName += ".wav";

            var candidate = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(AppContext.BaseDirectory, baseName)
                : Path.Combine(directory, baseName);
            if (File.Exists(candidate))
                return _audio.CreateSource(candidate, streamFromDisk: true);

            var fallback = GetLegacySoundPath("error.wav");
            return fallback != null ? _audio.CreateSource(fallback, streamFromDisk: true) : null;
        }

        public void Dispose()
        {
            _soundQueue.Clear();
            _car.Dispose();
            _track.Dispose();
            DisposeSound(_soundStart);
            DisposeSound(_soundBestTime);
            DisposeSound(_soundNewTime);
            DisposeSound(_soundYourTime);
            DisposeSound(_soundMinute);
            DisposeSound(_soundMinutes);
            DisposeSound(_soundSecond);
            DisposeSound(_soundSeconds);
            DisposeSound(_soundPoint);
            DisposeSound(_soundPercent);
            DisposeSound(_soundTheme4);
            DisposeSound(_soundPause);
            DisposeSound(_soundUnpause);
            DisposeSound(_soundTrackName);

            for (var i = 0; i < _soundNumbers.Length; i++)
                DisposeSound(_soundNumbers[i]);

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

        protected static void DisposeSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.Dispose();
        }

        protected void QueueSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            _soundQueue.Enqueue(sound);
        }

        protected sealed class RaceEvent
        {
            public RaceEventType Type { get; set; }
            public float Time { get; set; }
            public AudioSourceHandle? Sound { get; set; }
            public long Sequence { get; set; }
        }

        protected enum RaceEventType
        {
            CarStart,
            RaceStart,
            RaceFinish,
            PlaySound,
            PlayRadioSound,
            RaceTimeFinalize,
            AcceptPlayerInfo,
            AcceptCurrentRaceInfo
        }

        private sealed class SoundQueue
        {
            private readonly Queue<AudioSourceHandle> _queue = new Queue<AudioSourceHandle>();
            private readonly object _lock = new object();
            private AudioSourceHandle? _current;

            public void Enqueue(AudioSourceHandle sound)
            {
                lock (_lock)
                {
                    _queue.Enqueue(sound);
                    if (_current == null)
                        PlayNextLocked();
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    _queue.Clear();
                    _current = null;
                }
            }

            public bool IsIdle
            {
                get
                {
                    lock (_lock)
                        return _current == null && _queue.Count == 0;
                }
            }

            private void PlayNextLocked()
            {
                if (_queue.Count == 0)
                {
                    _current = null;
                    return;
                }

                var next = _queue.Dequeue();
                _current = next;
                next.Stop();
                next.SeekToStart();
                next.SetOnEnd(() => OnEnd(next));
                next.Play(loop: false);
            }

            private void OnEnd(AudioSourceHandle finished)
            {
                lock (_lock)
                {
                    if (!ReferenceEquals(_current, finished))
                        return;
                    _current = null;
                    PlayNextLocked();
                }
            }
        }
    }
}
