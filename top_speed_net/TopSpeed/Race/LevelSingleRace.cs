using System;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed class LevelSingleRace : Level
    {
        private const int MaxComputerPlayers = 7;
        private const int MaxPlayers = 8;
        private const float StartLineY = 140.0f;

        private readonly ComputerPlayer?[] _computerPlayers;
        private readonly AudioSourceHandle?[] _soundPosition;
        private readonly AudioSourceHandle?[] _soundPlayerNr;
        private readonly AudioSourceHandle?[] _soundFinished;

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
        private float _raceStartDelay;
        private bool _botsScheduled;

        public LevelSingleRace(
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
            : base(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice)
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
        }

        public void Initialize(int playerNumber)
        {
            InitializeLevel();
            _playerNumber = playerNumber;
            _position = playerNumber + 1;
            _positionComment = playerNumber + 1;
            _raceStartDelay = 6.5f;
            _botsScheduled = false;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var botNumber = i;
                if (botNumber >= _playerNumber)
                    botNumber++;
                _computerPlayers[i] = GenerateRandomPlayer(botNumber);
            }

            var maxLength = _car.LengthM;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot != null && bot.LengthM > maxLength)
                    maxLength = bot.LengthM;
            }

            var rowSpacing = Math.Max(10.0f, maxLength * 1.5f);
            var playerX = CalculateStartX(_playerNumber, _car.WidthM);
            var playerY = CalculateStartY(_playerNumber, rowSpacing);
            _car.SetPosition(playerX, playerY);

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                var botX = CalculateStartX(bot.PlayerNumber, bot.WidthM);
                var botY = CalculateStartY(bot.PlayerNumber, rowSpacing);
                bot.Initialize(botX, botY, _track.Length);
            }

            for (var i = 0; i <= _nComputerPlayers; i++)
            {
                _soundPlayerNr[i] = LoadLanguageSound($"race\\info\\player{i + 1}");

                var positionIndex = i == _nComputerPlayers ? MaxPlayers : i + 1;
                _soundPosition[i] = LoadLanguageSound($"race\\info\\youarepos{positionIndex}");
                _soundFinished[i] = LoadLanguageSound($"race\\info\\finished{positionIndex}");
            }

            LoadRandomSounds(RandomSound.Front, "race\\info\\front");
            LoadRandomSounds(RandomSound.Tail, "race\\info\\tail");

            _soundYouAre = LoadLanguageSound("race\\youare");
            _soundPlayer = LoadLanguageSound("race\\player");
            _soundTheme4 = LoadLanguageSound("music\\theme4", streamFromDisk: false);
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
            }

            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            FinalizeLevel();
        }

        public void Run(float elapsed)
        {
            if (_elapsedTotal == 0.0f)
            {
                var countdownLength = _soundStart.GetLengthSeconds();
                var countdownTotal = 1.5f + Math.Max(0f, countdownLength);
                _raceStartDelay = Math.Max(6.5f, countdownTotal);
                PushEvent(RaceEventType.CarStart, 3.0f);
                PushEvent(RaceEventType.RaceStart, _raceStartDelay);
                PushEvent(RaceEventType.PlaySound, 1.5f, _soundStart);
            }

            var dueEvents = CollectDueEvents();
            foreach (var e in dueEvents)
            {
                switch (e.Type)
                {
                    case RaceEventType.CarStart:
                        // Player car start is now manual via Enter key
                        break;
                    case RaceEventType.RaceStart:
                        _raceTime = 0;
                        _stopwatch.Restart();
                        _lap = 0;
                        _started = true;
                        if (!_botsScheduled)
                        {
                            for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
                                _computerPlayers[botIndex]?.PendingStart(0.0f);
                            _botsScheduled = true;
                        }
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
                        RequestExitWhenQueueIdle();
                        break;
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
            UpdateAudioListener(elapsed);
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

            // Allow starting engine initially or restarting after crash
            HandleEngineStartRequest();

            HandleCurrentGearRequest();
            HandleCurrentLapNumberRequest();
            HandleCurrentRacePercentageRequest();
            HandleCurrentLapPercentageRequest();
            HandleCurrentRaceTimeRequestWithFinish();

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
                SpeakText(GetVehicleNameForPlayer(infoPlayer));
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            if (_input.TryGetPlayerPosition(out var positionPlayer) && _acceptPlayerInfo && positionPlayer <= _nComputerPlayers && _started)
            {
                _acceptPlayerInfo = false;
                var perc = CalculatePlayerPerc(positionPlayer);
                SpeakText(FormatPercentageText(string.Empty, perc));
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            HandleTrackNameRequest();

            if (_input.GetPlayerNumber() && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                QueueSound(_soundNumbers[_playerNumber + 1]);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundNumbers[_playerNumber + 1].GetLengthSeconds());
            }

            HandleSpeedReportRequest();
            HandleDistanceReportRequest();
            HandlePauseRequest(ref _pauseKeyReleased);

            if (UpdateExitWhenQueueIdle())
                return;

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
            return new ComputerPlayer(
                _audio,
                _track,
                _settings,
                vehicleIndex,
                playerNumber,
                () => _elapsedTotal,
                () => _started,
                null);
        }

        private float CalculateStartX(int gridIndex, float vehicleWidth)
        {
            var halfWidth = Math.Max(0.1f, vehicleWidth * 0.5f);
            var margin = 0.3f;
            var laneHalfWidth = _track.LaneWidth;
            var laneOffset = laneHalfWidth - halfWidth - margin;
            if (laneOffset < 0f)
                laneOffset = 0f;
            return gridIndex % 2 == 1 ? laneOffset : -laneOffset;
        }

        private float CalculateStartY(int gridIndex, float rowSpacing)
        {
            var row = gridIndex / 2;
            return StartLineY - (row * rowSpacing);
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
            var inFrontDist = 500.0f;
            var onTail = -1;
            var onTailDist = 500.0f;

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
                    var dx = _car.PositionX - bot.PositionX;
                    var dy = _car.PositionY - bot.PositionY;
                    var xThreshold = (_car.WidthM + bot.WidthM) * 0.5f;
                    var yThreshold = (_car.LengthM + bot.LengthM) * 0.5f;
                    if (Math.Abs(dx) < xThreshold && Math.Abs(dy) < yThreshold)
                    {
                        var bumpX = dx;
                        var bumpY = dy;
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

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == _playerNumber)
            {
                if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                    return FormatVehicleName(_car.CustomFile);
                return _car.VehicleName;
            }

            if (playerIndex < _playerNumber)
            {
                var bot = _computerPlayers[playerIndex];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }
            else if (playerIndex > _playerNumber)
            {
                var bot = _computerPlayers[playerIndex - 1];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }

            return "Vehicle";
        }
    }
}
