using System;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class CoreRequests : Subsystem
    {
        private const float KmToMiles = 0.621371f;
        private const float MetersPerMile = 1609.344f;
        private const float MetersToFeet = 3.28084f;
        private const float LitersToGallons = 0.264172052f;

        private readonly DriveInput _input;
        private readonly DriveSettings _settings;
        private readonly ICar _car;
        private readonly Track _track;
        private readonly Func<bool> _isStarted;
        private readonly Func<int> _getLap;
        private readonly Func<int> _getLapLimit;
        private readonly Func<int> _getRaceTimeMs;
        private readonly Func<bool> _canToggleShiftOnDemand;
        private readonly Func<bool>? _isInPitStop;
        private readonly Action<string> _speakText;

        public CoreRequests(
            string name,
            int order,
            DriveInput input,
            DriveSettings settings,
            ICar car,
            Track track,
            Func<bool> isStarted,
            Func<int> getLap,
            Func<int> getLapLimit,
            Func<int> getRaceTimeMs,
            Action<string> speakText,
            Func<bool>? canToggleShiftOnDemand = null,
            Func<bool>? isInPitStop = null)
            : base(name, order)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _getLapLimit = getLapLimit ?? throw new ArgumentNullException(nameof(getLapLimit));
            _getRaceTimeMs = getRaceTimeMs ?? throw new ArgumentNullException(nameof(getRaceTimeMs));
            _canToggleShiftOnDemand = canToggleShiftOnDemand ?? (() => true);
            _isInPitStop = isInPitStop;
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
        }

        public override void Update(SessionContext context, float elapsed)
        {
            HandleEngineStartRequest();
            HandleShiftOnDemandToggleRequest();
            HandleCurrentGearRequest();
            HandleCurrentLapNumberRequest();
            HandleCurrentRacePercentageRequest();
            HandleCurrentLapPercentageRequest();
            HandleCurrentRaceTimeRequest();
            HandleTrackNameRequest();
            HandleSpeedReportRequest();
            HandleFuelReportRequest();
            HandleDistanceReportRequest();
        }

        private bool IsActiveLapRange()
        {
            return _isStarted() && _getLap() <= _getLapLimit();
        }

        private void HandleEngineStartRequest()
        {
            if (_isInPitStop?.Invoke() == true)
                return;
            if (!_input.Intents.IsTriggered(DriveIntent.StartEngine) || !_isStarted())
                return;
            if (_car.State == CarState.Crashing || _car.State == CarState.Starting || _car.State == CarState.Stopping)
                return;

            if (_car.CombustionActive)
            {
                _car.ShutdownEngine();
                return;
            }

            if (_car.State == CarState.Crashed)
                _car.RestartAfterCrash();
            else if (_car.State == CarState.Stopped)
                _car.Start();
            else
                _car.RestartFromStall();
        }

        private void HandleShiftOnDemandToggleRequest()
        {
            if (!_input.GetToggleShiftOnDemand() || !IsActiveLapRange() || !_canToggleShiftOnDemand())
                return;
            if (!_car.ToggleShiftOnDemand())
                return;

            _speakText(_car.ShiftOnDemandEnabled
                ? LocalizationService.Mark("shift on demand")
                : LocalizationService.Mark("automatic"));
        }

        private void HandleCurrentGearRequest()
        {
            if (_input.Intents.IsTriggered(DriveIntent.CurrentGear) && IsActiveLapRange())
                _speakText(LocalizationService.Format(LocalizationService.Mark("Gear {0}"), SessionText.FormatGearCode(_car)));
        }

        private void HandleCurrentLapNumberRequest()
        {
            if (_input.Intents.IsTriggered(DriveIntent.CurrentLapNr) && _isStarted() && _getLap() <= _getLapLimit())
                _speakText(LocalizationService.Format(LocalizationService.Mark("Lap {0}"), _getLap()));
        }

        private void HandleCurrentRacePercentageRequest()
        {
            if (_input.Intents.IsTriggered(DriveIntent.CurrentRacePerc) && _isStarted() && _getLap() <= _getLapLimit())
            {
                var trackLength = _track.Length;
                var lapLimit = _getLapLimit();
                var percent = trackLength > 0f && lapLimit > 0
                    ? (int)((_car.PositionY / (trackLength * lapLimit)) * 100.0f)
                    : 0;
                if (percent < 0)
                    percent = 0;
                if (percent > 100)
                    percent = 100;
                _speakText(SessionText.FormatRacePercentage(percent));
            }
        }

        private void HandleCurrentLapPercentageRequest()
        {
            if (_input.Intents.IsTriggered(DriveIntent.CurrentLapPerc) && _isStarted() && _getLap() <= _getLapLimit())
            {
                var lap = _getLap();
                var trackLength = _track.Length;
                var percent = trackLength > 0f
                    ? (int)(((_car.PositionY - (trackLength * (lap - 1))) / trackLength) * 100.0f)
                    : 0;
                if (percent < 0)
                    percent = 0;
                if (percent > 100)
                    percent = 100;
                _speakText(SessionText.FormatLapPercentage(percent));
            }
        }

        private void HandleCurrentRaceTimeRequest()
        {
            if (_input.Intents.IsTriggered(DriveIntent.CurrentRaceTime) && _isStarted())
                _speakText(LocalizationService.Format(LocalizationService.Mark("Race time {0}"), SessionText.FormatTime(_getRaceTimeMs(), detailed: false)));
        }

        private void HandleTrackNameRequest()
        {
            if (_input.Intents.IsTriggered(DriveIntent.TrackName))
                _speakText(SessionText.FormatTrackName(_track.TrackName));
        }

        private void HandleSpeedReportRequest()
        {
            if (!_input.Intents.IsTriggered(DriveIntent.ReportSpeed) || !IsActiveLapRange())
                return;

            var speedKmh = _car.SpeedKmh;
            var rpm = _car.EngineRpm;
            var horsepower = _car.EngineNetHorsepower;
            if (_settings.Units == UnitSystem.Imperial)
            {
                _speakText(LocalizationService.Format(
                    LocalizationService.Mark("{0:F0} miles per hour, {1:F0} RPM, {2:F0} horsepower"),
                    speedKmh * KmToMiles,
                    rpm,
                    horsepower));
            }
            else
            {
                _speakText(LocalizationService.Format(
                    LocalizationService.Mark("{0:F0} kilometers per hour, {1:F0} RPM, {2:F0} horsepower"),
                    speedKmh,
                    rpm,
                    horsepower));
            }
        }

        private void HandleFuelReportRequest()
        {
            if (!_input.Intents.IsTriggered(DriveIntent.ReportFuel) || !IsActiveLapRange())
                return;

            _speakText(BuildFuelStatusPhrase());
        }

        private void HandleDistanceReportRequest()
        {
            if (!_input.Intents.IsTriggered(DriveIntent.ReportDistance) || !IsActiveLapRange())
                return;

            var distanceM = _car.DistanceMeters;
            if (_settings.Units == UnitSystem.Imperial)
            {
                var distanceMiles = distanceM / MetersPerMile;
                if (distanceMiles >= 1f)
                    _speakText(LocalizationService.Format(LocalizationService.Mark("{0:F1} miles traveled"), distanceMiles));
                else
                    _speakText(LocalizationService.Format(LocalizationService.Mark("{0:F0} feet traveled"), distanceM * MetersToFeet));
            }
            else
            {
                var distanceKm = distanceM / 1000f;
                if (distanceKm >= 1f)
                    _speakText(LocalizationService.Format(LocalizationService.Mark("{0:F1} kilometers traveled"), distanceKm));
                else
                    _speakText(LocalizationService.Format(LocalizationService.Mark("{0:F0} meters traveled"), distanceM));
            }
        }

        private string BuildFuelStatusPhrase()
        {
            var tankLiters = Math.Max(0f, _car.FuelTankCapacityLiters);
            var remainingLiters = Math.Max(0f, Math.Min(tankLiters, _car.FuelLitersRemaining));
            var fuelPercent = tankLiters > 0f
                ? Math.Max(0f, Math.Min(100f, (remainingLiters / tankLiters) * 100f))
                : 0f;

            if (_settings.Units == UnitSystem.Imperial)
            {
                var remainingGallons = remainingLiters * LitersToGallons;
                var mpg = _car.FuelEfficiencyMpg;
                if (mpg > 0.05f)
                {
                    return LocalizationService.Format(
                        LocalizationService.Mark("fuel {0:F1} gallons, {1:F0} percent, {2:F1} miles per gallon"),
                        remainingGallons,
                        fuelPercent,
                        mpg);
                }

                return LocalizationService.Format(
                    LocalizationService.Mark("fuel {0:F1} gallons, {1:F0} percent"),
                    remainingGallons,
                    fuelPercent);
            }

            var litersPer100Km = _car.FuelEfficiencyLitersPer100Km;
            if (litersPer100Km > 0.05f)
            {
                return LocalizationService.Format(
                    LocalizationService.Mark("fuel {0:F1} liters, {1:F0} percent, {2:F1} liters per 100 kilometers"),
                    remainingLiters,
                    fuelPercent,
                    litersPer100Km);
            }

            return LocalizationService.Format(
                LocalizationService.Mark("fuel {0:F1} liters, {1:F0} percent"),
                remainingLiters,
                fuelPercent);
        }
    }
}
