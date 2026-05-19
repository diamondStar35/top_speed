using System;
using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private const float SurfaceHeatingTauSeconds = 45f;
        private const float SurfaceCoolingTauSeconds = 18f;

        private static float Clamp(float v, float min, float max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }

        private static float Clamp01(float v)
        {
            return Clamp(v, 0f, 1f);
        }

        private static float ResolveForwardSafetySpeedKph(float referenceTopSpeedKph)
        {
            var referenceTopSpeed = Math.Max(1f, referenceTopSpeedKph);
            var scaledSafetySpeed = referenceTopSpeed * 1.08f;
            var safetySpeed = Math.Min(550f, scaledSafetySpeed);
            return Math.Max(5f, safetySpeed);
        }

        private static float DegToRad(float deg)
        {
            return (float)(Math.PI / 180.0) * deg;
        }

        private static float ResolveAmbientTemperatureC(float temperatureC, float fallback)
        {
            if (!IsFinite(temperatureC))
                return fallback;
            return Clamp(temperatureC, -40f, 80f);
        }

        private static float ResolveWeatherWetness(float rainGain, float stormGain)
        {
            var rain = Clamp01(rainGain);
            var storm = Clamp01(stormGain);
            return Clamp01(rain + (storm * 0.55f));
        }

        private static float StepSurfaceTemperature(
            float currentSurfaceTemperatureC,
            float elapsedSeconds,
            TrackSurface surface,
            float ambientTemperatureC,
            float rainGain,
            float stormGain,
            float windGain)
        {
            var wetness = ResolveWeatherWetness(rainGain, stormGain);
            var windCooling = Clamp01(Math.Abs(windGain));
            var stormCooling = Clamp01(stormGain);
            var dryLift = ResolveSurfaceDryHeatLift(surface);
            var dryFactor = Clamp01(1f - (wetness * 0.70f) - (stormCooling * 0.30f));
            var targetSurfaceTemperatureC = ambientTemperatureC
                + (dryLift * dryFactor)
                - (wetness * 6f)
                - (windCooling * 2.5f)
                - (stormCooling * 1.5f);

            var surfaceTemperatureC = IsFinite(currentSurfaceTemperatureC)
                ? currentSurfaceTemperatureC
                : targetSurfaceTemperatureC;
            var tau = targetSurfaceTemperatureC >= surfaceTemperatureC
                ? SurfaceHeatingTauSeconds
                : SurfaceCoolingTauSeconds;
            var alpha = ResolveExpAlpha(elapsedSeconds, tau);
            surfaceTemperatureC += (targetSurfaceTemperatureC - surfaceTemperatureC) * alpha;
            return Clamp(surfaceTemperatureC, ambientTemperatureC - 35f, ambientTemperatureC + 75f);
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

        private static float ResolveExpAlpha(float elapsedSeconds, float timeConstantSeconds)
        {
            var tau = Math.Max(0.0001f, timeConstantSeconds);
            var alpha = 1f - (float)Math.Exp(-Math.Max(0f, elapsedSeconds) / tau);
            return Clamp01(alpha);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
