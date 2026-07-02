using System;

namespace TopSpeed.Protocol
{
    public enum RoomVehicleSelectionKind : byte
    {
        None = 0,
        BuiltIn = 1,
        CustomPackage = 2
    }

    public sealed class VehiclePackageRef
    {
        public RoomVehicleSelectionKind Kind = RoomVehicleSelectionKind.None;
        public byte BuiltInCar;
        public string VehicleId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;

        public bool IsBuiltIn => Kind == RoomVehicleSelectionKind.BuiltIn;
        public bool IsCustomPackage => Kind == RoomVehicleSelectionKind.CustomPackage;

        public static VehiclePackageRef None()
        {
            return new VehiclePackageRef { Kind = RoomVehicleSelectionKind.None };
        }

        public static VehiclePackageRef BuiltIn(byte car)
        {
            return new VehiclePackageRef
            {
                Kind = RoomVehicleSelectionKind.BuiltIn,
                BuiltInCar = car
            };
        }

        public static VehiclePackageRef Custom(string vehicleId, string version, string hash)
        {
            return new VehiclePackageRef
            {
                Kind = RoomVehicleSelectionKind.CustomPackage,
                VehicleId = (vehicleId ?? string.Empty).Trim(),
                Version = (version ?? string.Empty).Trim(),
                Hash = NormalizeHash(hash)
            };
        }

        public static VehiclePackageRef Clone(VehiclePackageRef? vehicle)
        {
            if (vehicle == null)
                return None();

            if (vehicle.IsCustomPackage)
                return Custom(vehicle.VehicleId, vehicle.Version, vehicle.Hash);
            if (vehicle.IsBuiltIn)
                return BuiltIn(vehicle.BuiltInCar);
            return None();
        }

        public static bool AreEqual(VehiclePackageRef? left, VehiclePackageRef? right)
        {
            var a = Clone(left);
            var b = Clone(right);
            if (a.Kind != b.Kind)
                return false;

            if (a.IsCustomPackage)
            {
                return string.Equals(a.VehicleId, b.VehicleId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(a.Version, b.Version, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(NormalizeHash(a.Hash), NormalizeHash(b.Hash), StringComparison.OrdinalIgnoreCase);
            }

            if (a.IsBuiltIn)
                return a.BuiltInCar == b.BuiltInCar;

            return true;
        }

        public static string NormalizeHash(string hash)
        {
            return (hash ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
