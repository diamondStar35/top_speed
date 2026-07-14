using System;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class PitLaneController : ICarController
    {
        private const float TargetSpeedKmh = 56.33f;
        private const float ShiftBandKmh = 2f;
        private const float SteerThresholdM = 0.3f;

        // Cruise is a proportional throttle law rather than an on/off band. Every vehicle in the
        // catalog holds pit-lane speed on somewhere between 16% and 85% throttle, so the loop
        // settles on a steady partial opening instead of chattering the throttle shut and open
        // several times a second. A closed throttle is what arms the overrun backfire, so a
        // chattering throttle made backfire-equipped cars pop continuously down pit road.
        private const float CruiseThrottlePercent = 50f;
        private const float ThrottleGainPercentPerKmh = 30f;

        // The brake only engages for a genuine overspeed — arriving at pit entry from racing
        // speed. Below this the car sheds speed on coast alone (1.4-3.9 m/s^2 depending on the
        // vehicle), which is also all the old band-edge brake did: Car.ApplyCoastDecel ignores
        // any brake request weaker than 10%, so a -4% command was already a no-op.
        private const float BrakeEngageKmh = 5f;
        private const float BrakeGainPercentPerKmh = 2f;

        private readonly Func<float, int> _gearForSpeed;

        public bool BrakeMode { get; set; }
        public float? SteerTargetX { get; set; }

        public PitLaneController(Func<float, int> gearForSpeed)
        {
            _gearForSpeed = gearForSpeed;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            var steering = 0;
            if (SteerTargetX.HasValue)
            {
                var diff = SteerTargetX.Value - context.PositionX;
                if (diff > SteerThresholdM) steering = 100;
                else if (diff < -SteerThresholdM) steering = -100;
            }

            if (BrakeMode)
                return new CarControlIntent(steering, 10, -100, 100, false, false, false);

            var speed = context.Speed;
            var error = TargetSpeedKmh - speed;

            // Gear management for manual transmissions only;
            // automatic transmissions use their own auto-shifter.
            var gearDown = false;
            var gearUp = false;
            var clutch = 0;

            if (context.ManualTransmission && context.Gear > 0)
            {
                var gear = context.Gear;
                var targetGear = _gearForSpeed(speed);
                if (gear > targetGear)
                {
                    gearDown = true;
                    clutch = 100;
                }
                else if (gear < targetGear && error > ShiftBandKmh)
                {
                    gearUp = true;
                    clutch = 100;
                }
            }

            if (error < -BrakeEngageKmh)
            {
                var brake = (int)Math.Max(-100f, error * BrakeGainPercentPerKmh);
                return new CarControlIntent(steering, 0, brake, clutch, false, false, gearDown);
            }

            // Clamped at zero rather than left negative: CarControlIntent only clamps to [-100, 100],
            // and a negative throttle reads as a brake request once Car resolves thrust.
            var throttle = (int)Math.Round(CruiseThrottlePercent + (ThrottleGainPercentPerKmh * error));
            throttle = Math.Max(0, Math.Min(100, throttle));
            return new CarControlIntent(steering, throttle, 0, clutch, false, gearUp, gearDown);
        }
    }
}
