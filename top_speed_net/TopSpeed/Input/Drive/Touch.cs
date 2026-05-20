using System;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void SetTouchInputState(
            int steering,
            int throttle,
            int brake,
            int clutch,
            bool horn,
            bool gearUp,
            bool gearDown,
            bool startEngine,
            bool reportDistance = false,
            bool reportSpeed = false,
            bool currentGear = false,
            bool currentLapNr = false,
            bool currentRacePerc = false,
            bool currentLapPerc = false,
            bool currentRaceTime = false,
            bool pause = false,
            bool requestInfo = false,
            bool previousPlayerInfo = false,
            bool nextPlayerInfo = false,
            bool repeatPlayerInfo = false,
            bool reportFuel = false,
            bool reportTireState = false,
            bool pitThisTime = false)
        {
            _touchSteering = ClampRange(steering, -100, 100);
            _touchThrottle = ClampRange(throttle, 0, 100);
            _touchBrake = ClampRange(brake, -100, 0);
            _touchClutch = ClampRange(clutch, 0, 100);
            _touchHorn = horn;
            _touchGearUp = gearUp;
            _touchGearDown = gearDown;
            _touchStartEngine = startEngine;
            _touchReportDistance = reportDistance;
            _touchReportSpeed = reportSpeed;
            _touchReportFuel = reportFuel;
            _touchReportTireState = reportTireState;
            _touchCurrentGear = currentGear;
            _touchCurrentLapNr = currentLapNr;
            _touchCurrentRacePerc = currentRacePerc;
            _touchCurrentLapPerc = currentLapPerc;
            _touchCurrentRaceTime = currentRaceTime;
            _touchPause = pause;
            _touchRequestInfo = requestInfo;
            _touchPreviousPlayerInfo = previousPlayerInfo;
            _touchNextPlayerInfo = nextPlayerInfo;
            _touchRepeatPlayerInfo = repeatPlayerInfo;
            _touchPit = pitThisTime;
        }

        public void ClearTouchInputState()
        {
            _touchSteering = 0;
            _touchThrottle = 0;
            _touchBrake = 0;
            _touchClutch = 0;
            _touchHorn = false;
            _touchGearUp = false;
            _touchGearDown = false;
            _touchStartEngine = false;
            _touchReportDistance = false;
            _touchReportSpeed = false;
            _touchReportFuel = false;
            _touchReportTireState = false;
            _touchCurrentGear = false;
            _touchCurrentLapNr = false;
            _touchCurrentRacePerc = false;
            _touchCurrentLapPerc = false;
            _touchCurrentRaceTime = false;
            _touchPause = false;
            _touchRequestInfo = false;
            _touchPreviousPlayerInfo = false;
            _touchNextPlayerInfo = false;
            _touchRepeatPlayerInfo = false;
            _touchPit = false;
        }

        private static int ClampRange(int value, int min, int max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
