using System;
using TopSpeed.Physics.Fuel;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private const float MinUsableFuelLiters = 0.0001f;
        private const float HorsepowerToKilowatts = 0.745699872f;
        private const float TorqueRpmToKwFactor = 1f / 9549.29658551372f;
        private const float FuelWarningMaxPulseIntervalSeconds = 5f;
        private const float FuelWarningMinPulseIntervalSeconds = 1f;

        private void ConfigureFuelModel(VehicleDefinition definition)
        {
            var tankCapacityLiters = SanitizeFinite(
                definition.FuelTankCapacityLiters > 0f
                    ? definition.FuelTankCapacityLiters
                    : VehicleDefinition.FuelTankCapacityDefaultLiters,
                VehicleDefinition.FuelTankCapacityDefaultLiters);
            var displacementLiters = SanitizeFinite(
                definition.EngineDisplacementLiters > 0f
                    ? definition.EngineDisplacementLiters
                    : VehicleDefinition.EngineDisplacementDefaultLiters,
                VehicleDefinition.EngineDisplacementDefaultLiters);
            var referencePowerKw = ResolveFuelReferencePowerKw(definition);

            _fuelConfiguration = new FuelConfig(
                tankCapacityLiters,
                displacementLiters,
                referencePowerKw: referencePowerKw);
            _fuelTankCapacityLiters = _fuelConfiguration.TankCapacityLiters;
            _fuelEngineDisplacementLiters = _fuelConfiguration.EngineDisplacementLiters;
            _fuelState = new FuelRuntimeState(_fuelTankCapacityLiters, 0f);
            _fuelBurnLitersPerHour = 0f;
            _fuelEstimatedRangeMeters = 0f;
            _fuelEfficiencyLitersPer100Km = 0f;
            _fuelEfficiencyMpg = 0f;
            _fuelLow = false;
            _fuelEmpty = false;
            _fuelPowerScale = 1f;
            _fuelWarningPulseTimerSeconds = 0f;

            _baseMassKgAtFullTank = Math.Max(1f, _massKg);
            ApplyFuelMassForRemaining(_fuelState.RemainingLiters);
        }

        private void UpdateFuelModel(float elapsedSeconds)
        {
            var throttleNormalized = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
            var netPowerKw = Math.Max(0f, _engine.NetHorsepower) * HorsepowerToKilowatts;
            var speedMps = Math.Max(0f, _speed / 3.6f);
            var result = FuelRuntime.Step(
                _fuelConfiguration,
                _fuelState,
                new FuelRuntimeInput(
                    elapsedSeconds: Math.Max(0f, elapsedSeconds),
                    combustionActive: _combustionState == EngineCombustionState.On && !_engineStalled,
                    throttleNormalized: throttleNormalized,
                    netPowerKw: netPowerKw,
                    speedMps: speedMps));

            _fuelState = result.State;
            _fuelBurnLitersPerHour = result.BurnLitersPerHour;
            _fuelEstimatedRangeMeters = result.EstimatedRangeMeters;
            _fuelEfficiencyLitersPer100Km = result.EfficiencyLitersPer100Km;
            _fuelEfficiencyMpg = result.EfficiencyMpg;
            _fuelLow = result.LowFuel;
            _fuelEmpty = result.EmptyFuel;
            _fuelPowerScale = result.PowerScale;
            ApplyFuelMassForRemaining(_fuelState.RemainingLiters);

            if (_fuelEmpty && _combustionState == EngineCombustionState.On && !_engineStalled)
                StallEngine(playFailureCue: false);

            UpdateFuelWarningPulse(Math.Max(0f, elapsedSeconds), result.FuelPercent);
        }

        private void ApplyFuelMassForRemaining(float remainingLiters)
        {
            var clampedRemaining = Math.Max(0f, Math.Min(_fuelTankCapacityLiters, remainingLiters));
            var consumedLiters = Math.Max(0f, _fuelTankCapacityLiters - clampedRemaining);
            var consumedMassKg = consumedLiters * _fuelConfiguration.FuelDensityKgPerLiter;
            var effectiveMassKg = _baseMassKgAtFullTank - consumedMassKg;
            _massKg = Math.Max(1f, effectiveMassKg);
        }

        private bool CanStartEngineWithFuel()
        {
            return _fuelState.RemainingLiters > MinUsableFuelLiters;
        }

        private void UpdateFuelWarningPulse(float elapsedSeconds, float fuelPercent)
        {
            var shouldPulse = _fuelLow
                && !_fuelEmpty
                && _combustionState == EngineCombustionState.On
                && !_engineStalled
                && _state == CarState.Running;
            if (!shouldPulse)
            {
                _fuelWarningPulseTimerSeconds = 0f;
                if (_soundFuelWarning.IsPlaying)
                    _soundFuelWarning.Stop();
                return;
            }

            _fuelWarningPulseTimerSeconds -= elapsedSeconds;
            if (_fuelWarningPulseTimerSeconds > 0f)
                return;
            if (_soundFuelWarning.IsPlaying)
                return;

            _soundFuelWarning.SeekToStart();
            _soundFuelWarning.Play(loop: false);
            _fuelWarningPulseTimerSeconds = ResolveFuelWarningPulseIntervalSeconds(fuelPercent);
        }

        private float ResolveFuelWarningPulseIntervalSeconds(float fuelPercent)
        {
            var lowFuelFraction = Math.Max(0.0001f, _fuelConfiguration.LowFuelFraction);
            var clampedPercent = Math.Max(0f, Math.Min(lowFuelFraction, fuelPercent));
            var urgency = 1f - (clampedPercent / lowFuelFraction);
            if (urgency < 0f)
                urgency = 0f;
            if (urgency > 1f)
                urgency = 1f;

            return FuelWarningMaxPulseIntervalSeconds
                + ((FuelWarningMinPulseIntervalSeconds - FuelWarningMaxPulseIntervalSeconds) * urgency);
        }

        private static float ResolveFuelReferencePowerKw(VehicleDefinition definition)
        {
            var referencePowerKw = 0f;
            if (definition.TorqueCurveRpm != null && definition.TorqueCurveTorqueNm != null)
            {
                var count = Math.Min(definition.TorqueCurveRpm.Length, definition.TorqueCurveTorqueNm.Length);
                for (var i = 0; i < count; i++)
                {
                    var curveRpm = Math.Max(0f, definition.TorqueCurveRpm[i]);
                    var curveTorque = Math.Max(0f, definition.TorqueCurveTorqueNm[i]);
                    referencePowerKw = Math.Max(referencePowerKw, curveTorque * curveRpm * TorqueRpmToKwFactor);
                }
            }

            referencePowerKw = Math.Max(
                referencePowerKw,
                Math.Max(0f, definition.PeakTorqueNm) * Math.Max(0f, definition.PeakTorqueRpm) * TorqueRpmToKwFactor);
            referencePowerKw = Math.Max(
                referencePowerKw,
                Math.Max(0f, definition.RedlineTorqueNm)
                * Math.Max(0f, Math.Max(definition.RevLimiter, definition.MaxRpm))
                * TorqueRpmToKwFactor);

            if (referencePowerKw <= 0f)
                referencePowerKw = 180f;

            return Math.Max(20f, Math.Min(1000f, referencePowerKw));
        }

        private void PlayFuelStartBlockedCue()
        {
            if (!_soundBadSwitch.IsPlaying)
                _soundBadSwitch.Play(loop: false);
        }
    }
}
