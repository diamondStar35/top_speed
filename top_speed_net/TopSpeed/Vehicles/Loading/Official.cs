using System;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles.Loader
{
    internal static class Official
    {
        public static VehicleDefinition Load(int vehicleIndex, TrackWeather weather)
        {
            if (vehicleIndex < 0 || vehicleIndex >= VehicleCatalog.VehicleCount)
                vehicleIndex = 0;

            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var vehiclesRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var currentVehicleFolder = $"Vehicle{vehicleIndex + 1}";
            var spec = Spec.FromParameters(parameters, weather);

            var def = new VehicleDefinition
            {
                CarType = (CarType)vehicleIndex,
                Name = parameters.Name,
                UserDefined = false
            };
            Spec.Apply(def, spec);

            foreach (VehicleAction action in Enum.GetValues(typeof(VehicleAction)))
            {
                var overridePath = parameters.GetSoundPath(action);
                if (!string.IsNullOrWhiteSpace(overridePath))
                    def.SetSoundPath(action, Path.Combine(vehiclesRoot, overridePath!));
                else
                    def.SetSoundPath(action, Sound.ResolveOfficialFallback(vehiclesRoot, currentVehicleFolder, action));
            }

            return def;
        }
    }
}

