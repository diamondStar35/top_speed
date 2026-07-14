using System;
using System.Collections.Generic;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static void FlushPending(ref SegmentBuilder? pendingSegment, List<TrackDefinition> segments, float minPart)
        {
            if (!pendingSegment.HasValue)
                return;
            var p = pendingSegment.Value;
            pendingSegment = null;
            segments.Add(new TrackDefinition(
                p.Type,
                p.Surface,
                p.Noise,
                Math.Max(minPart, p.Length),
                p.Id,
                p.Width,
                p.Height,
                p.WeatherProfileId,
                p.WeatherTransitionSeconds,
                p.RoomId,
                p.RoomOverrides.HasAny ? p.RoomOverrides : null,
                p.SoundSourceIds,
                p.Metadata,
                p.IsPitEntry,
                p.IsPitExit));
        }

        private static void FlushPending(ref WeatherBuilder? pendingWeather, Dictionary<string, TrackWeatherProfile> weatherProfiles)
        {
            if (!pendingWeather.HasValue)
                return;
            var p = pendingWeather.Value;
            pendingWeather = null;
            weatherProfiles[p.Id] = p.Build();
        }

        private static void FlushPending(ref RoomBuilder? pendingRoom, Dictionary<string, TrackRoomDefinition> rooms)
        {
            if (!pendingRoom.HasValue)
                return;
            var p = pendingRoom.Value;
            pendingRoom = null;
            rooms[p.Id] = p.Build();
        }

        private static void FlushPending(ref SoundBuilder? pendingSound, Dictionary<string, TrackSoundSourceDefinition> sounds)
        {
            if (!pendingSound.HasValue)
                return;
            var p = pendingSound.Value;
            pendingSound = null;
            sounds[p.Id] = p.Build();
        }

        private static bool TryParseWeatherKind(string raw, out TrackWeather value)
        {
            value = TrackWeather.Sunny;
            if (TryParseInt(raw, out var weatherInt) && weatherInt >= 0 && weatherInt <= 3)
            {
                value = (TrackWeather)weatherInt;
                return true;
            }

            switch (NormalizeLookupToken(raw))
            {
                case "rain":
                case "rainy":
                    value = TrackWeather.Rain;
                    return true;
                case "wind":
                case "windy":
                    value = TrackWeather.Wind;
                    return true;
                case "storm":
                case "stormy":
                    value = TrackWeather.Storm;
                    return true;
                default:
                    if (NormalizeLookupToken(raw) == "sunny")
                    {
                        value = TrackWeather.Sunny;
                        return true;
                    }

                    return false;
            }
        }

        private static TrackAmbience ParseAmbience(IReadOnlyDictionary<string, string> meta)
        {
            if (!meta.TryGetValue("ambience", out var raw))
                return TrackAmbience.NoAmbience;
            if (TryParseInt(raw, out var ambienceInt) && ambienceInt >= 0 && ambienceInt <= 2)
                return (TrackAmbience)ambienceInt;
            switch (NormalizeLookupToken(raw))
            {
                case "desert":
                    return TrackAmbience.Desert;
                case "airport":
                    return TrackAmbience.Airport;
                default:
                    return TrackAmbience.NoAmbience;
            }
        }

        private static bool TryParseSegmentPitPoint(string raw, out SegmentPitPoint value)
        {
            value = SegmentPitPoint.PitEntry;
            switch (NormalizeLookupToken(raw))
            {
                case "pitentry": value = SegmentPitPoint.PitEntry; return true;
                case "pitexit": value = SegmentPitPoint.PitExit; return true;
                default: return false;
            }
        }

        private static bool TryParseTrackType(string raw, out TrackType value)
        {
            value = TrackType.Straight;
            if (TryParseInt(raw, out var parsed) && parsed >= 0 && parsed < Types)
            {
                value = (TrackType)parsed;
                return true;
            }
            switch (NormalizeLookupToken(raw))
            {
                case "straight": value = TrackType.Straight; return true;
                case "easyleft": value = TrackType.EasyLeft; return true;
                case "left": value = TrackType.Left; return true;
                case "hardleft": value = TrackType.HardLeft; return true;
                case "hairpinleft": value = TrackType.HairpinLeft; return true;
                case "easyright": value = TrackType.EasyRight; return true;
                case "right": value = TrackType.Right; return true;
                case "hardright": value = TrackType.HardRight; return true;
                case "hairpinright": value = TrackType.HairpinRight; return true;
                default: return false;
            }
        }

        private static bool TryParseTrackSurface(string raw, out TrackSurface value)
        {
            value = TrackSurface.Asphalt;
            if (TryParseInt(raw, out var parsed) && parsed >= 0 && parsed < Surfaces)
            {
                value = (TrackSurface)parsed;
                return true;
            }
            switch (NormalizeLookupToken(raw))
            {
                case "asphalt": value = TrackSurface.Asphalt; return true;
                case "gravel": value = TrackSurface.Gravel; return true;
                case "water": value = TrackSurface.Water; return true;
                case "sand": value = TrackSurface.Sand; return true;
                case "snow": value = TrackSurface.Snow; return true;
                default: return false;
            }
        }

        private static bool TryParseTrackNoise(string raw, out TrackNoise value)
        {
            value = TrackNoise.NoNoise;
            if (TryParseInt(raw, out var parsed) && parsed >= 0 && parsed < Noises)
            {
                value = (TrackNoise)parsed;
                return true;
            }
            switch (NormalizeLookupToken(raw))
            {
                case "none":
                case "nonoise":
                case "off": value = TrackNoise.NoNoise; return true;
                case "crowd": value = TrackNoise.Crowd; return true;
                case "ocean": value = TrackNoise.Ocean; return true;
                case "runway": value = TrackNoise.Runway; return true;
                case "clock": value = TrackNoise.Clock; return true;
                case "jet": value = TrackNoise.Jet; return true;
                case "thunder": value = TrackNoise.Thunder; return true;
                case "pile": value = TrackNoise.Pile; return true;
                case "construction": value = TrackNoise.Construction; return true;
                case "river": value = TrackNoise.River; return true;
                case "helicopter": value = TrackNoise.Helicopter; return true;
                case "owl": value = TrackNoise.Owl; return true;
                default: return false;
            }
        }

        private static bool TryParseSoundType(string raw, out TrackSoundSourceType value)
        {
            value = TrackSoundSourceType.Ambient;
            if (TryParseInt(raw, out var parsed) && parsed >= 0 && parsed <= 3)
            {
                value = (TrackSoundSourceType)parsed;
                return true;
            }
            switch (NormalizeLookupToken(raw))
            {
                case "ambient": value = TrackSoundSourceType.Ambient; return true;
                case "static": value = TrackSoundSourceType.Static; return true;
                case "moving": value = TrackSoundSourceType.Moving; return true;
                case "random": value = TrackSoundSourceType.Random; return true;
                default: return false;
            }
        }

        private static bool TryParseSoundRandomMode(string raw, out TrackSoundRandomMode value)
        {
            value = TrackSoundRandomMode.OnStart;
            if (TryParseInt(raw, out var parsed) && parsed >= 0 && parsed <= 1)
            {
                value = (TrackSoundRandomMode)parsed;
                return true;
            }
            switch (NormalizeLookupToken(raw))
            {
                case "onstart": value = TrackSoundRandomMode.OnStart; return true;
                case "perarea": value = TrackSoundRandomMode.PerArea; return true;
                default: return false;
            }
        }
    }
}
