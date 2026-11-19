using System;
using TopSpeed.Data;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Loader;

namespace TopSpeed.Vehicles
{
    internal static class VehicleLoader
    {
        public static VehicleDefinition LoadOfficial(int vehicleIndex, TrackWeather weather)
        {
            return Official.Load(vehicleIndex, weather);
        }

        public static VehicleDefinition LoadCustom(string vehicleFile, TrackWeather weather)
        {
            return Custom.Load(vehicleFile, weather);
        }
    }
}

