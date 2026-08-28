using System;

namespace TopSpeed.Vehicles
{
    public readonly struct AutomaticShiftDecision
    {
        public AutomaticShiftDecision(bool changed, int newGear, float cooldownSeconds)
        {
            Changed = changed;
            NewGear = newGear;
            CooldownSeconds = cooldownSeconds;
        }

        public bool Changed { get; }
        public int NewGear { get; }
        public float CooldownSeconds { get; }
    }

    public readonly struct AutomaticShiftInput
    {
        public AutomaticShiftInput(
            int currentGear,
            int gears,
            float speedMps,
            float referenceTopSpeedMps,
            float idleRpm,
            float revLimiter,
            float currentRpm,
            float currentAccel,
            float upAccel,
            float downAccel)
        {
            CurrentGear = currentGear;
            Gears = gears;
            SpeedMps = speedMps;
            ReferenceTopSpeedMps = referenceTopSpeedMps;
            IdleRpm = idleRpm;
            RevLimiter = revLimiter;
            CurrentRpm = currentRpm;
            CurrentAccel = currentAccel;
            UpAccel = upAccel;
            DownAccel = downAccel;
        }

        public int CurrentGear { get; }
        public int Gears { get; }
        public float SpeedMps { get; }
        public float ReferenceTopSpeedMps { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float CurrentRpm { get; }
        public float CurrentAccel { get; }
        public float UpAccel { get; }
        public float DownAccel { get; }
    }

    public static class AutomaticTransmissionLogic
    {
        private const float DownshiftReentryBandFraction = 0.70f;

        /// <summary>
        /// Acceleration below which the current gear is treated as having nothing left to give.
        /// </summary>
        private const float GearExhaustedAccelMps2 = 0.05f;

        public static AutomaticShiftDecision Decide(in AutomaticShiftInput input, TransmissionPolicy? policy)
        {
            var p = policy ?? TransmissionPolicy.Default;
            if (input.Gears <= 1 || input.CurrentGear < 1 || input.CurrentGear > input.Gears)
                return new AutomaticShiftDecision(false, input.CurrentGear, 0f);

            var intendedTopSpeedGear = p.ResolveIntendedTopSpeedGear(input.Gears);
            var topSpeedPursuitThreshold = input.ReferenceTopSpeedMps > 0f
                ? input.ReferenceTopSpeedMps * p.TopSpeedPursuitSpeedFraction
                : float.MaxValue;
            var nearTopSpeed = input.SpeedMps >= topSpeedPursuitThreshold;
            var upshiftRpm = p.ResolveUpshiftRpm(input.IdleRpm, input.RevLimiter);
            var downshiftRpm = p.ResolveDownshiftRpm(input.IdleRpm, input.RevLimiter);
            var performanceDownshiftReentryRpm = ResolvePerformanceDownshiftReentryRpm(downshiftRpm, upshiftRpm);

            var bestGear = input.CurrentGear;
            var bestAccel = input.CurrentAccel;

            // A gear that has stopped accelerating must never be able to trap the car. Without
            // this the "hold the intended top-speed gear until near the limit" preference becomes
            // circular: the taller gear is refused until a speed that only the taller gear can
            // reach, and the car sits below its real top speed for the rest of the race.
            var currentGearExhausted = input.CurrentAccel <= GearExhaustedAccelMps2;

            if (input.CurrentGear < input.Gears)
            {
                if (input.CurrentRpm >= upshiftRpm &&
                    CanConsiderUpshift(in input, p, intendedTopSpeedGear, nearTopSpeed, currentGearExhausted) &&
                    input.UpAccel > bestAccel)
                {
                    bestAccel = input.UpAccel;
                    bestGear = input.CurrentGear + 1;
                }
            }

            if (input.CurrentGear < input.Gears && input.CurrentRpm >= input.RevLimiter * 0.995f)
            {
                if (CanForceUpshiftAtLimiter(in input, p, intendedTopSpeedGear, nearTopSpeed, currentGearExhausted))
                    return UpshiftDecision(input.CurrentGear, input.Gears, p);
            }

            if (input.CurrentGear > 1 && input.CurrentRpm < downshiftRpm)
            {
                return new AutomaticShiftDecision(true, input.CurrentGear - 1, p.BaseAutoShiftCooldownSeconds);
            }

            if (CanConsiderPerformanceDownshift(in input, performanceDownshiftReentryRpm) &&
                input.DownAccel > input.CurrentAccel * (1f + p.UpshiftHysteresis))
            {
                return new AutomaticShiftDecision(true, input.CurrentGear - 1, p.BaseAutoShiftCooldownSeconds);
            }

            if (bestGear > input.CurrentGear && bestAccel > input.CurrentAccel * (1f + p.UpshiftHysteresis))
            {
                return UpshiftDecision(input.CurrentGear, input.Gears, p);
            }

            // The proportional hysteresis above cannot be cleared once both gears are down to a
            // fraction of a m/s2, so an exhausted gear needs an absolute comparison to escape.
            // The margin matters: a taller gear that is only marginally better right now can still
            // have a lower terminal speed, and taking it would cost the car speed rather than
            // gain it.
            if (currentGearExhausted &&
                bestGear > input.CurrentGear &&
                bestAccel > input.CurrentAccel + GearExhaustedAccelMps2)
            {
                return UpshiftDecision(input.CurrentGear, input.Gears, p);
            }

            return new AutomaticShiftDecision(false, input.CurrentGear, 0f);
        }

        private static float ResolvePerformanceDownshiftReentryRpm(float downshiftRpm, float upshiftRpm)
        {
            if (upshiftRpm <= downshiftRpm)
                return downshiftRpm;
            var reentryRpm = downshiftRpm + ((upshiftRpm - downshiftRpm) * DownshiftReentryBandFraction);
            return Math.Max(downshiftRpm, Math.Min(upshiftRpm, reentryRpm));
        }

        private static bool CanConsiderPerformanceDownshift(in AutomaticShiftInput input, float performanceDownshiftReentryRpm)
        {
            if (input.CurrentGear <= 1)
                return false;
            if (input.CurrentRpm > performanceDownshiftReentryRpm)
                return false;
            if (float.IsNaN(input.DownAccel) || float.IsInfinity(input.DownAccel))
                return false;
            return true;
        }

        private static bool CanConsiderUpshift(
            in AutomaticShiftInput input,
            TransmissionPolicy policy,
            int intendedTopSpeedGear,
            bool nearTopSpeed,
            bool currentGearExhausted)
        {
            var nextGear = input.CurrentGear + 1;
            if (nextGear > input.Gears)
                return false;

            if (!policy.AllowOverdriveAboveGameTopSpeed &&
                nextGear > intendedTopSpeedGear &&
                !currentGearExhausted &&
                input.SpeedMps < input.ReferenceTopSpeedMps * 0.999f)
            {
                return false;
            }

            if (policy.AllowOverdriveAboveGameTopSpeed &&
                nextGear > intendedTopSpeedGear &&
                policy.PreferIntendedTopSpeedGearNearLimit &&
                !nearTopSpeed &&
                !currentGearExhausted)
            {
                return false;
            }

            if (input.UpAccel < policy.MinUpshiftNetAccelerationMps2 && !nearTopSpeed)
                return false;

            return true;
        }

        private static bool CanForceUpshiftAtLimiter(
            in AutomaticShiftInput input,
            TransmissionPolicy policy,
            int intendedTopSpeedGear,
            bool nearTopSpeed,
            bool currentGearExhausted)
        {
            var nextGear = input.CurrentGear + 1;
            if (nextGear > input.Gears)
                return false;

            if (!policy.AllowOverdriveAboveGameTopSpeed && nextGear > intendedTopSpeedGear && !currentGearExhausted)
                return false;

            if (policy.AllowOverdriveAboveGameTopSpeed &&
                nextGear > intendedTopSpeedGear &&
                policy.PreferIntendedTopSpeedGearNearLimit &&
                !nearTopSpeed &&
                !currentGearExhausted)
            {
                return false;
            }

            if (input.UpAccel < policy.MinUpshiftNetAccelerationMps2 && !nearTopSpeed)
                return false;

            return true;
        }

        private static AutomaticShiftDecision UpshiftDecision(int currentGear, int gears, TransmissionPolicy policy)
        {
            return new AutomaticShiftDecision(
                true,
                currentGear + 1,
                policy.GetUpshiftCooldownSeconds(currentGear, gears));
        }
    }
}
