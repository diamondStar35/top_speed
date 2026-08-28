using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public readonly struct AutomaticShiftRuntimeInput
    {
        public AutomaticShiftRuntimeInput(
            Config powertrainConfig,
            TransmissionPolicy transmissionPolicy,
            TransmissionType transmissionType,
            int currentGear,
            int gears,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float referenceTopSpeedMps,
            float elapsedSeconds,
            float cooldownSeconds,
            bool shiftOnDemandActive = false,
            float? driveRatioOverride = null,
            float drivelineCouplingFactor = 1f)
        {
            PowertrainConfig = powertrainConfig ?? throw new ArgumentNullException(nameof(powertrainConfig));
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
            TransmissionType = transmissionType;
            CurrentGear = currentGear;
            Gears = gears;
            SpeedMps = speedMps;
            Throttle = throttle;
            SurfaceTractionModifier = surfaceTractionModifier;
            LongitudinalGripFactor = longitudinalGripFactor;
            ReferenceTopSpeedMps = referenceTopSpeedMps;
            ElapsedSeconds = elapsedSeconds;
            CooldownSeconds = cooldownSeconds;
            ShiftOnDemandActive = shiftOnDemandActive;
            DriveRatioOverride = driveRatioOverride;
            DrivelineCouplingFactor = drivelineCouplingFactor;
        }

        public Config PowertrainConfig { get; }
        public TransmissionPolicy TransmissionPolicy { get; }
        public TransmissionType TransmissionType { get; }
        public int CurrentGear { get; }
        public int Gears { get; }
        public float SpeedMps { get; }
        public float Throttle { get; }
        public float SurfaceTractionModifier { get; }
        public float LongitudinalGripFactor { get; }
        public float ReferenceTopSpeedMps { get; }
        public float ElapsedSeconds { get; }
        public float CooldownSeconds { get; }
        public bool ShiftOnDemandActive { get; }
        public float? DriveRatioOverride { get; }

        /// <summary>
        /// Live driveline coupling, so the gear comparison sees the same partial engagement the
        /// longitudinal step will apply.
        /// </summary>
        public float DrivelineCouplingFactor { get; }
    }

    public readonly struct AutomaticShiftRuntimeResult
    {
        public AutomaticShiftRuntimeResult(
            bool changed,
            int gear,
            float cooldownSeconds,
            int shiftDirection = 0,
            float inGearDelaySeconds = 0f)
        {
            Changed = changed;
            Gear = gear;
            CooldownSeconds = cooldownSeconds;
            ShiftDirection = shiftDirection;
            InGearDelaySeconds = inGearDelaySeconds;
        }

        public bool Changed { get; }
        public int Gear { get; }
        public float CooldownSeconds { get; }
        public int ShiftDirection { get; }
        public float InGearDelaySeconds { get; }
    }
}
