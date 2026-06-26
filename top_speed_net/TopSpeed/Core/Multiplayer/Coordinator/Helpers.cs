using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private static string[] BuildNumberOptions(int min, int max)
        {
            if (max < min)
                return Array.Empty<string>();

            var options = new string[max - min + 1];
            for (var i = min; i <= max; i++)
            {
                var index = i - min;
                options[index] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return options;
        }

        private static string[] BuildNumericOptions(int min, int max, string singularTemplate, string pluralTemplate)
        {
            if (max < min)
                return Array.Empty<string>();

            var options = new string[max - min + 1];
            for (var i = min; i <= max; i++)
            {
                var index = i - min;
                var template = i == 1 ? singularTemplate : pluralTemplate;
                options[index] = LocalizationService.Format(template, i);
            }

            return options;
        }

        private static TrackInfo[] BuildRoomTrackOptions()
        {
            var tracks = new List<TrackInfo>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var track in TrackList.RaceTracks)
            {
                if (names.Add(track.Display))
                    tracks.Add(track);
            }

            foreach (var track in TrackList.AdventureTracks)
            {
                if (names.Add(track.Display))
                    tracks.Add(track);
            }

            return tracks.ToArray();
        }

        private bool TrySend(bool sent, string actionMessageId)
        {
            if (sent)
                return true;

            var action = LocalizationService.Translate(actionMessageId);
            if (string.IsNullOrWhiteSpace(action))
                action = actionMessageId ?? string.Empty;
            _speech.Speak(
                LocalizationService.Format(
                    LocalizationService.Mark("Failed to send {0}. Please check your connection."),
                    action));
            return false;
        }

        internal string ResolvePlayerName(byte playerNumber)
        {
            return _state.Rooms.ResolvePlayerName(playerNumber);
        }

        internal uint GetCurrentRaceEffectiveGameRules()
        {
            return NormalizeRoomOptionsGameRulesFlags(_state.Rooms.CurrentRoom.RaceEffectiveGameRulesFlags);
        }
    }
}

