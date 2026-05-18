using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        private Dictionary<DriveIntent, DriveIntentBinding> CreateIntentBindings()
        {
            var bindings = new Dictionary<DriveIntent, DriveIntentBinding>();

            void Add(
                DriveIntent intent,
                string label,
                InputScope scope,
                TriggerMode keyboardMode,
                TriggerMode controllerMode,
                Func<Key> getKey,
                Action<Key> setKey,
                Func<AxisOrButton> getAxis,
                Action<AxisOrButton> setAxis,
                bool allowNumpadEnterAlias = false)
            {
                bindings[intent] = new DriveIntentBinding(
                    label,
                    new DriveIntentMeta(scope, keyboardMode, controllerMode, allowNumpadEnterAlias),
                    getKey,
                    setKey,
                    getAxis,
                    setAxis);
                _intentDefinitions.Add(new DriveIntentDefinition(intent, label));
            }

            Add(DriveIntent.SteerLeft, LocalizationService.Mark("Steer left"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbLeft, key => SetLeft(key), () => _left, axis => SetLeft(axis));
            Add(DriveIntent.SteerRight, LocalizationService.Mark("Steer right"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbRight, key => SetRight(key), () => _right, axis => SetRight(axis));
            Add(DriveIntent.Throttle, LocalizationService.Mark("Throttle"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbThrottle, key => SetThrottle(key), () => _throttle, axis => SetThrottle(axis));
            Add(DriveIntent.Brake, LocalizationService.Mark("Brake"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbBrake, key => SetBrake(key), () => _brake, axis => SetBrake(axis));
            Add(DriveIntent.Clutch, LocalizationService.Mark("Clutch"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbClutch, key => SetClutch(key), () => _clutch, axis => SetClutch(axis));
            Add(DriveIntent.GearUp, LocalizationService.Mark("Shift gear up"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbGearUp, key => SetGearUp(key), () => _gearUp, axis => SetGearUp(axis));
            Add(DriveIntent.GearDown, LocalizationService.Mark("Shift gear down"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbGearDown, key => SetGearDown(key), () => _gearDown, axis => SetGearDown(axis));
            Add(DriveIntent.Horn, LocalizationService.Mark("Use horn"), InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbHorn, key => SetHorn(key), () => _horn, axis => SetHorn(axis));
            Add(DriveIntent.RequestInfo, LocalizationService.Mark("Request position information"), InputScope.Auxiliary, TriggerMode.Hold, TriggerMode.Hold, () => _kbRequestInfo, key => SetRequestInfo(key), () => _requestInfo, axis => SetRequestInfo(axis));
            Add(DriveIntent.CurrentGear, LocalizationService.Mark("Current gear"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentGear, key => SetCurrentGear(key), () => _currentGear, axis => SetCurrentGear(axis));
            Add(DriveIntent.CurrentLapNr, LocalizationService.Mark("Current lap number"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentLapNr, key => SetCurrentLapNr(key), () => _currentLapNr, axis => SetCurrentLapNr(axis));
            Add(DriveIntent.CurrentRacePerc, LocalizationService.Mark("Current race percentage"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentRacePerc, key => SetCurrentRacePerc(key), () => _currentRacePerc, axis => SetCurrentRacePerc(axis));
            Add(DriveIntent.CurrentLapPerc, LocalizationService.Mark("Current lap percentage"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentLapPerc, key => SetCurrentLapPerc(key), () => _currentLapPerc, axis => SetCurrentLapPerc(axis));
            Add(DriveIntent.CurrentRaceTime, LocalizationService.Mark("Current race time"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentRaceTime, key => SetCurrentRaceTime(key), () => _currentRaceTime, axis => SetCurrentRaceTime(axis));
            Add(DriveIntent.StartEngine, LocalizationService.Mark("Start the engine"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbStartEngine, key => SetStartEngine(key), () => _startEngine, axis => SetStartEngine(axis), allowNumpadEnterAlias: true);
            Add(DriveIntent.ReportDistance, LocalizationService.Mark("Report distance"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbReportDistance, key => SetReportDistance(key), () => _reportDistance, axis => SetReportDistance(axis));
            Add(DriveIntent.ReportSpeed, LocalizationService.Mark("Report speed"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbReportSpeed, key => SetReportSpeed(key), () => _reportSpeed, axis => SetReportSpeed(axis));
            Add(DriveIntent.ReportFuel, LocalizationService.Mark("Report fuel"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbReportFuel, key => SetReportFuel(key), () => _reportFuel, axis => SetReportFuel(axis));
            Add(DriveIntent.ReportTireState, LocalizationService.Mark("Report tire state"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbReportTireState, key => SetReportTireState(key), () => _reportTireState, axis => SetReportTireState(axis));
            Add(DriveIntent.TrackName, LocalizationService.Mark("Report track name"), InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbTrackName, key => SetTrackName(key), () => _trackName, axis => SetTrackName(axis));
            Add(DriveIntent.Pause, LocalizationService.Mark("Pause"), InputScope.Auxiliary, TriggerMode.Hold, TriggerMode.Hold, () => _kbPause, key => SetPause(key), () => _pause, axis => SetPause(axis));

            return bindings;
        }
    }
}



