using System;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void SubmitLoadoutReady(bool automaticTransmission)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not in a game room."));
                return;
            }

            var pendingVehicle = _state.RoomDrafts.PendingLoadoutVehicle;
            if (pendingVehicle != null && pendingVehicle.IsCustomPackage)
            {
                if (!TrySend(session.SendRoomPlayerReady(CarType.CustomVehicle, automaticTransmission, pendingVehicle), LocalizationService.Mark("ready state")))
                    return;
                _speech.Speak(LocalizationService.Mark("Ready. Waiting for other players."));
                _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
                return;
            }

            var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _state.RoomDrafts.PendingLoadoutVehicleIndex));
            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            if (!TransmissionSelect.TryResolveRequested(
                    automaticRequested: automaticTransmission,
                    primary: parameters.PrimaryTransmissionType,
                    supported: parameters.SupportedTransmissionTypes,
                    out _))
            {
                _speech.Speak(LocalizationService.Mark("This vehicle does not support the selected transmission mode."));
                return;
            }

            var selectedCar = (CarType)vehicleIndex;
            _setLocalMultiplayerLoadout(vehicleIndex, automaticTransmission);
            if (!TrySend(session.SendRoomPlayerReady(selectedCar, automaticTransmission, VehiclePackageRef.None()), LocalizationService.Mark("ready state")))
                return;
            _speech.Speak(LocalizationService.Mark("Ready. Waiting for other players."));
            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
        }

        private void CompleteLoadoutVehicleSelection(int vehicleIndex)
        {
            vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            _state.RoomDrafts.PendingLoadoutVehicleIndex = vehicleIndex;
            _state.RoomDrafts.PendingLoadoutVehicle = null;
            if (TryResolveSingleLoadoutTransmission(vehicleIndex, out var automaticTransmission))
            {
                SubmitLoadoutReady(automaticTransmission);
                return;
            }

            _menu.Push(MultiplayerMenuKeys.LoadoutTransmission);
        }

        private static bool TryResolveSingleLoadoutTransmission(int vehicleIndex, out bool automaticTransmission)
        {
            automaticTransmission = true;
            vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            return TransmissionSelect.TryResolveSingleMode(
                parameters.PrimaryTransmissionType,
                parameters.SupportedTransmissionTypes,
                out automaticTransmission);
        }

        private bool PickRandomLoadoutTransmission(int vehicleIndex)
        {
            vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var supportsAutomatic = TransmissionSelect.SupportsAutomatic(parameters.SupportedTransmissionTypes);
            var supportsManual = TransmissionSelect.SupportsManual(parameters.SupportedTransmissionTypes);
            if (supportsAutomatic && supportsManual)
                return Algorithm.RandomInt(2) == 0;
            if (supportsManual)
                return false;
            return true;
        }
    }
}
