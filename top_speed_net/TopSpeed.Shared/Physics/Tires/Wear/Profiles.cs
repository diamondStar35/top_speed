using System;

namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearProfiles
    {
        public static TireWearConfig Balanced => TireWearDefaults.Balanced;

        public static TireWearConfig CreateFromVehicle(
            float tireGripCoefficient,
            float massKg,
            float tireCircumferenceM,
            float lateralGripCoefficient)
        {
            var gripNorm = Clamp01((tireGripCoefficient - 0.80f) / 0.40f);
            var lateralNorm = Clamp01((lateralGripCoefficient - 0.80f) / 0.45f);
            var compoundAggression = Clamp01((gripNorm * 0.65f) + (lateralNorm * 0.35f));
            var massNorm = Clamp01((massKg - 350f) / 1850f);
            var sizeNorm = Clamp01((tireCircumferenceM - 1.6f) / 0.8f);

            var baseWearPerKilometer = Clamp(
                0.0022f + (0.0019f * compoundAggression) + (0.0012f * massNorm) - (0.0003f * sizeNorm),
                0.0016f,
                0.0075f);
            // Scaled 0.35x after the smooth-then-square wear cleanup removed the
            // old 0-1 smoothed-slip gate; lands slightly slower than the
            // pre-refactor pace.
            var slipWearRatePerSecond = Clamp(0.000084f + (0.000077f * compoundAggression) + (0.000035f * massNorm), 0.000049f, 0.000252f);
            var corneringSlipWearWeight = Clamp(0.38f + (0.14f * compoundAggression), 0.24f, 0.62f);
            var longitudinalSlipWearWeight = Clamp(0.62f + (0.08f * compoundAggression), 0.45f, 0.86f);
            var loadWearGain = Clamp(0.72f + (0.95f * massNorm), 0.60f, 1.95f);
            var wearHotStartTemperatureC = Clamp(102f + (7f * compoundAggression), 96f, 116f);
            var wearHotGainPerC = Clamp(0.014f + (0.016f * compoundAggression), 0.008f, 0.045f);
            var wearColdStartTemperatureC = Clamp(40f - (6f * compoundAggression), 30f, 46f);
            var wearColdGainPerC = Clamp(0.0025f + (0.0025f * (1f - compoundAggression)), 0.001f, 0.009f);
            var coldEndTemperatureC = Clamp(54f - (10f * compoundAggression), 46f, 60f);
            var optimalStartTemperatureC = Clamp(81f + (7f * compoundAggression), 79f, 90f);
            var optimalEndTemperatureC = Clamp(126f + (10f * compoundAggression), 122f, 136f);
            var overheatEndTemperatureC = Clamp(139f + (11f * compoundAggression), 136f, 148f);
            var gripAtVeryCold = Clamp(0.74f - (0.05f * compoundAggression), 0.60f, 0.82f);
            var gripAtColdEnd = Clamp(0.93f - (0.03f * compoundAggression), 0.80f, 0.97f);
            var gripAtOptimal = 1.0f;
            var gripAtOverheatEnd = Clamp(0.79f - (0.08f * compoundAggression), 0.58f, 0.86f);
            var gripAtCooked = Clamp(0.64f - (0.06f * compoundAggression), 0.48f, 0.74f);
            var gripAtFullWear = Clamp(0.79f - (0.14f * compoundAggression) + (0.03f * sizeNorm), 0.52f, 0.90f);
            // Heat coefficients (°C/s when speed=1m/s, load=1, signal=1).
            // Aggressive compounds heat faster; heavier cars load the contact patch harder.
            // Cornering heat trimmed 30% (was 0.12 / 0.12 / 0.06, cap 0.45) — it
            // ran a touch high, and the headroom helps keep peaks manageable once
            // braking heat is brought up to spec.
            var corneringHeatCPerSecond = Clamp(0.084f + (0.084f * compoundAggression) + (0.042f * massNorm), 0.056f, 0.315f);
            // Acceleration heat trimmed 40% total (was 0.18 / 0.15 / 0.06, cap
            // 0.55) — realized-acceleration heat ran high in low gears (more
            // wheel torque = harder real acceleration out of corners).
            var accelerationHeatCPerSecond = Clamp(0.108f + (0.09f * compoundAggression) + (0.036f * massNorm), 0.072f, 0.33f);
            // Braking is the worse offender. Independent formula (not derived
            // from acceleration). Bumped 1.5x so braking makes more heat, paired
            // with the speed-independent brake floor in the heat model so hard
            // stops keep heating as they slow. Re-tune vs stint feel.
            var brakeHeatCPerSecond = Clamp(0.864f + (0.72f * compoundAggression) + (0.438f * massNorm), 0.24f, 3.75f);
            var loadHeatCPerSecond = Clamp(0.035f + (0.014f * massNorm) + (0.005f * compoundAggression), 0.025f, 0.060f);
            var rollingHeatCPerSecond = Clamp(0.018f + (0.008f * massNorm) + (0.004f * (1f - sizeNorm)), 0.012f, 0.032f);
            // Cooling coefficients (1/s). Larger tires shed heat faster; soft compounds run hotter.
            // TRIPLED ambient, road, and airflow cooling to rapidly dump heat into the environment.
            var airflowCoolingPerMpsPerCPerSecond = Clamp(0.00135f + (0.00018f * sizeNorm) - (0.00012f * compoundAggression), 0.00060f, 0.00225f);
            var ambientExchangePerCPerSecond = Clamp(0.0105f + (0.0015f * sizeNorm), 0.0045f, 0.0120f);
            var roadExchangePerCPerSecond = Clamp(0.0135f + (0.0024f * sizeNorm) - (0.0012f * compoundAggression), 0.0060f, 0.0150f);
            var wetRoadExchangePerCPerSecond = Clamp(0.0150f + (0.0060f * compoundAggression), 0.0120f, 0.0270f);

            // Surface?tread conductance: increased base and caps ~3.75x
            // Drains surface heat spikes into intermediate tread node instantly.
            var surfaceToTreadConductancePerSecond = Clamp(
                0.45f + (0.15f * compoundAggression), 0.30f, 0.80f);

            // Tread?carcass conductance: increased base and caps ~5x
            // Channels bulk tread heat deep into the carcass reservoir.
            var treadToCarcassConductancePerSecond = Clamp(
                0.100f + (0.040f * compoundAggression), 0.060f, 0.200f);

            // Tread mass ratio: halved mass to reduce thermal buffer inertia.
            var treadMassRatio = Clamp(
                0.45f + (0.20f * sizeNorm) + (0.10f * massNorm) - (0.10f * compoundAggression),
                0.30f,
                0.90f);

            // Carcass mass ratio: halved bulk mass so the entire tire sheds heat and cools down rapidly.
            var carcassMassRatio = Clamp(
                1.80f + (0.60f * massNorm) + (0.40f * sizeNorm) - (0.30f * compoundAggression),
                1.20f,
                3.20f);
            var slipSmoothingTau = Clamp(1.55f - (0.68f * compoundAggression), 0.55f, 2.50f);

            return new TireWearConfig
            {
                BaseWearPerKilometer = baseWearPerKilometer,
                SlipWearRatePerSecond = slipWearRatePerSecond,
                CorneringSlipWearWeight = corneringSlipWearWeight,
                LongitudinalSlipWearWeight = longitudinalSlipWearWeight,
                LoadWearGain = loadWearGain,
                WearHotStartTemperatureC = wearHotStartTemperatureC,
                WearHotGainPerC = wearHotGainPerC,
                WearColdStartTemperatureC = wearColdStartTemperatureC,
                WearColdGainPerC = wearColdGainPerC,
                ColdEndTemperatureC = coldEndTemperatureC,
                OptimalStartTemperatureC = optimalStartTemperatureC,
                OptimalEndTemperatureC = optimalEndTemperatureC,
                OverheatEndTemperatureC = overheatEndTemperatureC,
                GripAtVeryCold = gripAtVeryCold,
                GripAtColdEnd = gripAtColdEnd,
                GripAtOptimal = gripAtOptimal,
                GripAtOverheatEnd = gripAtOverheatEnd,
                GripAtCooked = gripAtCooked,
                GripAtFullWear = gripAtFullWear,
                CorneringHeatCPerSecond = corneringHeatCPerSecond,
                AccelerationHeatCPerSecond = accelerationHeatCPerSecond,
                BrakeHeatCPerSecond = brakeHeatCPerSecond,
                LoadHeatCPerSecond = loadHeatCPerSecond,
                RollingHeatCPerSecond = rollingHeatCPerSecond,
                AirflowCoolingPerMpsPerCPerSecond = airflowCoolingPerMpsPerCPerSecond,
                AmbientExchangePerCPerSecond = ambientExchangePerCPerSecond,
                RoadExchangePerCPerSecond = roadExchangePerCPerSecond,
                WetRoadExchangePerCPerSecond = wetRoadExchangePerCPerSecond,
                SurfaceToTreadConductancePerSecond = surfaceToTreadConductancePerSecond,
                TreadToCarcassConductancePerSecond = treadToCarcassConductancePerSecond,
                TreadMassRatio = treadMassRatio,
                CarcassMassRatio = carcassMassRatio,
                SlipSmoothingTimeConstantSeconds = slipSmoothingTau,
            };
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return min;
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
