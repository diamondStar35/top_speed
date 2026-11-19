using System;

namespace TopSpeed.Vehicles
{
    internal sealed partial class TorqueCurve
    {
        private static TorqueProfileKind SelectProfile(float maxRpm, float peakTorqueRpm, float massKg, float frontalAreaM2)
        {
            var isMotorcycle = (massKg > 0f && massKg < 450f)
                || (frontalAreaM2 > 0f && frontalAreaM2 < 1.0f)
                || maxRpm >= 11000f;
            if (isMotorcycle)
                return TorqueProfileKind.Motorcycle;

            var isDiesel = peakTorqueRpm > 0f && peakTorqueRpm <= 2200f && maxRpm <= 5200f;
            if (isDiesel)
                return massKg > 2200f ? TorqueProfileKind.HeavyTruck : TorqueProfileKind.DieselLowRev;

            var isHighRev = maxRpm >= 8000f && peakTorqueRpm >= 4500f;
            if (isHighRev)
                return TorqueProfileKind.HighRevNa;

            var isTurboBroad = peakTorqueRpm > 0f && peakTorqueRpm <= 3500f && maxRpm >= 6000f;
            if (isTurboBroad)
                return TorqueProfileKind.SportTurbo;

            var isMuscle = maxRpm <= 6200f && peakTorqueRpm <= 3500f && massKg >= 1300f;
            if (isMuscle)
                return TorqueProfileKind.Muscle;

            if (massKg > 1800f && maxRpm <= 6200f)
                return TorqueProfileKind.Economy;

            return TorqueProfileKind.Default;
        }

        private static TorqueProfileParams GetProfileParams(TorqueProfileKind profile)
        {
            switch (profile)
            {
                case TorqueProfileKind.Motorcycle:
                    return new TorqueProfileParams(1.7f, 1.15f, 0.25f, 0.65f);
                case TorqueProfileKind.HighRevNa:
                    return new TorqueProfileParams(1.45f, 1.10f, 0.28f, 0.70f);
                case TorqueProfileKind.TurboBroad:
                    return new TorqueProfileParams(0.80f, 1.60f, 0.35f, 0.60f);
                case TorqueProfileKind.DieselLowRev:
                    return new TorqueProfileParams(0.70f, 1.85f, 0.45f, 0.55f);
                case TorqueProfileKind.Muscle:
                    return new TorqueProfileParams(0.95f, 1.25f, 0.35f, 0.65f);
                case TorqueProfileKind.Economy:
                    return new TorqueProfileParams(1.05f, 1.55f, 0.30f, 0.58f);
                case TorqueProfileKind.SportTurbo:
                    return new TorqueProfileParams(0.85f, 1.45f, 0.33f, 0.62f);
                case TorqueProfileKind.Supercharged:
                    return new TorqueProfileParams(1.05f, 1.10f, 0.35f, 0.70f);
                case TorqueProfileKind.HeavyTruck:
                    return new TorqueProfileParams(0.65f, 2.10f, 0.55f, 0.50f);
                default:
                    return new TorqueProfileParams(1.10f, 1.30f, 0.32f, 0.65f);
            }
        }

        public static bool TryParseProfile(string? text, out TorqueProfileKind profile)
        {
            profile = TorqueProfileKind.Default;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            var value = (text ?? string.Empty).Trim().ToLowerInvariant();
            switch (value)
            {
                case "default":
                case "auto":
                    profile = TorqueProfileKind.Default;
                    return true;
                case "highrev":
                case "highrevna":
                case "na":
                case "sportna":
                    profile = TorqueProfileKind.HighRevNa;
                    return true;
                case "turbo":
                case "turbobroad":
                case "sportturbo":
                    profile = TorqueProfileKind.SportTurbo;
                    return true;
                case "diesel":
                case "diesellowrev":
                    profile = TorqueProfileKind.DieselLowRev;
                    return true;
                case "truck":
                case "heavytruck":
                    profile = TorqueProfileKind.HeavyTruck;
                    return true;
                case "motorcycle":
                case "bike":
                    profile = TorqueProfileKind.Motorcycle;
                    return true;
                case "muscle":
                    profile = TorqueProfileKind.Muscle;
                    return true;
                case "economy":
                    profile = TorqueProfileKind.Economy;
                    return true;
                case "supercharged":
                case "sc":
                    profile = TorqueProfileKind.Supercharged;
                    return true;
            }

            return false;
        }
    }
}

