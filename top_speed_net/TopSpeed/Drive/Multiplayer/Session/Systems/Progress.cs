using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Drive.Multiplayer.Session.Systems
{
    internal sealed class Progress : TopSpeed.Drive.Session.Subsystem
    {
        private readonly Tracks.Track _track;
        private readonly ICar _car;
        private readonly DriveSettings _settings;
        private readonly IDictionary<byte, RemotePlayer> _remotePlayers;
        private readonly int _lapLimit;
        private readonly Func<int, Source?> _getLapSound;
        private readonly byte _localPlayerNumber;
        private readonly Func<int> _getLap;
        private readonly Action<int> _setLap;
        private readonly Func<int> _getRaceTimeMs;
        private readonly Func<CarState> _getLastCarState;
        private readonly Action<CarState> _setLastCarState;
        private readonly Action _applyPlayerFinishState;
        private readonly Func<bool> _isHostPaused;
        private readonly Func<bool> _isSentFinish;
        private readonly Action _markSentFinish;
        private readonly Action<PlayerState> _setPlayerState;
        private readonly Action<int> _setPosition;
        private readonly Action<int> _announceFinishOrder;
        private readonly Action<Source, bool> _speak;
        private readonly Action<bool> _sendPlayerState;
        private readonly Action _sendCrash;

        public Progress(
            string name,
            int order,
            Tracks.Track track,
            ICar car,
            DriveSettings settings,
            IDictionary<byte, RemotePlayer> remotePlayers,
            int lapLimit,
            Func<int, Source?> getLapSound,
            byte localPlayerNumber,
            Func<int> getLap,
            Action<int> setLap,
            Func<int> getRaceTimeMs,
            Func<CarState> getLastCarState,
            Action<CarState> setLastCarState,
            Action applyPlayerFinishState,
            Func<bool> isHostPaused,
            Func<bool> isSentFinish,
            Action markSentFinish,
            Action<PlayerState> setPlayerState,
            Action<int> setPosition,
            Action<int> announceFinishOrder,
            Action<Source, bool> speak,
            Action<bool> sendPlayerState,
            Action sendCrash)
            : base(name, order)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _remotePlayers = remotePlayers ?? throw new ArgumentNullException(nameof(remotePlayers));
            _lapLimit = lapLimit;
            _getLapSound = getLapSound ?? throw new ArgumentNullException(nameof(getLapSound));
            _localPlayerNumber = localPlayerNumber;
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _setLap = setLap ?? throw new ArgumentNullException(nameof(setLap));
            _getRaceTimeMs = getRaceTimeMs ?? throw new ArgumentNullException(nameof(getRaceTimeMs));
            _getLastCarState = getLastCarState ?? throw new ArgumentNullException(nameof(getLastCarState));
            _setLastCarState = setLastCarState ?? throw new ArgumentNullException(nameof(setLastCarState));
            _applyPlayerFinishState = applyPlayerFinishState ?? throw new ArgumentNullException(nameof(applyPlayerFinishState));
            _isHostPaused = isHostPaused ?? throw new ArgumentNullException(nameof(isHostPaused));
            _isSentFinish = isSentFinish ?? throw new ArgumentNullException(nameof(isSentFinish));
            _markSentFinish = markSentFinish ?? throw new ArgumentNullException(nameof(markSentFinish));
            _setPlayerState = setPlayerState ?? throw new ArgumentNullException(nameof(setPlayerState));
            _setPosition = setPosition ?? throw new ArgumentNullException(nameof(setPosition));
            _announceFinishOrder = announceFinishOrder ?? throw new ArgumentNullException(nameof(announceFinishOrder));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
            _sendPlayerState = sendPlayerState ?? throw new ArgumentNullException(nameof(sendPlayerState));
            _sendCrash = sendCrash ?? throw new ArgumentNullException(nameof(sendCrash));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            if (_isHostPaused())
                return;

            UpdatePositions();

            var lastState = _getLastCarState();
            if (!_isSentFinish()
                && lastState != CarState.Crashing
                && lastState != CarState.Crashed
                && (_car.State == CarState.Crashing || _car.State == CarState.Crashed))
            {
                _sendCrash();
            }

            _setLastCarState(_car.State);

            var currentLap = _track.Lap(_car.PositionY);
            if (currentLap <= _getLap())
                return;

            _setLap(currentLap);
            if (currentLap <= _lapLimit)
            {
                if (_settings.AutomaticInfo != AutomaticInfoMode.Off
                    && currentLap > 1)
                {
                    var lapSound = _getLapSound(_lapLimit - currentLap);
                    if (lapSound != null)
                        _speak(lapSound, true);
                }

                return;
            }

            _applyPlayerFinishState();
            AnnounceLocalFinish();
        }

        public void AnnounceRemoteFinish(byte playerNumber)
        {
            _announceFinishOrder(playerNumber);
        }

        private void UpdatePositions()
        {
            var position = 1;
            foreach (var remote in _remotePlayers.Values)
            {
                if (remote.Finished || remote.Player.PositionY > _car.PositionY)
                    position++;
            }

            _setPosition(position);
        }

        private void AnnounceLocalFinish()
        {
            _announceFinishOrder(_localPlayerNumber);
            if (_isSentFinish())
                return;

            _markSentFinish();
            _setPlayerState(PlayerState.Finished);
            _sendPlayerState(false);
        }
    }
}
