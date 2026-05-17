using System;
using TopSpeed.Localization;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
    {
        private string GetVehicleName()
        {
            if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                return TopSpeed.Drive.Session.SessionText.FormatVehicleName(_car.CustomFile);

            return _car.VehicleName;
        }

        private string GetPlayerNameForPlayer(int playerIndex)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("Player {0}"),
                playerIndex + 1);
        }

        private int CalculatePlayerPerc(int player)
        {
            if (player != 0 || _track.Length <= 0 || _nrOfLaps <= 0)
                return 0;

            var percent = (int)((_car.PositionY / (_track.Length * (float)_nrOfLaps)) * 100.0f);
            if (percent < 0)
                return 0;
            if (percent > 100)
                return 100;
            return percent;
        }
    }
}
