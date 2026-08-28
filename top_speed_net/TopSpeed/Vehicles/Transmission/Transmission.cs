using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles.Events;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;
            if (_gear < FirstForwardGear)
            {
                _switchingGear = 0;
                _autoShiftCooldown = 0f;
                return;
            }

            var shiftResult = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    _powertrainConfiguration,
                    _transmissionPolicy,
                    EffectiveTransmissionType(),
                    _gear,
                    _gears,
                    speedMps,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor,
                    _topSpeed / 3.6f,
                    elapsed,
                    _autoShiftCooldown,
                    shiftOnDemandActive: IsShiftOnDemandActive(),
                    _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null,
                    _drivelineCouplingFactor));
            _autoShiftCooldown = shiftResult.CooldownSeconds;
            if (shiftResult.Changed)
                ShiftAutomaticGear(shiftResult.Gear, shiftResult.CooldownSeconds, shiftResult.ShiftDirection, shiftResult.InGearDelaySeconds);
        }

        private void ShiftAutomaticGear(int newGear, float cooldownSeconds, int shiftDirection, float inGearDelaySeconds)
        {
            if (newGear == _gear)
                return;
            _switchingGear = shiftDirection != 0 ? shiftDirection : (newGear > _gear ? 1 : -1);
            _gear = newGear;
            PushEvent(EventType.InGear, Math.Max(0f, inGearDelaySeconds));
            _autoShiftCooldown = Math.Max(0f, cooldownSeconds);
        }
    }
}



