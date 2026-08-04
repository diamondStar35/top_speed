using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    // Derives which transmission modes a parsed custom vehicle supports. Shared so the server
    // (building the catalog) and the client (deciding whether to prompt automatic vs. manual)
    // agree exactly, without duplicating the family logic.
    public static class VehicleTransmissionSupport
    {
        public static bool SupportsAutomatic(CustomVehicleTsvData? vehicle)
        {
            foreach (var type in Effective(vehicle))
            {
                if (TransmissionTypes.IsAutomaticFamily(type))
                    return true;
            }

            return false;
        }

        public static bool SupportsManual(CustomVehicleTsvData? vehicle)
        {
            foreach (var type in Effective(vehicle))
            {
                if (type == TransmissionType.Manual)
                    return true;
            }

            return false;
        }

        private static IEnumerable<TransmissionType> Effective(CustomVehicleTsvData? vehicle)
        {
            if (vehicle == null)
                yield break;

            var supported = vehicle.SupportedTransmissionTypes;
            if (supported != null && supported.Length > 0)
            {
                foreach (var type in supported)
                    yield return type;
                yield break;
            }

            yield return vehicle.PrimaryTransmissionType;
        }
    }
}
