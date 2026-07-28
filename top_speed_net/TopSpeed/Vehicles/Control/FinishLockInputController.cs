using TopSpeed.Input;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class FinishLockInputController : ICarController
    {
        private readonly DriveInput _input;

        public FinishLockInputController(DriveInput input)
        {
            _input = input;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            // The finish lock holds the car in neutral (FinishVehicle.Apply) and this controller never
            // issues a gear change, so the driveline stays disengaged. Drive acceleration is scaled by
            // the coupling factor (0 in neutral) in LongitudinalStep, so passing throttle through lets a
            // finished player free-rev the engine while waiting for the field without moving the car.
            return new CarControlIntent(
                _input.Intents.GetAxisPercent(DriveIntent.Steering),
                throttle: _input.Intents.GetAxisPercent(DriveIntent.Throttle),
                brake: 0,
                clutch: _input.Intents.GetAxisPercent(DriveIntent.Clutch),
                horn: _input.Intents.IsTriggered(DriveIntent.Horn),
                gearUp: false,
                gearDown: false);
        }
    }
}

