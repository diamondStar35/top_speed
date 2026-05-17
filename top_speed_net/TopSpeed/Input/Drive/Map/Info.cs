using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void SetRequestInfo(AxisOrButton a)
        {
            _requestInfo = a;
            _settings.SetControllerBinding(DriveIntent.RequestInfo, a);
        }

        public void SetRequestInfo(Key key)
        {
            _kbRequestInfo = key;
            _settings.SetKeyboardBinding(DriveIntent.RequestInfo, key);
        }

        public void SetCurrentGear(AxisOrButton a)
        {
            _currentGear = a;
            _settings.SetControllerBinding(DriveIntent.CurrentGear, a);
        }

        public void SetCurrentGear(Key key)
        {
            _kbCurrentGear = key;
            _settings.SetKeyboardBinding(DriveIntent.CurrentGear, key);
        }

        public void SetCurrentLapNr(AxisOrButton a)
        {
            _currentLapNr = a;
            _settings.SetControllerBinding(DriveIntent.CurrentLapNr, a);
        }

        public void SetCurrentLapNr(Key key)
        {
            _kbCurrentLapNr = key;
            _settings.SetKeyboardBinding(DriveIntent.CurrentLapNr, key);
        }

        public void SetCurrentRacePerc(AxisOrButton a)
        {
            _currentRacePerc = a;
            _settings.SetControllerBinding(DriveIntent.CurrentRacePerc, a);
        }

        public void SetCurrentRacePerc(Key key)
        {
            _kbCurrentRacePerc = key;
            _settings.SetKeyboardBinding(DriveIntent.CurrentRacePerc, key);
        }

        public void SetCurrentLapPerc(AxisOrButton a)
        {
            _currentLapPerc = a;
            _settings.SetControllerBinding(DriveIntent.CurrentLapPerc, a);
        }

        public void SetCurrentLapPerc(Key key)
        {
            _kbCurrentLapPerc = key;
            _settings.SetKeyboardBinding(DriveIntent.CurrentLapPerc, key);
        }

        public void SetCurrentRaceTime(AxisOrButton a)
        {
            _currentRaceTime = a;
            _settings.SetControllerBinding(DriveIntent.CurrentRaceTime, a);
        }

        public void SetCurrentRaceTime(Key key)
        {
            _kbCurrentRaceTime = key;
            _settings.SetKeyboardBinding(DriveIntent.CurrentRaceTime, key);
        }

        public void SetStartEngine(AxisOrButton a)
        {
            _startEngine = a;
            _settings.SetControllerBinding(DriveIntent.StartEngine, a);
        }

        public void SetStartEngine(Key key)
        {
            _kbStartEngine = key;
            _settings.SetKeyboardBinding(DriveIntent.StartEngine, key);
        }

        public void SetReportDistance(AxisOrButton a)
        {
            _reportDistance = a;
            _settings.SetControllerBinding(DriveIntent.ReportDistance, a);
        }

        public void SetReportDistance(Key key)
        {
            _kbReportDistance = key;
            _settings.SetKeyboardBinding(DriveIntent.ReportDistance, key);
        }

        public void SetReportSpeed(AxisOrButton a)
        {
            _reportSpeed = a;
            _settings.SetControllerBinding(DriveIntent.ReportSpeed, a);
        }

        public void SetReportSpeed(Key key)
        {
            _kbReportSpeed = key;
            _settings.SetKeyboardBinding(DriveIntent.ReportSpeed, key);
        }

        public void SetReportFuel(AxisOrButton a)
        {
            _reportFuel = a;
            _settings.SetControllerBinding(DriveIntent.ReportFuel, a);
        }

        public void SetReportFuel(Key key)
        {
            _kbReportFuel = key;
            _settings.SetKeyboardBinding(DriveIntent.ReportFuel, key);
        }

        public void SetTrackName(AxisOrButton a)
        {
            _trackName = a;
            _settings.SetControllerBinding(DriveIntent.TrackName, a);
        }

        public void SetTrackName(Key key)
        {
            _kbTrackName = key;
            _settings.SetKeyboardBinding(DriveIntent.TrackName, key);
        }

        public void SetPause(AxisOrButton a)
        {
            _pause = a;
            _settings.SetControllerBinding(DriveIntent.Pause, a);
        }

        public void SetPause(Key key)
        {
            _kbPause = key;
            _settings.SetKeyboardBinding(DriveIntent.Pause, key);
        }
    }
}



