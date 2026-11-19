using System;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        private float SpeedMpsFromRpm(float rpm, int gear)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var ratio = _gearRatios[clampedGear - 1] * _finalDriveRatio;
            if (ratio <= 0f)
                return 0f;
            return (rpm / ratio) * (_tireCircumferenceM / 60f);
        }

        private static float[] CalculateGearRatios(int gearCount)
        {
            var ratios = new float[gearCount];
            for (var i = 0; i < gearCount; i++)
            {
                var progress = (float)i / Math.Max(1, gearCount - 1);
                ratios[i] = 2.5f - (1.7f * progress);
            }

            return ratios;
        }

        private static float EvaluateTorqueCurve(float rpmNormalized)
        {
            var peak = 0.6f;
            var width = 0.4f;
            var x = (rpmNormalized - peak) / width;
            return (float)Math.Exp(-x * x) * 0.9f + 0.1f;
        }
    }
}

