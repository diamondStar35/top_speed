using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void SetLeft(AxisOrButton a)
        {
            _left = a;
            _settings.SetControllerBinding(DriveIntent.SteerLeft, a);
        }

        public void SetLeft(Key key)
        {
            _kbLeft = key;
            _settings.SetKeyboardBinding(DriveIntent.SteerLeft, key);
        }

        public void SetRight(AxisOrButton a)
        {
            _right = a;
            _settings.SetControllerBinding(DriveIntent.SteerRight, a);
        }

        public void SetRight(Key key)
        {
            _kbRight = key;
            _settings.SetKeyboardBinding(DriveIntent.SteerRight, key);
        }

        public void SetThrottle(AxisOrButton a)
        {
            _throttle = a;
            _settings.SetControllerBinding(DriveIntent.Throttle, a);
        }

        public void SetThrottle(Key key)
        {
            _kbThrottle = key;
            _settings.SetKeyboardBinding(DriveIntent.Throttle, key);
        }

        public void SetBrake(AxisOrButton a)
        {
            _brake = a;
            _settings.SetControllerBinding(DriveIntent.Brake, a);
        }

        public void SetBrake(Key key)
        {
            _kbBrake = key;
            _settings.SetKeyboardBinding(DriveIntent.Brake, key);
        }

        public void SetClutch(AxisOrButton a)
        {
            _clutch = a;
            _settings.SetControllerBinding(DriveIntent.Clutch, a);
        }

        public void SetClutch(Key key)
        {
            _kbClutch = key;
            _settings.SetKeyboardBinding(DriveIntent.Clutch, key);
        }

        public void SetGearUp(AxisOrButton a)
        {
            _gearUp = a;
            _settings.SetControllerBinding(DriveIntent.GearUp, a);
        }

        public void SetGearUp(Key key)
        {
            _kbGearUp = key;
            _settings.SetKeyboardBinding(DriveIntent.GearUp, key);
        }

        public void SetGearDown(AxisOrButton a)
        {
            _gearDown = a;
            _settings.SetControllerBinding(DriveIntent.GearDown, a);
        }

        public void SetGearDown(Key key)
        {
            _kbGearDown = key;
            _settings.SetKeyboardBinding(DriveIntent.GearDown, key);
        }

        public void SetHorn(AxisOrButton a)
        {
            _horn = a;
            _settings.SetControllerBinding(DriveIntent.Horn, a);
        }

        public void SetHorn(Key key)
        {
            _kbHorn = key;
            _settings.SetKeyboardBinding(DriveIntent.Horn, key);
        }

        public void SetPit(AxisOrButton a)
        {
            _pit = a;
            _settings.SetControllerBinding(DriveIntent.Pit, a);
        }

        public void SetPit(Key key)
        {
            _kbPit = key;
            _settings.SetKeyboardBinding(DriveIntent.Pit, key);
        }
    }
}



