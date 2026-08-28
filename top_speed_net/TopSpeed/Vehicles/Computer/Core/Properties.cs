namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        public ComputerState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float Speed => _speed;
        public int PlayerNumber => _playerNumber;
        public int VehicleIndex => _vehicleIndex;
        // Display name of the custom vehicle this bot represents, or null when it uses a built-in.
        public string? CustomVehicleName => _customVehicleName;
        public bool Finished => _finished;
        public void SetFinished(bool value) => _finished = value;
        public float WidthM => _widthM;
        public float LengthM => _lengthM;
        public float MassKg => _massKg;
        public float LateralVelocityMps => _lateralVelocityMps;
    }
}

