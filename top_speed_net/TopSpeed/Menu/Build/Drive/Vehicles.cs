using System;
using System.Collections.Generic;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildVehicleMenu(string id, DriveMode mode)
        {
            var items = new List<MenuItem>();

            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var index = i;
                var name = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(name, MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, () => _selection.SelectVehicle(index))));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Custom"), MenuAction.None, onActivate: () => OpenCustomVehicleMenuOrAnnounce(mode)));
            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, _selection.SelectRandomVehicle)));
            return BackMenu(id, items, LocalizationService.Mark("Select a vehicle"));
        }

        private MenuScreen BuildCustomVehicleMenu(string id, DriveMode mode)
        {
            return BackMenu(id, BuildCustomVehicleItems(mode), LocalizationService.Mark("Select a custom vehicle"));
        }

        private void RefreshCustomVehicleMenu(DriveMode mode)
        {
            _menu.UpdateItems(CustomVehicleMenuId(mode), BuildCustomVehicleItems(mode));
        }

        private List<MenuItem> BuildCustomVehicleItems(DriveMode mode)
        {
            var items = new List<MenuItem>();
            var customVehicles = _selection.GetCustomVehicleInfo();
            if (customVehicles.Count == 0)
                return items;

            foreach (var vehicle in customVehicles)
            {
                var filePath = vehicle.Key;
                var displayName = string.IsNullOrWhiteSpace(vehicle.Display)
                    ? LocalizationService.Mark("Custom vehicle")
                    : vehicle.Display;
                items.Add(new MenuItem(displayName, MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, () => _selection.SelectCustomVehicle(filePath))));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, _selection.SelectRandomCustomVehicle)));
            return items;
        }

        private void CompleteVehicleSelection(DriveMode mode, Action selectVehicle)
        {
            selectVehicle();
            if (TryResolveSingleTransmissionMode(out var forcedMode))
            {
                _setup.Transmission = forcedMode;
                _ui.SpeakMessage(forcedMode == TransmissionMode.Automatic
                    ? LocalizationService.Mark("Automatic transmission is required for this vehicle.")
                    : LocalizationService.Mark("Manual transmission is required for this vehicle."));
                _driveActions.QueueDriveStart(mode);
                return;
            }

            _menu.Push(TransmissionMenuId(mode));
        }

        private void OpenCustomVehicleMenuOrAnnounce(DriveMode mode)
        {
            var customVehicles = _selection.GetCustomVehicleInfo();
            var issues = _selection.ConsumeCustomVehicleIssues();
            if (customVehicles.Count == 0)
            {
                if (issues.Count > 0)
                {
                    _ui.ShowMessageDialog(
                        LocalizationService.Mark("Custom vehicle errors"),
                        LocalizationService.Mark("Some custom vehicle files are invalid and were skipped."),
                        issues);
                }
                else
                {
                    _ui.SpeakMessage(LocalizationService.Mark("No custom vehicles found."));
                }
                return;
            }

            RefreshCustomVehicleMenu(mode);
            _menu.Push(CustomVehicleMenuId(mode));

            if (issues.Count > 0)
            {
                _ui.ShowMessageDialog(
                    LocalizationService.Mark("Custom vehicle issues"),
                    LocalizationService.Mark("Some custom vehicle files reported issues. Vehicles with errors were skipped; vehicles with warnings still loaded."),
                    issues);
            }
        }
    }
}


