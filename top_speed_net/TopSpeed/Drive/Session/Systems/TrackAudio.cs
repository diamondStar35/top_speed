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
        // Curve tones are language independent. There is one mono file per curve tightness;
        // the engine pans it to the side the curve turns toward, so a left curve is heard on
        // the left. The tighter the curve, the higher the pitch (baked into each file).
        private static readonly string[] TurnToneNames = { "easy", "normal", "hard", "hairpin" };

        // There are 8 curve slots (index = TrackType - 1): slots 0-3 are the left curves in
        // order (easy, normal, hard, hairpin) and slots 4-7 mirror them on the right.
        private const int CurveSlotCount = 8;

        // Pan applied to a turn tone (-1 full left, +1 full right). The baked stereo originals
        // were hard panned, so full pan preserves that; a future setting can narrow the field.
        private const float TurnTonePan = 1.0f;

        private readonly DriveSettings _settings;
        private readonly Func<int, Source?> _getRandomSound;
        private readonly Func<string, Source?>? _loadRaceCueSound;
        private readonly Source?[] _turnTones = new Source?[CurveSlotCount];
        private readonly Source? _turnEndDing;
        private readonly Action<Source?> _queueTrackInfoSound;
        private readonly Action<Event, float> _queueEvent;
        private TrackType _lastRoadTypeAtPosition;
        private bool _hasLastRoadTypeAtPosition;

        public TrackAudio(
            DriveSettings settings,
            Func<int, Source?> getRandomSound,
            Func<string, Source?>? loadRaceCueSound,
            Source? turnEndDing,
            Action<Source?> queueTrackInfoSound,
            Action<Event, float> queueEvent)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _getRandomSound = getRandomSound ?? throw new ArgumentNullException(nameof(getRandomSound));
            _loadRaceCueSound = loadRaceCueSound;
            _turnEndDing = turnEndDing;
            _queueTrackInfoSound = queueTrackInfoSound ?? throw new ArgumentNullException(nameof(queueTrackInfoSound));
            _queueEvent = queueEvent ?? throw new ArgumentNullException(nameof(queueEvent));
            Reset();
        }

        // Resolves the sound for a curve slot (0-7), honoring the spoken/tones preference.
        // In tones mode the language independent cue is used; if it is missing we fall back to
        // the spoken copilot sound so a curve is never announced silently.
        private Source? ResolveCurveSound(int index)
        {
            if (_settings.CurveAnnouncementStyle == CurveAnnouncementStyle.Tones &&
                _loadRaceCueSound != null &&
                index >= 0 && index < _turnTones.Length)
            {
                var tone = _turnTones[index] ??= LoadTurnTone(index);
                if (tone != null)
                    return tone;
            }

            return _getRandomSound(index);
        }

        // Loads the mono tone for a curve slot and pans it to the side the curve turns toward.
        // Left curves (slots 0-3) pan left, right curves (slots 4-7) pan right; both sides share
        // the same file per tightness, so we get two independently panned sources from one asset.
        private Source? LoadTurnTone(int index)
        {
            var tightness = index % TurnToneNames.Length;
            var pan = index < TurnToneNames.Length ? -TurnTonePan : TurnTonePan;
            var tone = _loadRaceCueSound!($"turns/{TurnToneNames[tightness]}");
            tone?.SetPan(pan);
            return tone;
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

        // Announce a curve the player is about to drive onto (e.g. the segment they rejoin on when
        // leaving pit road). Unlike AnnounceNextRoad this is a plain curve callout with no surface
        // comparison, since there is no meaningful "current" road while parked in the pit box.
        public void AnnounceUpcomingCurve(Track.Road road)
        {
            if ((int)_settings.Copilot > 0 && road.Type != TrackType.Straight)
            {
                var index = (int)road.Type - 1;
                _queueTrackInfoSound(ResolveCurveSound(index));
            }
        }

        public Track.Road AnnounceNextRoad(Track.Road currentRoad, Track.Road nextRoad)
        {
            if ((int)_settings.Copilot > 0 && nextRoad.Type != TrackType.Straight)
            {
                var index = (int)nextRoad.Type - 1;
                _queueTrackInfoSound(ResolveCurveSound(index));
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
