using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal static class VehicleLoader
    {
        private const string BuiltinPrefix = "builtin";

        public static VehicleDefinition LoadOfficial(int vehicleIndex, TrackWeather weather)
        {
            if (vehicleIndex < 0 || vehicleIndex >= VehicleCatalog.VehicleCount)
                vehicleIndex = 0;

            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var vehiclesRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");

            return new VehicleDefinition
            {
                CarType = (CarType)vehicleIndex,
                UserDefined = false,
                Acceleration = parameters.Acceleration,
                Deceleration = parameters.Deceleration,
                TopSpeed = parameters.TopSpeed,
                IdleFreq = parameters.IdleFreq,
                TopFreq = parameters.TopFreq,
                ShiftFreq = parameters.ShiftFreq,
                Gears = parameters.Gears,
                Steering = parameters.Steering,
                SteeringFactor = parameters.SteeringFactor,
                HasWipers = parameters.HasWipers == 1 && weather == TrackWeather.Rain ? 1 : 0,
                EngineSound = CombineSound(vehiclesRoot, parameters.EngineSound),
                StartSound = CombineSound(vehiclesRoot, parameters.StartSound),
                HornSound = CombineSound(vehiclesRoot, parameters.HornSound),
                ThrottleSound = CombineSound(vehiclesRoot, parameters.ThrottleSound),
                CrashSound = CombineSound(vehiclesRoot, parameters.CrashSound),
                BrakeSound = CombineSound(vehiclesRoot, parameters.BrakeSound),
                BackfireSound = CombineSound(vehiclesRoot, parameters.BackfireSound)
            };
        }

        public static VehicleDefinition LoadCustom(string vehicleFile, TrackWeather weather)
        {
            var filePath = Path.IsPathRooted(vehicleFile)
                ? vehicleFile
                : Path.Combine(AssetPaths.Root, vehicleFile);
            var settings = ReadVehicleFile(filePath);
            var builtinRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var customVehiclesRoot = Path.Combine(AssetPaths.Root, "Vehicles");

            var acceleration = ReadInt(settings, "acceleration", 10);
            var deceleration = ReadInt(settings, "deceleration", 40);
            var topSpeed = ReadInt(settings, "topspeed", 15000);
            var idleFreq = ReadInt(settings, "idlefreq", 11000);
            var topFreq = ReadInt(settings, "topfreq", 50000);
            var shiftFreq = ReadInt(settings, "shiftfreq", 40000);
            var gears = ReadInt(settings, "numberofgears", 5);
            var steering = ReadInt(settings, "steering", 100);
            var steeringFactor = ReadInt(settings, "steeringfactor", 40);

            var engineSound = ReadString(settings, "enginesound", "builtin1");
            var throttleSound = ReadString(settings, "throttlesound", string.Empty);
            var startSound = ReadString(settings, "startsound", "builtin1");
            var hornSound = ReadString(settings, "hornsound", "builtin1");
            var backfireSound = ReadString(settings, "backfiresound", string.Empty);
            var crashSound = ReadString(settings, "crashsound", "builtin1");
            var brakeSound = ReadString(settings, "brakesound", "builtin1");

            var hasWipers = 0;
            if (weather == TrackWeather.Rain)
                hasWipers = ReadInt(settings, "haswipers", 1);

            return new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                Acceleration = acceleration,
                Deceleration = deceleration,
                TopSpeed = topSpeed,
                IdleFreq = idleFreq,
                TopFreq = topFreq,
                ShiftFreq = shiftFreq,
                Gears = gears,
                Steering = steering,
                SteeringFactor = steeringFactor,
                HasWipers = hasWipers,
                EngineSound = ResolveSound(engineSound, builtinRoot, customVehiclesRoot, p => p.EngineSound),
                StartSound = ResolveSound(startSound, builtinRoot, customVehiclesRoot, p => p.StartSound),
                HornSound = ResolveSound(hornSound, builtinRoot, customVehiclesRoot, p => p.HornSound),
                ThrottleSound = ResolveSound(throttleSound, builtinRoot, customVehiclesRoot, p => p.ThrottleSound),
                CrashSound = ResolveSound(crashSound, builtinRoot, customVehiclesRoot, p => p.CrashSound),
                BrakeSound = ResolveSound(brakeSound, builtinRoot, customVehiclesRoot, p => p.BrakeSound),
                BackfireSound = ResolveSound(backfireSound, builtinRoot, customVehiclesRoot, p => p.BackfireSound)
            };
        }

        private static string? CombineSound(string root, string? file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return null;
            return Path.Combine(root, file);
        }

        private static string? ResolveSound(string? value, string builtinRoot, string customVehiclesRoot, Func<VehicleParameters, string?> selector)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value!.StartsWith(BuiltinPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value.Substring(BuiltinPrefix.Length), out var index))
                    return null;
                index -= 1;
                if (index < 0 || index >= VehicleCatalog.VehicleCount)
                    return null;
                var parameters = VehicleCatalog.Vehicles[index];
                var file = selector(parameters);
                return CombineSound(builtinRoot, file);
            }

            return Path.Combine(customVehiclesRoot, value);
        }

        private static Dictionary<string, string> ReadVehicleFile(string filePath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(filePath))
                return result;

            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                if (trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;
                var idx = trimmed.IndexOf('=');
                if (idx <= 0)
                    continue;
                var key = trimmed.Substring(0, idx).Trim();
                var value = trimmed.Substring(idx + 1).Trim();
                result[key] = value;
            }
            return result;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && int.TryParse(raw, out var value))
                return value;
            return defaultValue;
        }

        private static string ReadString(Dictionary<string, string> values, string key, string defaultValue)
        {
            if (values.TryGetValue(key, out var raw))
                return raw;
            return defaultValue;
        }
    }
}
