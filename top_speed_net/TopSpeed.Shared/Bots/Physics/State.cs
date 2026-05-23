namespace TopSpeed.Bots
{
    public struct BotPhysicsState
    {
        public float PositionX;
        public float PositionY;
        public float SpeedKph;
        public float LateralVelocityMps;
        public float YawRateRad;
        public int Gear;
        public float AutoShiftCooldownSeconds;
        public float AutomaticCouplingFactor;
        public float CvtRatio;
        public float EffectiveDriveRatio;
        public float TireWearFraction;
        public float TireTemperatureC;
        public float TireTreadTemperatureC;
        public float TireCarcassTemperatureC;
        public float TireSmoothedSlipNormalized;
        public float SurfaceTemperatureC;
    }
}
