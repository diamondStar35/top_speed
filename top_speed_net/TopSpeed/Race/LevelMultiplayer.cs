using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed class LevelMultiplayer : Level
    {
        private const int MaxPlayers = 8;
        private const float SendIntervalSeconds = 0.1f;

        private sealed class RemotePlayer
        {
            public RemotePlayer(ComputerPlayer player)
            {
                Player = player;
                State = PlayerState.NotReady;
                Finished = false;
            }

            public ComputerPlayer Player { get; }
            public PlayerState State { get; set; }
            public bool Finished { get; set; }
        }

        private readonly MultiplayerSession _session;
        private readonly uint _playerId;
        private readonly byte _playerNumber;
        private readonly Dictionary<byte, RemotePlayer> _remotePlayers;
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
        private bool _pauseKeyReleased = true;
        private float _sendAccumulator;
        private bool _sentStart;
        private bool _sentFinish;
        private PlayerState _currentState;

        public LevelMultiplayer(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            JoystickDevice? joystick,
            MultiplayerSession session,
            uint playerId,
            byte playerNumber)
            : base(audio, speech, settings, input, trackName, automaticTransmission, nrOfLaps, vehicle, vehicleFile, joystick, trackData, trackData.UserDefined)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _playerId = playerId;
            _playerNumber = playerNumber;
            _remotePlayers = new Dictionary<byte, RemotePlayer>();
            _soundPosition = new AudioSourceHandle?[MaxPlayers];
            _soundPlayerNr = new AudioSourceHandle?[MaxPlayers];
            _soundFinished = new AudioSourceHandle?[MaxPlayers];
            _currentState = PlayerState.NotReady;
        }

        public void Initialize()
        {
            InitializeLevel();
            _positionFinish = 0;
            _position = _playerNumber + 1;
            _positionComment = _position;
            _lastComment = 0.0f;
            _infoKeyReleased = true;
            _sendAccumulator = 0.0f;
            _sentStart = false;
            _sentFinish = false;

            var positionX = _playerNumber % 2 == 1 ? 30.0f : -30.0f;
            var positionY = 140.0f - _playerNumber * 20.0f;
            _car.SetPosition(positionX, positionY);

            for (var i = 0; i < MaxPlayers; i++)
            {
                _soundPlayerNr[i] = LoadLanguageSound($"race\\info\\player{i + 1}");
                _soundPosition[i] = LoadLanguageSound($"race\\info\\youarepos{i + 1}");
                _soundFinished[i] = LoadLanguageSound($"race\\info\\finished{i + 1}");
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

            _currentState = PlayerState.NotReady;
            _session.SendPlayerState(_currentState);
        }

        public void FinalizeLevelMultiplayer()
        {
            foreach (var remote in _remotePlayers.Values)
            {
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
            }
            _remotePlayers.Clear();

            for (var i = 0; i < _soundPosition.Length; i++)
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
                PushEvent(RaceEventType.CarStart, 3.0f);
                PushEvent(RaceEventType.RaceStart, 6.5f);
                PushEvent(RaceEventType.PlaySound, 1.5f, _soundStart);
            }

            var dueEvents = CollectDueEvents();
            foreach (var e in dueEvents)
            {
                switch (e.Type)
                {
                    case RaceEventType.CarStart:
                        // Manual start now
                        break;
                    case RaceEventType.RaceStart:
                        _raceTime = 0;
                        _stopwatch.Restart();
                        _lap = 0;
                        _started = true;
                        if (!_sentStart)
                        {
                            _sentStart = true;
                            _currentState = PlayerState.Racing;
                            _session.SendPlayerStarted();
                            _session.SendPlayerState(_currentState);
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
                    if (!_sentFinish)
                    {
                        _sentFinish = true;
                        _currentState = PlayerState.Finished;
                        _session.SendPlayerFinished();
                        _session.SendPlayerState(_currentState);
                    }
                    PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                }
                else if (_settings.AutomaticInfo != AutomaticInfoMode.Off && _lap > 1 && _lap <= _nrOfLaps)
                {
                    Speak(_soundLaps[_nrOfLaps - _lap], true);
                }
            }

            if (_input.GetStartEngine() && _started && !_engineStarted && !_finished)
            {
                _engineStarted = true;
                _car.Start();
            }

            if (_input.GetCurrentGear() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var gear = _car.Gear;
                SpeakText($"Gear {gear}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }

            if (_input.GetCurrentLapNr() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText($"Lap {_lap}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }

            if (_input.GetCurrentRacePerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = (_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Race percentage", units));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }

            if (_input.GetCurrentLapPerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = ((_car.PositionY - (_track.Length * (_lap - 1))) / (float)_track.Length) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Lap percentage", units));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }

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

            if (_input.TryGetPlayerInfo(out var infoPlayer) && _acceptPlayerInfo && infoPlayer >= 0 && infoPlayer < MaxPlayers)
            {
                _acceptPlayerInfo = false;
                SpeakText(GetVehicleNameForPlayer(infoPlayer));
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            if (_input.TryGetPlayerPosition(out var positionPlayer) && _acceptPlayerInfo && _started && positionPlayer >= 0 && positionPlayer < MaxPlayers)
            {
                _acceptPlayerInfo = false;
                var perc = CalculatePlayerPerc(positionPlayer);
                SpeakText(FormatPercentageText(string.Empty, perc));
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            if (_input.GetTrackName() && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText(FormatTrackName(_track.TrackName));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
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

            _sendAccumulator += elapsed;
            if (_sendAccumulator >= SendIntervalSeconds)
            {
                _sendAccumulator = 0.0f;
                var state = _currentState;
                if (_sentFinish)
                    state = PlayerState.Finished;
                else if (_started)
                    state = PlayerState.Racing;
                var raceData = new PlayerRaceData
                {
                    PositionX = _car.PositionX,
                    PositionY = _car.PositionY,
                    Speed = (ushort)_car.Speed,
                    Frequency = _car.Frequency
                };
                _session.SendPlayerData(raceData, _car.CarType, state, _car.EngineRunning, _car.Braking, _car.Horning, _car.Backfiring());
            }

            if (UpdateExitWhenQueueIdle())
                return;

            _elapsedTotal += elapsed;
        }

        public void Pause()
        {
            _soundTheme4?.Play(loop: true);
            FadeIn();
            _car.Pause();
            foreach (var remote in _remotePlayers.Values)
                remote.Player.Pause();
            _soundPause?.Play(loop: false);
        }

        public void Unpause()
        {
            _car.Unpause();
            foreach (var remote in _remotePlayers.Values)
                remote.Player.Unpause();
            FadeOut();
            _soundTheme4?.Stop();
            _soundTheme4?.SeekToStart();
            _soundUnpause?.Play(loop: false);
        }

        public void ApplyRemoteData(PacketPlayerData data)
        {
            if (data.PlayerNumber == _playerNumber)
                return;

            if (!_remotePlayers.TryGetValue(data.PlayerNumber, out var remote))
            {
                var vehicleIndex = data.Car == CarType.CustomVehicle ? 0 : (int)data.Car;
                var bot = new ComputerPlayer(_audio, _track, _settings, vehicleIndex, data.PlayerNumber, () => _elapsedTotal, () => _started);
                bot.Initialize(data.RaceData.PositionX, data.RaceData.PositionY, _track.Length);
                remote = new RemotePlayer(bot);
                _remotePlayers[data.PlayerNumber] = remote;
            }

            remote.State = data.State;
            if (data.State == PlayerState.Finished && !remote.Finished)
            {
                remote.Finished = true;
                if (!_settings.ThreeDSound)
                    remote.Player.Quiet();
                remote.Player.Stop();
                if ((int)data.PlayerNumber < _soundPlayerNr.Length)
                {
                    Speak(_soundPlayerNr[data.PlayerNumber]!, true);
                    Speak(_soundFinished[Math.Min(_positionFinish++, _soundFinished.Length - 1)]!, true);
                }
            }

            remote.Player.ApplyNetworkState(
                data.RaceData.PositionX,
                data.RaceData.PositionY,
                data.RaceData.Speed,
                data.RaceData.Frequency,
                data.EngineRunning,
                data.Braking,
                data.Horning,
                data.Backfiring,
                _car.PositionX,
                _car.PositionY,
                _track.Length);
        }

        public void ApplyBump(PacketPlayerBumped bump)
        {
            if (bump.PlayerNumber != _playerNumber)
                return;
            _car.Bump(bump.BumpX, bump.BumpY, bump.BumpSpeed);
        }

        public void RemoveRemotePlayer(byte playerNumber)
        {
            if (_remotePlayers.TryGetValue(playerNumber, out var remote))
            {
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
                _remotePlayers.Remove(playerNumber);
            }
        }

        private void UpdatePositions()
        {
            _position = 1;
            foreach (var remote in _remotePlayers.Values)
            {
                if (remote.Player.PositionY > _car.PositionY)
                    _position++;
            }
        }

        private void Comment(bool automatic)
        {
            if (!_started || _lap > _nrOfLaps)
                return;

            var position = 1;
            int inFrontNumber = -1;
            var inFrontDist = 500.0f;
            int onTailNumber = -1;
            var onTailDist = 500.0f;

            foreach (var remote in _remotePlayers.Values)
            {
                var bot = remote.Player;
                if (bot.PositionY > _car.PositionY)
                {
                    position++;
                    var dist = bot.PositionY - _car.PositionY;
                    if (dist < inFrontDist)
                    {
                        inFrontNumber = bot.PlayerNumber;
                        inFrontDist = dist;
                    }
                }
                else if (bot.PositionY < _car.PositionY)
                {
                    var dist = _car.PositionY - bot.PositionY;
                    if (dist < onTailDist)
                    {
                        onTailNumber = bot.PlayerNumber;
                        onTailDist = dist;
                    }
                }
            }

            if (automatic && position != _positionComment)
            {
                if (position - 1 < _soundPosition.Length)
                    Speak(_soundPosition[position - 1]!, true);
                _positionComment = position;
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFrontNumber != -1 && inFrontNumber < _soundPlayerNr.Length)
                {
                    Speak(_soundPlayerNr[inFrontNumber]!, true);
                    var sound = _randomSounds[(int)RandomSound.Front][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Front])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }
            else
            {
                if (onTailNumber != -1 && onTailNumber < _soundPlayerNr.Length)
                {
                    Speak(_soundPlayerNr[onTailNumber]!, true);
                    var sound = _randomSounds[(int)RandomSound.Tail][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Tail])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }

            if (inFrontNumber == -1 && onTailNumber == -1 && !automatic)
            {
                if (position - 1 < _soundPosition.Length)
                    Speak(_soundPosition[position - 1]!, true);
                _positionComment = position;
            }
        }

        private int CalculatePlayerPerc(int player)
        {
            if (player == _playerNumber)
                return ClampPercent(_car.PositionY);

            var targetNumber = (byte)player;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return ClampPercent(remote.Player.PositionY);

            return 0;
        }

        private int ClampPercent(float positionY)
        {
            var perc = (int)((positionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            if (perc > 100)
                perc = 100;
            if (perc < 0)
                perc = 0;
            return perc;
        }

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == _playerNumber)
            {
                if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                    return FormatVehicleName(_car.CustomFile);
                return $"Vehicle {(int)_car.CarType + 1}";
            }

            var targetNumber = (byte)playerIndex;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return $"Vehicle {remote.Player.VehicleIndex + 1}";

            return "Vehicle";
        }
    }
}