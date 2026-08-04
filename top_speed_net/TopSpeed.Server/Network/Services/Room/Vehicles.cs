using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;
using TopSpeed.Server.Bots;
using TopSpeed.Vehicles;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static CarType NormalizeNetworkCar(CarType car)
        {
            if (car < CarType.Vehicle1 || car >= CarType.CustomVehicle)
                return CarType.Vehicle1;
            return car;
        }

        private static void ApplyVehicleDimensions(PlayerConnection player, CarType car)
        {
            var dimensions = GetVehicleDimensions(car);
            player.WidthM = dimensions.WidthM;
            player.LengthM = dimensions.LengthM;
            player.MassKg = dimensions.MassKg;
        }

        private static void ApplyCustomVehicleDimensions(PlayerConnection player, VehiclePackageRecord record)
        {
            var fallback = GetVehicleDimensions(CarType.Vehicle1);
            player.WidthM = record != null && record.WidthM > 0f ? record.WidthM : fallback.WidthM;
            player.LengthM = record != null && record.LengthM > 0f ? record.LengthM : fallback.LengthM;
            player.MassKg = record != null && record.MassKg > 0f ? record.MassKg : fallback.MassKg;
        }

        private static void ApplyVehicleDimensions(RoomBot bot, CarType car)
        {
            var dimensions = GetVehicleDimensions(car);
            bot.WidthM = dimensions.WidthM;
            bot.LengthM = dimensions.LengthM;
            bot.PhysicsConfig = BotPhysicsCatalog.Get(car);
            bot.AudioProfile = GetVehicleAudioProfile(car);
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            var state = bot.PhysicsState;
            if (state.Gear <= 0)
                state.Gear = 1;
            bot.PhysicsState = state;
        }

        private static VehicleDimensions GetVehicleDimensions(CarType car)
        {
            var normalized = NormalizeNetworkCar(car);
            var spec = OfficialVehicleCatalog.Get((int)normalized);
            return new VehicleDimensions(spec.WidthM, spec.LengthM, spec.MassKg);
        }

        private static BotAudioProfile GetVehicleAudioProfile(CarType car)
        {
            return car switch
            {
                CarType.Vehicle1 => new BotAudioProfile(22050, 55000, 26000),
                CarType.Vehicle2 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle3 => new BotAudioProfile(6000, 25000, 19000),
                CarType.Vehicle4 => new BotAudioProfile(6000, 27000, 20000),
                CarType.Vehicle5 => new BotAudioProfile(6000, 33000, 27500),
                CarType.Vehicle6 => new BotAudioProfile(7025, 40000, 32500),
                CarType.Vehicle7 => new BotAudioProfile(6000, 26000, 21000),
                CarType.Vehicle8 => new BotAudioProfile(10000, 45000, 34000),
                CarType.Vehicle9 => new BotAudioProfile(22050, 30550, 22550),
                CarType.Vehicle10 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle11 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle12 => new BotAudioProfile(22050, 27550, 23550),
                _ => new BotAudioProfile(22050, 55000, 26000)
            };
        }

    }
}
