using System;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
    {
        private string GetVehicleName()
        {
            if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                return TopSpeed.Drive.Session.SessionText.FormatVehicleName(_car.CustomFile);

            return _car.VehicleName;
        }

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == _playerNumber)
                return GetVehicleName();

            if (playerIndex < _playerNumber)
            {
                var bot = _computerPlayers[playerIndex];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }
            else if (playerIndex > _playerNumber)
            {
                var bot = _computerPlayers[playerIndex - 1];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }

            return LocalizationService.Mark("Vehicle");
        }

        private string GetPlayerNameForPlayer(int playerIndex)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("Player {0}"),
                playerIndex + 1);
        }

        private int CalculatePlayerPerc(int player)
        {
            if (player == _playerNumber)
                return Math.Min(100, (int)((_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f));

            if (player > _playerNumber)
                return Math.Min(100, (int)((_computerPlayers[player - 1]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f));

            return Math.Min(100, (int)((_computerPlayers[player]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f));
        }
    }
}
