using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Tests;

internal static class BotPhysicsHarness
{
    public static IReadOnlyList<BotCatalogSnapshot> BuildCatalogSnapshot()
    {
        return OfficialCars()
            .Select(carType =>
            {
                var config = BotPhysicsCatalog.Get(carType);

                return new BotCatalogSnapshot(
                    Vehicle: carType.ToString(),
                    Transmission: config.ActiveTransmissionType.ToString(),
                    Gears: config.Gears,
                    TopSpeedKph: Rounding.F(config.TopSpeedKph, 1),
                    WheelRadiusM: Rounding.F(config.WheelRadiusM, 3),
                    FirstGearRatio: Rounding.F(config.GearRatios[0], 3),
                    TopGearRatio: Rounding.F(config.GearRatios[config.Gears - 1], 3),
                    SideAreaM2: Rounding.F(config.SideAreaM2),
                    RollingResistanceSpeedFactor: Rounding.F(config.RollingResistanceSpeedFactor),
                    CoupledDrivelineDragNm: Rounding.F(config.CoupledDrivelineDragNm),
                    CoupledDrivelineViscousDragNmPerKrpm: Rounding.F(config.CoupledDrivelineViscousDragNmPerKrpm),
                    SurfaceTractionFactor: Rounding.F(config.SurfaceTractionFactor));
            })
            .ToArray();
    }

    public static BotTrace SimulateLaunch(CarType carType, int steps = 60, float elapsedSeconds = 0.1f)
    {
        return SimulateScenario(
            scenario: "Launch",
            carType,
            TrackSurface.Asphalt,
            throttle: 100,
            brake: 0,
            steering: 0,
            steps,
            elapsedSeconds);
    }

    public static BotTrace SimulateScenario(
        string scenario,
        CarType carType,
        TrackSurface surface,
        int throttle,
        int brake,
        int steering,
        int steps,
        float elapsedSeconds,
        float initialSpeedKph = 0f)
    {
        var config = BotPhysicsCatalog.Get(carType);
        var state = CreateState(config, initialSpeedKph);
        var samples = new List<BotSample>();

        for (var i = 0; i < steps; i++)
        {
            var input = new BotPhysicsInput(
                elapsedSeconds,
                surface,
                throttle,
                brake,
                steering,
                ambientTemperatureC: float.NaN,
                rainGain: 0f,
                stormGain: 0f,
                windGain: 0f);
            BotPhysics.Step(config, ref state, input);

            if (i % 10 == 0 || i == steps - 1)
                samples.Add(ToSample(i + 1, elapsedSeconds, state));
        }

        return new BotTrace(
            Scenario: scenario,
            Vehicle: carType.ToString(),
            Surface: surface.ToString(),
            Steps: steps,
            FinalSpeedKph: Rounding.F(state.SpeedKph, 2),
            FinalGear: state.Gear,
            FinalPositionX: Rounding.F(state.PositionX, 2),
            FinalPositionY: Rounding.F(state.PositionY, 2),
            Samples: samples);
    }

    public static BotPhysicsState CreateState(BotPhysicsConfig config, float speedKph = 0f, int? gear = null)
    {
        return new BotPhysicsState
        {
            Gear = Math.Max(1, Math.Min(config.Gears, gear ?? 1)),
            AutomaticCouplingFactor = 1f,
            CvtRatio = config.AutomaticTuning.Cvt.RatioMax,
            SpeedKph = Math.Max(0f, speedKph),
            TireWearFraction = 0f,
            TireTemperatureC = float.NaN,
            TireCarcassTemperatureC = float.NaN,
            TireSmoothedSlipNormalized = 0f,
            SurfaceTemperatureC = float.NaN
        };
    }

    public static IEnumerable<CarType> OfficialCars()
    {
        return Enumerable.Range(0, 12).Select(index => (CarType)index);
    }

    private static BotSample ToSample(int step, float elapsedSeconds, in BotPhysicsState state)
    {
        return new BotSample(
            Step: step,
            TimeSeconds: Rounding.F(step * elapsedSeconds, 2),
            SpeedKph: Rounding.F(state.SpeedKph, 2),
            Gear: state.Gear,
            PositionX: Rounding.F(state.PositionX, 2),
            PositionY: Rounding.F(state.PositionY, 2),
            LateralVelocityMps: Rounding.F(state.LateralVelocityMps),
            YawRateRad: Rounding.F(state.YawRateRad),
            Coupling: Rounding.F(state.AutomaticCouplingFactor),
            EffectiveDriveRatio: Rounding.F(state.EffectiveDriveRatio));
    }
}

internal sealed record BotTrace(
    string Scenario,
    string Vehicle,
    string Surface,
    int Steps,
    float FinalSpeedKph,
    int FinalGear,
    float FinalPositionX,
    float FinalPositionY,
    IReadOnlyList<BotSample> Samples);

internal sealed record BotSample(
    int Step,
    float TimeSeconds,
    float SpeedKph,
    int Gear,
    float PositionX,
    float PositionY,
    float LateralVelocityMps,
    float YawRateRad,
    float Coupling,
    float EffectiveDriveRatio);

internal sealed record BotCatalogSnapshot(
    string Vehicle,
    string Transmission,
    int Gears,
    float TopSpeedKph,
    float WheelRadiusM,
    float FirstGearRatio,
    float TopGearRatio,
    float SideAreaM2,
    float RollingResistanceSpeedFactor,
    float CoupledDrivelineDragNm,
    float CoupledDrivelineViscousDragNmPerKrpm,
    float SurfaceTractionFactor);
