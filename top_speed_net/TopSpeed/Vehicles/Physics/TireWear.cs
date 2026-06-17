using System;
using TopSpeed.Data;
using TopSpeed.Physics.Tires.Wear;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private const float MinimumGripScale = 0.45f;
        private const float KphPerMps = 3.6f;
        private const float SurfaceHeatingTauSeconds = 45f;
        private const float SurfaceCoolingTauSeconds = 18f;
        // Low-pass on the longitudinal heat-stress signals so on/off (digital)
        // throttle/brake taps converge to the same heat as a smooth analog hold
        // of the equivalent average — parity across input methods.
        private const float HeatStressSmoothingTauSeconds = 0.8f;

        private float _smoothedAccelHeatStress;
        private float _smoothedBrakeHeatStress;
        private float _smoothedEngineBrakeHeatStress;

        private void ResetTireWearState()
        {
            var weather = _track.GetActiveWeatherProfile();
            var ambientTemperatureC = Clamp(weather.TemperatureC, -40f, 80f);
            var wetness = ResolveWeatherWetness(weather);
            _surfaceTemperatureC = ResolveInitialSurfaceTemperature(_surface, ambientTemperatureC, wetness);
            var initialTemperatureC = TireWearRuntime.ResolveInitialTemperature(_tireWearConfig, ambientTemperatureC, _surfaceTemperatureC);
            _tireWearState = TireWearDefaults.CreateInitialState(initialTemperatureC);
            _tireWearRuntime = TireWearRuntime.Resolve(_tireWearConfig, _tireWearState);
            _lastLongitudinalResult = default;
            _lastLateralLoadRatio = 0f;
            _lastSlipAngleNormalized = 0f;
            _lastLateralSlipNormalized = 0f;
            _smoothedAccelHeatStress = 0f;
            _smoothedBrakeHeatStress = 0f;
            _smoothedEngineBrakeHeatStress = 0f;
        }

        private void UpdateTireWear(float elapsedSeconds, float speedMps)
        {
            var speed = Math.Max(0f, speedMps);
            var longitudinalSlipNormalized = TireWearInputSignals.ResolveLongitudinalSlipNormalized(
                _lastLongitudinalResult.DriveAccelerationMps2,
                _lastLongitudinalResult.BrakeDecelKph / KphPerMps);
            var loadNormalized = TireWearInputSignals.ResolveLoadNormalized(
                _massKg,
                _lastLateralLoadRatio,
                longitudinalSlipNormalized);
            var rollingResistanceNormalized = TireWearInputSignals.ResolveRollingResistanceNormalized(
                _rollingResistanceCoefficient,
                _currentSurfaceRollingResistanceFactor,
                speed);

            // Separate, smoothed acceleration / brake / engine-brake heat-stress
            // signals (the merged longitudinal signal above still drives wear and
            // load). Smoothing gives digital tappers the same heat as an analog
            // hold of the equivalent average.
            var heatStressAlpha = ResolveExpAlpha(elapsedSeconds, HeatStressSmoothingTauSeconds);
            var accelHeatStress = TireWearInputSignals.ResolveAccelerationHeatStressNormalized(
                _lastLongitudinalResult.DriveAccelerationMps2);
            var brakeHeatStress = TireWearInputSignals.ResolveBrakeHeatStressNormalized(
                _lastLongitudinalResult.BrakeDecelKph / KphPerMps);
            var engineBrakeHeatStress = TireWearInputSignals.ResolveEngineBrakeHeatStressNormalized(
                _lastLongitudinalResult.EngineBrakeDecelKph / KphPerMps);
            _smoothedAccelHeatStress += (accelHeatStress - _smoothedAccelHeatStress) * heatStressAlpha;
            _smoothedBrakeHeatStress += (brakeHeatStress - _smoothedBrakeHeatStress) * heatStressAlpha;
            _smoothedEngineBrakeHeatStress += (engineBrakeHeatStress - _smoothedEngineBrakeHeatStress) * heatStressAlpha;

            var weather = _track.GetActiveWeatherProfile();
            var wetness = ResolveWeatherWetness(weather);
            UpdateSurfaceTemperature(elapsedSeconds, weather, wetness);

            _tireWearRuntime = TireWearRuntime.Step(
                _tireWearConfig,
                _tireWearState,
                new TireWearInput(
                    elapsedSeconds,
                    speed,
                    slipAngleNormalized: _lastSlipAngleNormalized,
                    lateralSlipNormalized: _lastLateralSlipNormalized,
                    longitudinalSlipNormalized: longitudinalSlipNormalized,
                    loadNormalized: loadNormalized,
                    rollingResistanceNormalized: rollingResistanceNormalized,
                    ambientTemperatureC: weather.TemperatureC,
                    surfaceTemperatureC: _surfaceTemperatureC,
                    wetnessNormalized: wetness,
                    accelerationHeatStressNormalized: _smoothedAccelHeatStress,
                    brakeHeatStressNormalized: _smoothedBrakeHeatStress,
                    engineBrakeHeatStressNormalized: _smoothedEngineBrakeHeatStress));
            _tireWearState = _tireWearRuntime.State;
        }

        private float ResolveTireWearTractionScale(
            float speedMps,
            int steeringInput,
            float slipAngleNormalized,
            float lateralSlipNormalized)
        {
            return TireTractionAssist.ResolveStraightLineTractionScale(
                baseTractionScale: Clamp(_tireWearRuntime.TractionGripScale, MinimumGripScale, 1f),
                speedMps: speedMps,
                steeringInput: steeringInput,
                slipAngleNormalized: slipAngleNormalized,
                lateralSlipNormalized: lateralSlipNormalized,
                wearFraction: _tireWearState.WearFraction,
                overheatNormalized: _tireWearRuntime.OverheatNormalized);
        }

        private float ResolveTireWearLateralScale()
        {
            return Clamp(_tireWearRuntime.LateralGripScale, MinimumGripScale, 1f);
        }

        private float ResolveTireWearBrakeScale()
        {
            return Clamp(_tireWearRuntime.BrakeGripScale, MinimumGripScale, 1f);
        }

        private void UpdateSurfaceTemperature(float elapsedSeconds, in TrackWeatherProfile weather, float wetness)
        {
            var ambientTemperatureC = Clamp(weather.TemperatureC, -40f, 80f);
            var windMagnitude = Math.Abs(weather.LongitudinalWindMps) + Math.Abs(weather.LateralWindMps);
            var windCooling = Clamp01(windMagnitude / 22f);
            var stormCooling = Clamp01(weather.StormGain);
            var dryLift = ResolveSurfaceDryHeatLift(_surface);
            var dryFactor = Clamp01(1f - (wetness * 0.70f) - (stormCooling * 0.30f));
            var targetSurfaceTemperatureC = ambientTemperatureC
                + (dryLift * dryFactor)
                - (wetness * 6f)
                - (windCooling * 2.5f)
                - (stormCooling * 1.5f);

            if (!IsFinite(_surfaceTemperatureC))
                _surfaceTemperatureC = targetSurfaceTemperatureC;

            var tau = targetSurfaceTemperatureC >= _surfaceTemperatureC
                ? SurfaceHeatingTauSeconds
                : SurfaceCoolingTauSeconds;
            var alpha = ResolveExpAlpha(Math.Max(0f, elapsedSeconds), tau);
            _surfaceTemperatureC += (targetSurfaceTemperatureC - _surfaceTemperatureC) * alpha;
            _surfaceTemperatureC = Clamp(_surfaceTemperatureC, ambientTemperatureC - 35f, ambientTemperatureC + 75f);
        }

        private static float ResolveInitialSurfaceTemperature(TrackSurface surface, float ambientTemperatureC, float wetness)
        {
            var dryLift = ResolveSurfaceDryHeatLift(surface);
            var dryFactor = Clamp01(1f - (wetness * 0.70f));
            var initialSurfaceTemperatureC = ambientTemperatureC + (dryLift * dryFactor) - (wetness * 3f);
            return Clamp(initialSurfaceTemperatureC, ambientTemperatureC - 12f, ambientTemperatureC + 35f);
        }

        private static float ResolveSurfaceDryHeatLift(TrackSurface surface)
        {
            switch (surface)
            {
                case TrackSurface.Asphalt:
                    return 7f;
                case TrackSurface.Gravel:
                    return 3f;
                case TrackSurface.Water:
                    return -2f;
                case TrackSurface.Sand:
                    return 12f;
                case TrackSurface.Snow:
                    return -10f;
                default:
                    return 4f;
            }
        }

        private static float ResolveWeatherWetness(in TrackWeatherProfile weather)
        {
            var rain = Clamp01(weather.RainGain);
            var storm = Clamp01(weather.StormGain);
            return Clamp01(rain + (storm * 0.55f));
        }

        private static float ResolveExpAlpha(float elapsedSeconds, float timeConstantSeconds)
        {
            var tau = Math.Max(0.0001f, timeConstantSeconds);
            var alpha = 1f - (float)Math.Exp(-elapsedSeconds / tau);
            return Clamp01(alpha);
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
