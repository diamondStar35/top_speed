using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildTrackTypeMenu(string id, DriveMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Race track"), MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.RaceTrack), onActivate: () => _setup.TrackCategory = TrackCategory.RaceTrack),
                new MenuItem(LocalizationService.Mark("Street adventure"), MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.StreetAdventure), onActivate: () => _setup.TrackCategory = TrackCategory.StreetAdventure),
                new MenuItem(LocalizationService.Mark("Custom track"), MenuAction.None, onActivate: () => OpenCustomTrackMenuOrAnnounce(mode)),
                new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => PushRandomTrackType(mode))
            };
            return BackMenu(id, items, LocalizationService.Mark("Choose track type"));
        }

        private MenuScreen BuildTrackMenu(string id, DriveMode mode, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var trackList = TrackList.GetTracks(category);
            var nextMenuId = VehicleMenuId(mode);

            foreach (var track in trackList)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectTrack(category, key)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectRandomTrack(category)));
            return BackMenu(id, items, LocalizationService.Mark("Select a track"));
        }

        private MenuScreen BuildCustomTrackMenu(string id, DriveMode mode)
        {
            return BackMenu(id, BuildCustomTrackItems(mode), LocalizationService.Mark("Select a custom track"));
        }

        private void RefreshCustomTrackMenu(DriveMode mode)
        {
            var id = TrackMenuId(mode, TrackCategory.CustomTrack);
            _menu.UpdateItems(id, BuildCustomTrackItems(mode));
        }

        private List<MenuItem> BuildCustomTrackItems(DriveMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = VehicleMenuId(mode);
            var customTracks = _selection.GetCustomTrackInfo();
            if (customTracks.Count == 0)
                return items;

            foreach (var track in customTracks)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectTrack(TrackCategory.CustomTrack, key)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomCustomTrack));
            return items;
        }

        private void PushRandomTrackType(DriveMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var roll = Algorithm.RandomInt(customTracks.Count > 0 ? 3 : 2);
            var category = roll switch
            {
                0 => TrackCategory.RaceTrack,
                1 => TrackCategory.StreetAdventure,
                _ => TrackCategory.CustomTrack
            };

            _setup.TrackCategory = category;
            if (category == TrackCategory.CustomTrack)
                RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, category));
        }

        private void OpenCustomTrackMenuOrAnnounce(DriveMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var issues = _selection.ConsumeCustomTrackIssues();
            if (customTracks.Count == 0)
            {
                if (issues.Count > 0)
                {
                    _ui.ShowMessageDialog(
                        LocalizationService.Mark("Custom track errors"),
                        LocalizationService.Mark("Some custom track files are invalid and were skipped."),
                        issues);
                }
                else
                {
                    _ui.SpeakMessage(LocalizationService.Mark("No custom tracks found."));
                }
                return;
            }

            _setup.TrackCategory = TrackCategory.CustomTrack;
            RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, TrackCategory.CustomTrack));

            if (issues.Count > 0)
            {
                _ui.ShowMessageDialog(
                    LocalizationService.Mark("Custom track issues"),
                    LocalizationService.Mark("Some custom track files reported issues. Tracks with errors were skipped; tracks with warnings still loaded."),
                    issues);
            }
        }
    }
}

