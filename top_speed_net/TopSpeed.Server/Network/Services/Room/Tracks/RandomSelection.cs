using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            private static readonly string[] RaceTrackKeys =
            {
                "america",
                "austria",
                "belgium",
                "brazil",
                "china",
                "england",
                "finland",
                "france",
                "germany",
                "ireland",
                "italy",
                "netherlands",
                "portugal",
                "russia",
                "spain",
                "sweden",
                "switserland"
            };

            private static readonly string[] AdventureTrackKeys =
            {
                "advHills",
                "advCoast",
                "advCountry",
                "advAirport",
                "advDesert",
                "advRush",
                "advEscape"
            };

            public void ApplyRandomTrackSelectionForRace(GameRoom room)
            {
                if (room == null || room.RandomTrackSelection == null || !room.RandomTrackSelection.IsRandomBuiltIn)
                    return;

                if (!ResolveRandomTrackSelection(room))
                    SetTrackData(room, "america");

                _owner.BroadcastSelectedTrackToRoom(room);
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.TrackChanged);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Random room track selected: room={0} \"{1}\", track={2}."),
                    room.Id,
                    room.Name,
                    room.TrackName));
            }

            private bool ResolveRandomTrackSelection(GameRoom room)
            {
                if (room == null || room.RandomTrackSelection == null || !room.RandomTrackSelection.IsRandomBuiltIn)
                    return false;

                var candidates = GetRandomTrackCandidates(room.RandomTrackSelection.BuiltInTrackKey);
                if (candidates.Length == 0)
                    return false;

                var index = _owner._random.Next(candidates.Length);
                if (candidates.Length > 1)
                {
                    var guard = 0;
                    while (string.Equals(candidates[index], room.TrackName, StringComparison.OrdinalIgnoreCase)
                           && guard < candidates.Length)
                    {
                        index = _owner._random.Next(candidates.Length);
                        guard++;
                    }
                }

                SetTrackData(room, candidates[index]);
                return true;
            }

            private static string[] GetRandomTrackCandidates(string randomTrackKey)
            {
                var key = (randomTrackKey ?? string.Empty).Trim();
                if (string.Equals(key, TrackPackageRef.RandomBuiltInRaceKey, StringComparison.OrdinalIgnoreCase))
                    return RaceTrackKeys;
                if (string.Equals(key, TrackPackageRef.RandomBuiltInAdventureKey, StringComparison.OrdinalIgnoreCase))
                    return AdventureTrackKeys;
                if (string.Equals(key, TrackPackageRef.RandomBuiltInAnyKey, StringComparison.OrdinalIgnoreCase))
                    return AllBuiltInTrackKeys.Value;

                return Array.Empty<string>();
            }

            private static readonly Lazy<string[]> AllBuiltInTrackKeys =
                new Lazy<string[]>(() =>
                {
                    var tracks = new string[RaceTrackKeys.Length + AdventureTrackKeys.Length];
                    Array.Copy(RaceTrackKeys, tracks, RaceTrackKeys.Length);
                    Array.Copy(AdventureTrackKeys, 0, tracks, RaceTrackKeys.Length, AdventureTrackKeys.Length);
                    return tracks;
                });
        }
    }
}
