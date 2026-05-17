using System;
using System.Collections.Generic;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Core;
using TopSpeed.Vehicles.Physics;

namespace TopSpeed.Vehicles
{
    internal interface ICar : IDisposable
    {
        CarState State { get; }
        float PositionX { get; }
        float PositionY { get; }
        float Speed { get; }
        int Frequency { get; }
        int Gear { get; }
        bool InReverseGear { get; }
        bool ManualTransmission { get; set; }
        bool ShiftOnDemandSupported { get; }
        bool ShiftOnDemandEnabled { get; }
        CarType CarType { get; }
        ICarListener? Listener { get; set; }
        bool CombustionActive { get; }
        bool EngineRunning { get; }
        bool Braking { get; }
        bool Horning { get; }
        bool UserDefined { get; }
        string? CustomFile { get; }
        string VehicleName { get; }
        float WidthM { get; }
        float LengthM { get; }
        float MassKg { get; }
        float SpeedKmh { get; }
        float EngineRpm { get; }
        float EngineHorsepower { get; }
        float EngineGrossHorsepower { get; }
        float EngineNetHorsepower { get; }
        float DistanceMeters { get; }
        float FuelLitersRemaining { get; }
        float FuelTankCapacityLiters { get; }
        float FuelBurnLitersPerHour { get; }
        float FuelEstimatedRangeMeters { get; }
        float FuelEfficiencyLitersPer100Km { get; }
        float FuelEfficiencyMpg { get; }
        bool FuelLow { get; }
        bool FuelEmpty { get; }
        void AddFuelLiters(float liters);

        void Initialize(float positionX = 0, float positionY = 0);
        void SetPosition(float positionX, float positionY);
        void FinalizeCar();
        void Start();
        void RestartFromStall();
        void RestartAfterCrash();
        void ShutdownEngine();
        void StopMotionImmediately();
        void Crash();
        void MiniCrash(float newPosition);
        void Bump(float bumpX, float bumpY, float speedDeltaKph);
        void SetNeutralGear();
        void SetFirstGear();
        int GetGearForSpeedKmh(float speedKmh);
        void Stop();
        void Quiet();
        void Run(float elapsed);
        void BrakeSound();
        void BrakeCurveSound();
        void Evaluate(Track.Road road);
        bool Backfiring();
        void Pause();
        void Unpause();

        void PrepareEngineForPitExit(float targetSpeedKmh);

        void SetPrimaryController(ICarController controller);
        void SetOverrideController(ICarController? controller);
        void SetControlArbiter(IControlArbiter arbiter);
        void SetModifiers(IReadOnlyList<ICarModifier>? modifiers);
        void SetPhysicsModel(IModel model);
        bool ToggleShiftOnDemand();
    }
}

