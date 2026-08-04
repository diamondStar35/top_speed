using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal readonly struct PlayerLoadout
    {
        public PlayerLoadout(CarType car, bool automaticTransmission)
            : this(car, automaticTransmission, string.Empty)
        {
        }

        public PlayerLoadout(CarType car, bool automaticTransmission, string vehicleHash)
        {
            Car = car;
            AutomaticTransmission = automaticTransmission;
            VehicleHash = vehicleHash ?? string.Empty;
        }

        public CarType Car { get; }
        public bool AutomaticTransmission { get; }

        // Custom vehicle package hash when Car == CustomVehicle; empty for built-in vehicles.
        public string VehicleHash { get; }
    }

}
