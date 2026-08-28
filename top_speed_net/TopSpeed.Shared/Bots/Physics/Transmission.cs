using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static void UpdateAutomaticGear(
            BotPhysicsConfig config,
            ref BotPhysicsState state,
            float elapsed,
            float speedMps,
            float throttle,
            float surfaceTractionMod,
            float longitudinalGripFactor,
            float? driveRatioOverride = null)
        {
            if (config.Gears <= 1)
                return;

            var shiftResult = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config.Powertrain,
                    config.TransmissionPolicy,
                    config.ActiveTransmissionType,
                    state.Gear,
                    config.Gears,
                    speedMps,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor,
                    config.TopSpeedKph / 3.6f,
                    elapsed,
                    state.AutoShiftCooldownSeconds,
                    shiftOnDemandActive: false,
                    driveRatioOverride,
                    state.AutomaticCouplingFactor));
            state.AutoShiftCooldownSeconds = shiftResult.CooldownSeconds;
            if (shiftResult.Changed)
                state.Gear = shiftResult.Gear;
        }
    }
}


