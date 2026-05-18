using System;
using TopSpeed.Data;
using TopSpeed.Physics.Tires.Wear;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private const float MinimumGripScale = 0.45f;
        private const float KphPerMps = 3.6f;
        private const float DriveSlipReferenceMps2 = 6.0f;
        private const float BrakeSlipReferenceMps2 = 9.0f;
        private const float SurfaceHeatingTauSeconds = 45f;
        private const float SurfaceCoolingTauSeconds = 18f;

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
        }

        private void UpdateTireWear(float elapsedSeconds, float speedMps)
        {
            var speed = Math.Max(0f, speedMps);
            var driveSlipNormalized = NormalizeUnit(_lastLongitudinalResult.DriveAccelerationMps2, DriveSlipReferenceMps2);
            var brakeSlipNormalized = NormalizeUnit(_lastLongitudinalResult.BrakeDecelKph / KphPerMps, BrakeSlipReferenceMps2);
            var longitudinalSlipNormalized = Clamp01((driveSlipNormalized * 0.55f) + (brakeSlipNormalized * 0.45f));
            var loadNormalized = ResolveLoadNormalized(_massKg, _lastLateralLoadRatio, longitudinalSlipNormalized);
            var rollingResistanceNormalized = ResolveRollingResistanceNormalized(
                _rollingResistanceCoefficient,
                _currentSurfaceRollingResistanceFactor,
                speed);

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
                    wetnessNormalized: wetness));
            _tireWearState = _tireWearRuntime.State;
        }

        private float ResolveTireWearTractionScale()
        {
            return Clamp(_tireWearRuntime.TractionGripScale, MinimumGripScale, 1f);
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

        private static float ResolveLoadNormalized(float massKg, float lateralLoadRatio, float longitudinalSlipNormalized)
        {
            var massNormalized = Clamp01((massKg - 700f) / 1800f);
            return Clamp01(
                (lateralLoadRatio * 0.56f)
                + (longitudinalSlipNormalized * 0.24f)
                + (massNormalized * 0.20f));
        }

        private static float ResolveRollingResistanceNormalized(float rollingResistanceCoefficient, float surfaceRollingResistanceFactor, float speedMps)
        {
            var speedFactor = Clamp01(speedMps / 45f);
            var normalizedRollingCoefficient = Clamp01((rollingResistanceCoefficient * Math.Max(0.1f, surfaceRollingResistanceFactor)) / 0.030f);
            return Clamp01(normalizedRollingCoefficient * speedFactor);
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

        private static float NormalizeUnit(float value, float reference)
        {
            var normalized = reference > 0f ? value / reference : 0f;
            return Clamp01(normalized);
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
