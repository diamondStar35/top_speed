using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        // The custom vehicle package hash the local player currently has selected, or null when
        // they picked a built-in vehicle. Used to build the local car directly, independent of the
        // (unstable) network player number.
        public string? LocalSelectedCustomVehicleHash
        {
            get
            {
                var pending = _state.RoomDrafts.PendingLoadoutVehicle;
                return pending != null && pending.IsCustomPackage ? pending.Hash : null;
            }
        }

        public void HandleVehiclePackageCatalog(PacketVehiclePackageCatalog catalog)
        {
            var source = catalog?.Vehicles ?? Array.Empty<PacketVehiclePackageCatalogEntry>();
            var items = new List<PacketVehiclePackageCatalogEntry>(source.Length);
            for (var i = 0; i < source.Length && items.Count < ProtocolConstants.MaxVehiclePackageCatalogEntries; i++)
            {
                var item = source[i];
                if (!PacketValidation.IsValidVehiclePackageCatalogEntry(item))
                    continue;

                items.Add(new PacketVehiclePackageCatalogEntry
                {
                    Vehicle = VehiclePackageRef.Custom(item.Vehicle.VehicleId, item.Vehicle.Version, item.Vehicle.Hash),
                    DisplayName = item.DisplayName,
                    SupportsAutomatic = item.SupportsAutomatic,
                    SupportsManual = item.SupportsManual
                });
            }

            _state.RoomDrafts.LoadoutVehicleCatalog = items.ToArray();
            RebuildLoadoutVehicleCustomMenu();

            if (_state.RoomDrafts.LoadoutVehicleCatalogOpenPending)
            {
                _state.RoomDrafts.LoadoutVehicleCatalogOpenPending = false;
                _menu.Push(MultiplayerMenuKeys.LoadoutVehicleCustom);
            }
        }

        private void OpenLoadoutVehicleCustomMenu()
        {
            RequestLoadoutVehicleCatalog(openOnResponse: true);
        }

        private void RequestLoadoutVehicleCatalog(bool openOnResponse)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            _state.RoomDrafts.LoadoutVehicleCatalogOpenPending = openOnResponse;
            if (!TrySend(session.SendVehiclePackageCatalogRequest(), LocalizationService.Mark("custom vehicle list request")))
            {
                _state.RoomDrafts.LoadoutVehicleCatalogOpenPending = false;
                return;
            }

            if (openOnResponse)
                _speech.Speak(LocalizationService.Mark("Loading custom vehicles from server."));
        }

        private void RebuildLoadoutVehicleCustomMenu()
        {
            var items = new List<MenuItem>();
            var vehicles = _state.RoomDrafts.LoadoutVehicleCatalog ?? Array.Empty<PacketVehiclePackageCatalogEntry>();
            if (vehicles.Length == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No custom vehicles available on this server."), MenuAction.None));
            }
            else
            {
                for (var i = 0; i < vehicles.Length; i++)
                {
                    var entry = vehicles[i];
                    if (!PacketValidation.IsValidVehiclePackageCatalogEntry(entry))
                        continue;

                    var display = string.IsNullOrWhiteSpace(entry.DisplayName)
                        ? LocalizationService.Mark("Custom vehicle")
                        : entry.DisplayName;
                    var selected = entry;
                    items.Add(new MenuItem(display, MenuAction.None, onActivate: () => SelectLoadoutCustomVehicle(selected, display)));
                }
            }

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.LoadoutVehicleCustom, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.LoadoutVehicleCustom, items, preserveSelection);
        }

        private void SelectLoadoutCustomVehicle(PacketVehiclePackageCatalogEntry entry, string display)
        {
            _state.RoomDrafts.PendingLoadoutVehicle = VehiclePackageRef.Clone(entry.Vehicle);
            _state.RoomDrafts.PendingLoadoutVehicleDisplay = display ?? string.Empty;
            _state.RoomDrafts.PendingLoadoutVehicleSupportsAutomatic = entry.SupportsAutomatic;
            _state.RoomDrafts.PendingLoadoutVehicleSupportsManual = entry.SupportsManual;

            // Skip the transmission prompt when the vehicle only supports one mode (most custom
            // vehicles are manual-only). The catalog carries the supported modes so we can decide
            // this before the package is downloaded. Only ask when both modes are available.
            if (entry.SupportsAutomatic && !entry.SupportsManual)
            {
                SubmitLoadoutReady(true, LocalizationService.Mark("Automatic transmission is required for this vehicle."));
                return;
            }
            if (entry.SupportsManual && !entry.SupportsAutomatic)
            {
                SubmitLoadoutReady(false, LocalizationService.Mark("Manual transmission is required for this vehicle."));
                return;
            }

            _menu.Push(MultiplayerMenuKeys.LoadoutTransmission);
        }
    }
}
