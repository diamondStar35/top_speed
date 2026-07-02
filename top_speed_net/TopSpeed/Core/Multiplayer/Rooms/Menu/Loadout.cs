using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildLoadoutVehicleMenu()
        {
            var items = new List<MenuItem>();
            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var vehicleIndex = i;
                var vehicleName = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(vehicleName, MenuAction.None, onActivate: () => CompleteLoadoutVehicleSelection(vehicleIndex)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Custom"), MenuAction.None, onActivate: OpenLoadoutVehicleCustomMenu));
            items.Add(new MenuItem(LocalizationService.Mark("Random vehicle"), MenuAction.None, onActivate: () => CompleteLoadoutVehicleSelection(Algorithm.RandomInt(VehicleCatalog.VehicleCount))));
            _menu.UpdateItems(MultiplayerMenuKeys.LoadoutVehicle, items);
        }

        private void RebuildLoadoutTransmissionMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Automatic transmission"), MenuAction.None, onActivate: () => SubmitLoadoutReady(true)),
                new MenuItem(LocalizationService.Mark("Manual transmission"), MenuAction.None, onActivate: () => SubmitLoadoutReady(false)),
                new MenuItem(LocalizationService.Mark("Random transmission mode"), MenuAction.None, onActivate: () => SubmitLoadoutReady(PickRandomLoadoutTransmission(_state.RoomDrafts.PendingLoadoutVehicleIndex)))
            };
            _menu.UpdateItems(MultiplayerMenuKeys.LoadoutTransmission, items);
        }
    }
}
