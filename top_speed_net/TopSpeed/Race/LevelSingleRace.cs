using System;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed class LevelSingleRace : Level
    {
        private const int MaxComputerPlayers = 7;
        private const int MaxPlayers = 8;

        private readonly ComputerPlayer?[] _computerPlayers;
        private readonly AudioSourceHandle?[] _soundPosition;
        private readonly AudioSourceHandle?[] _soundPlayerNr;
        private readonly AudioSourceHandle?[] _soundFinished;
        private readonly AudioSourceHandle?[] _soundVehicle;

        private AudioSourceHandle? _soundYouAre;
        private AudioSourceHandle? _soundPlayer;
        private float _lastComment;
        private bool _infoKeyReleased;
        private int _positionFinish;
        private int _position;
        private int _positionComment;
        private int _playerNumber;
        private int _nComputerPlayers;
        private bool _pauseKeyReleased = true;

        public LevelSingleRace(
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
            _nComputerPlayers = Math.Min(settings.NrOfComputers, MaxComputerPlayers);
            _playerNumber = 1;
            _lastComment = 0.0f;
            _infoKeyReleased = true;
            _positionFinish = 0;

            _computerPlayers = new ComputerPlayer?[MaxComputerPlayers];
            _soundPosition = new AudioSourceHandle?[MaxPlayers];
            _soundPlayerNr = new AudioSourceHandle?[MaxPlayers];
            _soundFinished = new AudioSourceHandle?[MaxPlayers];
            _soundVehicle = new AudioSourceHandle?[MaxPlayers];
        }

        public void Initialize(int playerNumber)
        {
            InitializeLevel();
            _playerNumber = playerNumber;
            _position = playerNumber + 1;
            _positionComment = playerNumber + 1;

            var positionX = playerNumber % 2 == 1 ? 3000 : -3000;
            var positionY = 14000 - playerNumber * 2000;
            _car.SetPosition(positionX, positionY);

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var botNumber = i;
                if (botNumber >= _playerNumber)
                    botNumber++;
                _computerPlayers[i] = GenerateRandomPlayer(botNumber);
                positionX = botNumber % 2 == 1 ? 3000 : -3000;
                positionY = 14000 - botNumber * 2000;
                _computerPlayers[i]!.Initialize(positionX, positionY, _track.Length);
            }

            for (var i = 0; i <= _nComputerPlayers; i++)
            {
                _soundPlayerNr[i] = LoadLanguageSound($"race\\info\\player{i + 1}");

                var positionIndex = i == _nComputerPlayers ? MaxPlayers : i + 1;
                _soundPosition[i] = LoadLanguageSound($"race\\info\\youarepos{positionIndex}");
                _soundFinished[i] = LoadLanguageSound($"race\\info\\finished{positionIndex}");

                if (i < _playerNumber && i < _nComputerPlayers)
                {
                    var index = _computerPlayers[i]!.VehicleIndex;
                    _soundVehicle[i] = LoadLanguageSound($"vehicles\\vehicle{index + 1}");
                }
                else if (i > _playerNumber && (i - 1) < _nComputerPlayers)
                {
                    var index = _computerPlayers[i - 1]!.VehicleIndex;
                    _soundVehicle[i] = LoadLanguageSound($"vehicles\\vehicle{index + 1}");
                }
                else if (i == _playerNumber)
                {
                    if (!_car.UserDefined)
                    {
                        _soundVehicle[i] = LoadLanguageSound($"vehicles\\vehicle{(int)_car.CarType + 1}");
                    }
                    else if (!string.IsNullOrWhiteSpace(_car.CustomFile))
                    {
                        var file = _car.CustomFile!;
                        if (!file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                            file += ".wav";
                        _soundVehicle[i] = LoadCustomSound(file);
                    }
                }
            }

            LoadRandomSounds(RandomSound.Front, "race\\info\\front");
            LoadRandomSounds(RandomSound.Tail, "race\\info\\tail");

            _soundYouAre = LoadLanguageSound("race\\youare");
            _soundPlayer = LoadLanguageSound("race\\player");
            _soundTheme4 = LoadLanguageSound("music\\theme4");
            _soundPause = LoadLanguageSound("race\\pause");
            _soundUnpause = LoadLanguageSound("race\\unpause");
            _soundTheme4.SetVolumePercent(50);

            Speak(_soundYouAre);
            Speak(_soundPlayer);
            Speak(_soundNumbers[_playerNumber + 1]);
        }

        public void FinalizeLevelSingleRace()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                _computerPlayers[i]?.FinalizePlayer();
                _computerPlayers[i]?.Dispose();
            }

            for (var i = 0; i <= _nComputerPlayers; i++)
            {
                DisposeSound(_soundPosition[i]);
                DisposeSound(_soundPlayerNr[i]);
                DisposeSound(_soundFinished[i]);
                DisposeSound(_soundVehicle[i]);
            }

            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            FinalizeLevel();
        }

        public void Run(float elapsed)
        {
            if (_elapsedTotal == 0.0f)
            {
                for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
                    _computerPlayers[botIndex]?.PendingStart();
                PushEvent(RaceEventType.CarStart, 3.0f);
                PushEvent(RaceEventType.RaceStart, 6.5f);
                PushEvent(RaceEventType.PlaySound, 1.5f, _soundStart);
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

            UpdatePositions();
            _car.Run(elapsed);
            _track.Run(_car.PositionY);

            for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
            {
                var bot = _computerPlayers[botIndex];
                if (bot == null)
                    continue;
                bot.Run(elapsed, _car.PositionX, _car.PositionY);
                if (_track.Lap(bot.PositionY) > _nrOfLaps && !bot.Finished)
                {
                    if (!_settings.ThreeDSound)
                        bot.Quiet();
                    bot.Stop();
                    bot.SetFinished(true);
                    Speak(_soundPlayerNr[bot.PlayerNumber]!, true);
                    Speak(_soundFinished[_positionFinish++]!, true);
                    if (CheckFinish())
                        PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                }
            }

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
                    Speak(_soundPlayerNr[_playerNumber]!, true);
                    Speak(_soundFinished[_positionFinish++]!, true);
                    if (CheckFinish())
                        PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                }
                else if (_settings.AutomaticInfo != AutomaticInfoMode.Off && _lap > 1 && _lap <= _nrOfLaps)
                {
                    Speak(_soundLaps[_nrOfLaps - _lap], true);
                }
            }

            CheckForBumps();

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

            if (_input.GetCurrentRaceTime() && _started && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                _sayTimeLength = 0.0f;
                if (_lap <= _nrOfLaps)
                    SayTime((int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs), detailed: false);
                else
                    SayTime(_raceTime, detailed: false);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _sayTimeLength);
            }

            _lastComment += elapsed;
            if (_settings.AutomaticInfo == AutomaticInfoMode.On && _lastComment > 6.0f)
            {
                Comment(automatic: true);
                _lastComment = 0.0f;
            }

            if (_input.GetRequestInfo() && _infoKeyReleased)
            {
                if (_lastComment > 2.0f)
                {
                    _infoKeyReleased = false;
                    Comment(automatic: false);
                    _lastComment = 0.0f;
                }
            }
            else if (!_input.GetRequestInfo() && !_infoKeyReleased)
            {
                _infoKeyReleased = true;
            }

            if (_input.TryGetPlayerInfo(out var infoPlayer) && _acceptPlayerInfo && infoPlayer <= _nComputerPlayers)
            {
                _acceptPlayerInfo = false;
                var sound = _soundVehicle[infoPlayer];
                if (sound != null)
                {
                    QueueSound(sound);
                    PushEvent(RaceEventType.AcceptPlayerInfo, sound.GetLengthSeconds());
                }
            }

            if (_input.TryGetPlayerPosition(out var positionPlayer) && _acceptPlayerInfo && positionPlayer <= _nComputerPlayers && _started)
            {
                _acceptPlayerInfo = false;
                var perc = CalculatePlayerPerc(positionPlayer);
                QueueSound(_soundNumbers[perc]);
                var time = _soundNumbers[perc].GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, time, _soundPercent);
                time += _soundPercent.GetLengthSeconds();
                PushEvent(RaceEventType.AcceptPlayerInfo, time);
            }

            if (_input.GetTrackName() && _acceptCurrentRaceInfo && _soundTrackName != null)
            {
                _acceptCurrentRaceInfo = false;
                QueueSound(_soundTrackName);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundTrackName.GetLengthSeconds());
            }

            if (_input.GetPlayerNumber() && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                QueueSound(_soundNumbers[_playerNumber + 1]);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundNumbers[_playerNumber + 1].GetLengthSeconds());
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
            _soundTheme4?.Play(loop: true);
            FadeIn();
            _car.Pause();
            for (var i = 0; i < _nComputerPlayers; i++)
                _computerPlayers[i]?.Pause();
            _soundPause?.Play(loop: false);
        }

        public void Unpause()
        {
            _car.Unpause();
            for (var i = 0; i < _nComputerPlayers; i++)
                _computerPlayers[i]?.Unpause();
            FadeOut();
            _soundTheme4?.Stop();
            _soundTheme4?.SeekToStart();
            _soundUnpause?.Play(loop: false);
        }

        private ComputerPlayer GenerateRandomPlayer(int playerNumber)
        {
            var vehicleIndex = Algorithm.RandomInt(VehicleCatalog.VehicleCount);
            return new ComputerPlayer(_audio, _track, _settings, vehicleIndex, playerNumber, () => _elapsedTotal, () => _started);
        }

        private void UpdatePositions()
        {
            _position = 1;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.PositionY > _car.PositionY)
                    _position++;
            }
        }

        private void Comment(bool automatic)
        {
            if (!_started || _lap > _nrOfLaps)
                return;

            var position = 1;
            var inFront = -1;
            var inFrontDist = 50000;
            var onTail = -1;
            var onTailDist = 50000;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;

                if (bot.PositionY > _car.PositionY)
                {
                    position++;
                    var dist = bot.PositionY - _car.PositionY;
                    if (dist < inFrontDist)
                    {
                        inFront = i;
                        inFrontDist = dist;
                    }
                }
                else if (bot.PositionY < _car.PositionY)
                {
                    var dist = _car.PositionY - bot.PositionY;
                    if (dist < onTailDist)
                    {
                        onTail = i;
                        onTailDist = dist;
                    }
                }
            }

            if (automatic && position != _positionComment)
            {
                if (position == _nComputerPlayers + 1)
                    Speak(_soundPosition[_nComputerPlayers]!, true);
                else
                    Speak(_soundPosition[position - 1]!, true);
                _positionComment = position;
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFront != -1)
                {
                    var bot = _computerPlayers[inFront]!;
                    Speak(_soundPlayerNr[bot.PlayerNumber]!, true);
                    var sound = _randomSounds[(int)RandomSound.Front][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Front])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }
            else
            {
                if (onTail != -1)
                {
                    var bot = _computerPlayers[onTail]!;
                    Speak(_soundPlayerNr[bot.PlayerNumber]!, true);
                    var sound = _randomSounds[(int)RandomSound.Tail][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Tail])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }

            if (inFront == -1 && onTail == -1 && !automatic)
            {
                if (position == _nComputerPlayers + 1)
                    Speak(_soundPosition[_nComputerPlayers]!, true);
                else
                    Speak(_soundPosition[position - 1]!, true);
                _positionComment = position;
            }
        }

        private void CheckForBumps()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                if (_car.State == CarState.Running && !bot.Finished)
                {
                    if (Math.Abs(_car.PositionX - bot.PositionX) < 1000 &&
                        Math.Abs(_car.PositionY - bot.PositionY) < 500)
                    {
                        var bumpX = _car.PositionX - bot.PositionX;
                        var bumpY = _car.PositionY - bot.PositionY;
                        var bumpSpeed = _car.Speed - bot.Speed;
                        _car.Bump(bumpX, bumpY, bumpSpeed);
                        bot.Bump(-bumpX, -bumpY, -bumpSpeed);
                    }
                }
            }
        }

        private bool CheckFinish()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.Finished == false)
                    return false;
            }
            if (_lap <= _nrOfLaps)
                return false;
            return true;
        }

        private int CalculatePlayerPerc(int player)
        {
            int perc;
            if (player == _playerNumber)
                perc = (int)((_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            else if (player > _playerNumber)
                perc = (int)((_computerPlayers[player - 1]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            else
                perc = (int)((_computerPlayers[player]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            if (perc > 100)
                perc = 100;
            return perc;
        }

        private AudioSourceHandle LoadCustomSound(string fileName)
        {
            var path = System.IO.Path.IsPathRooted(fileName)
                ? fileName
                : System.IO.Path.Combine(AppContext.BaseDirectory, fileName);
            if (!System.IO.File.Exists(path))
                return LoadLegacySound("error.wav");
            return _audio.CreateSource(path, streamFromDisk: true);
        }
    }
}
