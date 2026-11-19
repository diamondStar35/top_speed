using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Drive.Multiplayer.Session.Systems
{
    internal sealed class Commentary : TopSpeed.Drive.Session.Subsystem
    {
        private const int FrontSlot = 14;
        private const int TailSlot = 15;

        private readonly DriveSettings _settings;
        private readonly DriveInput _input;
        private readonly Vehicles.ICar _car;
        private readonly IDictionary<byte, RemotePlayer> _remotePlayers;
        private readonly Func<int, Source?> _getPositionSound;
        private readonly Func<int, Source?> _getPlayerNumberSound;
        private readonly Func<int, Source?> _getRandomSound;
        private readonly Func<bool> _isStarted;
        private readonly Func<int> _getLap;
        private readonly Func<int> _getLapLimit;
        private readonly Func<int> _getPositionComment;
        private readonly Action<int> _setPositionComment;
        private readonly Action<Source?, bool> _speakIfLoaded;
        private readonly Action<Source, bool> _speak;
        private bool _infoKeyReleased = true;
        private float _lastComment;

        public Commentary(
            string name,
            int order,
            DriveSettings settings,
            DriveInput input,
            Vehicles.ICar car,
            IDictionary<byte, RemotePlayer> remotePlayers,
            Func<int, Source?> getPositionSound,
            Func<int, Source?> getPlayerNumberSound,
            Func<int, Source?> getRandomSound,
            Func<bool> isStarted,
            Func<int> getLap,
            Func<int> getLapLimit,
            Func<int> getPositionComment,
            Action<int> setPositionComment,
            Action<Source?, bool> speakIfLoaded,
            Action<Source, bool> speak)
            : base(name, order)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _remotePlayers = remotePlayers ?? throw new ArgumentNullException(nameof(remotePlayers));
            _getPositionSound = getPositionSound ?? throw new ArgumentNullException(nameof(getPositionSound));
            _getPlayerNumberSound = getPlayerNumberSound ?? throw new ArgumentNullException(nameof(getPlayerNumberSound));
            _getRandomSound = getRandomSound ?? throw new ArgumentNullException(nameof(getRandomSound));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _getLapLimit = getLapLimit ?? throw new ArgumentNullException(nameof(getLapLimit));
            _getPositionComment = getPositionComment ?? throw new ArgumentNullException(nameof(getPositionComment));
            _setPositionComment = setPositionComment ?? throw new ArgumentNullException(nameof(setPositionComment));
            _speakIfLoaded = speakIfLoaded ?? throw new ArgumentNullException(nameof(speakIfLoaded));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            _lastComment += elapsed;
            if (_settings.AutomaticInfo == AutomaticInfoMode.On && _lastComment > 6.0f)
            {
                Comment(automatic: true);
                _lastComment = 0f;
            }

            if (_input.Intents.IsTriggered(DriveIntent.RequestInfo) && _infoKeyReleased)
            {
                _infoKeyReleased = false;
                Comment(automatic: false);
                _lastComment = 0f;
            }
            else if (!_input.Intents.IsTriggered(DriveIntent.RequestInfo) && !_infoKeyReleased)
            {
                _infoKeyReleased = true;
            }
        }

        public void Reset()
        {
            _infoKeyReleased = true;
            _lastComment = 0f;
        }

        private void Comment(bool automatic)
        {
            if (!_isStarted() || _getLap() > _getLapLimit())
                return;

            var position = 1;
            var inFrontNumber = -1;
            var inFrontDist = 500f;
            var onTailNumber = -1;
            var onTailDist = 500f;

            foreach (var remote in _remotePlayers.Values)
            {
                var bot = remote.Player;
                if (bot.PositionY > _car.PositionY)
                    position++;

                var delta = bot.PositionY - _car.PositionY;
                if (delta > 0f)
                {
                    if (delta < inFrontDist)
                    {
                        inFrontNumber = bot.PlayerNumber;
                        inFrontDist = delta;
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

            if (automatic && position != _getPositionComment())
            {
                _speakIfLoaded(_getPositionSound(position - 1), true);
                _setPositionComment(position);
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFrontNumber >= 0)
                {
                    _speakIfLoaded(_getPlayerNumberSound(inFrontNumber), true);
                    SpeakRandom(FrontSlot);
                    return;
                }
            }
            else if (onTailNumber >= 0)
            {
                _speakIfLoaded(_getPlayerNumberSound(onTailNumber), true);
                SpeakRandom(TailSlot);
                return;
            }

            if (inFrontNumber == -1 && onTailNumber == -1 && !automatic)
            {
                _speakIfLoaded(_getPositionSound(position - 1), true);
                _setPositionComment(position);
            }
        }

        private void SpeakRandom(int slot)
        {
            var sound = _getRandomSound(slot);
            if (sound != null)
                _speak(sound, true);
        }
    }
}
