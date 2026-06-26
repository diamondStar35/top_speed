using System;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class TrackAudio
    {
        private readonly DriveSettings _settings;
        private readonly Func<int, Source?> _getRandomSound;
        private readonly Source? _turnEndDing;
        private readonly Action<Source?> _queueTrackInfoSound;
        private readonly Action<Event, float> _queueEvent;
        private TrackType _lastRoadTypeAtPosition;
        private bool _hasLastRoadTypeAtPosition;

        public TrackAudio(
            DriveSettings settings,
            Func<int, Source?> getRandomSound,
            Source? turnEndDing,
            Action<Source?> queueTrackInfoSound,
            Action<Event, float> queueEvent)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _getRandomSound = getRandomSound ?? throw new ArgumentNullException(nameof(getRandomSound));
            _turnEndDing = turnEndDing;
            _queueTrackInfoSound = queueTrackInfoSound ?? throw new ArgumentNullException(nameof(queueTrackInfoSound));
            _queueEvent = queueEvent ?? throw new ArgumentNullException(nameof(queueEvent));
            Reset();
        }

        public void Reset()
        {
            _lastRoadTypeAtPosition = TrackType.Straight;
            _hasLastRoadTypeAtPosition = false;
        }

        public void HandleRoad(Track.Road road)
        {
            var currentType = road.Type;
            if (_hasLastRoadTypeAtPosition &&
                _lastRoadTypeAtPosition != TrackType.Straight &&
                currentType == TrackType.Straight &&
                _turnEndDing != null)
            {
                _turnEndDing.Stop();
                _turnEndDing.SeekToStart();
                _turnEndDing.Play(loop: false);
            }

            _lastRoadTypeAtPosition = currentType;
            _hasLastRoadTypeAtPosition = true;
        }

        public Track.Road AnnounceNextRoad(Track.Road currentRoad, Track.Road nextRoad)
        {
            if ((int)_settings.Copilot > 0 && nextRoad.Type != TrackType.Straight)
            {
                var index = (int)nextRoad.Type - 1;
                _queueTrackInfoSound(_getRandomSound(index));
            }

            if ((int)_settings.Copilot > 1 && nextRoad.Surface != currentRoad.Surface)
            {
                var index = (int)nextRoad.Surface + 8;
                _queueEvent(new Event(Events.PlayTrackInfoSound, _getRandomSound(index)), 1.0f);
            }

            return nextRoad;
        }
    }
}
