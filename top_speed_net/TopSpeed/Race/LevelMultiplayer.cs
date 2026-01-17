using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Tracks.Map;
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
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint playerId,
            byte playerNumber)
            : base(audio, speech, settings, input, trackName, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, trackData, trackData.UserDefined)
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

            var rowSpacing = Math.Max(10.0f, _car.LengthM * 1.5f);
            var startPosition = CalculateStartPosition(_playerNumber, _car.WidthM, rowSpacing);
            _car.SetPosition(startPosition.X, startPosition.Z);

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
            _soundTheme4.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));

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
            _track.Run(_car.MapState);

            var road = _track.RoadAt(_car.MapState);
            _car.Evaluate(road);
            UpdateAudioListener(elapsed);
            if (_track.NextRoad(_car.MapState, _car.Speed, (int)_settings.CurveAnnouncement, out var nextRoad))
                CallNextRoad(nextRoad);

            if (_track.Lap(_car.DistanceMeters) > _lap)
            {
                _lap = _track.Lap(_car.DistanceMeters);
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

            HandleTrackNameRequest();

            if (_input.GetPlayerNumber() && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                QueueSound(_soundNumbers[_playerNumber + 1]);
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, _soundNumbers[_playerNumber + 1].GetLengthSeconds());
            }

            HandleSpeedReportRequest();
            HandleDistanceReportRequest();
            HandleWheelAngleReportRequest();
            HandleHeadingReportRequest();
            HandlePauseRequest(ref _pauseKeyReleased);

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
                    PositionX = _car.WorldPosition.X,
                    PositionY = _car.WorldPosition.Z,
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
            _soundTheme4?.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
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
                _car.WorldPosition.X,
                _car.WorldPosition.Z,
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
                if (remote.Player.DistanceMeters > _car.DistanceMeters)
                    _position++;
            }
        }

        private Vector3 CalculateStartPosition(int gridIndex, float vehicleWidth, float rowSpacing)
        {
            var start = _track.Map.CellToWorld(_track.Map.StartX, _track.Map.StartZ);
            var forward = MapMovement.DirectionVector(_track.Map.StartHeading);
            var right = new Vector3(forward.Z, 0f, -forward.X);

            var halfWidth = Math.Max(0.1f, vehicleWidth * 0.5f);
            var margin = 0.3f;
            var laneHalfWidth = _track.LaneWidth;
            var laneOffset = laneHalfWidth - halfWidth - margin;
            if (laneOffset < 0f)
                laneOffset = 0f;

            var row = gridIndex / 2;
            var columnOffset = gridIndex % 2 == 1 ? laneOffset : -laneOffset;
            return start + (right * columnOffset) - (forward * (row * rowSpacing));
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
                if (bot.DistanceMeters > _car.DistanceMeters)
                {
                    position++;
                }

                var delta = GetRelativeTrackDelta(bot.DistanceMeters);
                if (delta > 0f)
                {
                    var dist = delta;
                    if (dist < inFrontDist)
                    {
                        inFrontNumber = bot.PlayerNumber;
                        inFrontDist = dist;
                    }
                }
                else if (delta < 0f)
                {
                    var dist = -delta;
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
                return ClampPercent(_car.DistanceMeters);

            var targetNumber = (byte)player;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return ClampPercent(remote.Player.DistanceMeters);

            return 0;
        }

        private int ClampPercent(float distanceMeters)
        {
            var perc = (int)((distanceMeters / (float)(_track.Length * _nrOfLaps)) * 100.0f);
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
                return _car.VehicleName;
            }

            var targetNumber = (byte)playerIndex;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return VehicleCatalog.Vehicles[remote.Player.VehicleIndex].Name;

            return "Vehicle";
        }
    }
}
