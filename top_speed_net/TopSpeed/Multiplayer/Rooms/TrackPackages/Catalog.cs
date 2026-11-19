using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleTrackPackageCatalog(PacketTrackPackageCatalog catalog)
        {
            _roomsFlow.HandleTrackPackageCatalog(catalog);
        }

        internal void HandleTrackPackageCatalogCore(PacketTrackPackageCatalog catalog)
        {
            var source = catalog?.Tracks ?? Array.Empty<PacketTrackPackageCatalogEntry>();
            var items = new List<PacketTrackPackageCatalogEntry>(source.Length);
            for (var i = 0; i < source.Length && items.Count < ProtocolConstants.MaxTrackPackageCatalogEntries; i++)
            {
                var item = source[i];
                if (!PacketValidation.IsValidTrackPackageCatalogEntry(item))
                    continue;

                items.Add(new PacketTrackPackageCatalogEntry
                {
                    Track = TrackPackageRef.Custom(item.Track.TrackId, item.Track.Version, item.Track.Hash),
                    DisplayName = item.DisplayName,
                    HasPitArea = item.HasPitArea
                });

                // Remember each catalog track's pit-area status so the host's no-pit-area start-race
                // warning can resolve a track picked from the server catalog (not uploaded here).
                var hash = TrackPackageRef.NormalizeHash(item.Track.Hash);
                if (!string.IsNullOrWhiteSpace(hash))
                    _customTrackHasPitAreaByHash[hash] = item.HasPitArea;
            }

            _state.RoomDrafts.RoomTrackCatalog = items.ToArray();
            RebuildRoomTrackCustomMenu();

            if (_state.RoomDrafts.RoomTrackCatalogOpenPending)
            {
                _state.RoomDrafts.RoomTrackCatalogOpenPending = false;
                _menu.Push(MultiplayerMenuKeys.RoomTrackCustom);
            }
        }

        private void OpenRoomTrackCustomMenu()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (!IsCurrentRoomCustomTracksEnabled())
            {
                _speech.Speak(LocalizationService.Mark("Custom tracks are disabled for this room."));
                return;
            }

            RequestRoomTrackCatalog(openOnResponse: true);
        }

        private void RequestRoomTrackCatalog(bool openOnResponse)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            _state.RoomDrafts.RoomTrackCatalogOpenPending = openOnResponse;
            if (!TrySend(session.SendTrackPackageCatalogRequest(), LocalizationService.Mark("custom track list request")))
            {
                _state.RoomDrafts.RoomTrackCatalogOpenPending = false;
                return;
            }

            if (openOnResponse)
                _speech.Speak(LocalizationService.Mark("Loading custom tracks from server."));
        }

        private void RebuildRoomTrackCustomMenu()
        {
            var items = new List<MenuItem>();
            var tracks = _state.RoomDrafts.RoomTrackCatalog ?? Array.Empty<PacketTrackPackageCatalogEntry>();
            for (var i = 0; i < tracks.Length; i++)
            {
                var track = tracks[i];
                if (!PacketValidation.IsValidTrackPackageCatalogEntry(track))
                    continue;

                var display = string.IsNullOrWhiteSpace(track.DisplayName)
                    ? FormatTrackRefDisplay(track.Track)
                    : track.DisplayName;
                items.Add(new MenuItem(display, MenuAction.None, onActivate: () => SelectRoomTrack(track.Track, display, false)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Upload a local track"), MenuAction.None, onActivate: OpenRoomTrackLocalCustomMenu));
            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: SelectRandomRoomTrackAny));

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomTrackCustom, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackCustom, items, preserveSelection);
        }

        private void OpenRoomTrackLocalCustomMenu()
        {
            RebuildRoomTrackLocalCustomMenu();
            _menu.Push(MultiplayerMenuKeys.RoomTrackLocalCustom);
        }

        private void RebuildRoomTrackLocalCustomMenu()
        {
            var items = new List<MenuItem>();
            var tracks = _roomTrackUploadSource.GetInfo();
            if (tracks.Count == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No custom tracks found."), MenuAction.None));
            }
            else
            {
                for (var i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    items.Add(new MenuItem(track.Display, MenuAction.None, onActivate: () => StartLocalTrackUpload(track.Key, track.Display)));
                }
            }

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomTrackLocalCustom, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackLocalCustom, items, preserveSelection);
        }
    }
}
