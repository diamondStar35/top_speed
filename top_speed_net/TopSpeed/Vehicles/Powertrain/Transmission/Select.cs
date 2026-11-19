using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles
{
    internal static class TransmissionSelect
    {
        public static bool TryResolveRequested(
            bool automaticRequested,
            TransmissionType primary,
            IReadOnlyList<TransmissionType>? supported,
            out TransmissionType resolved)
        {
            resolved = primary;

            var values = NormalizeSupported(primary, supported);
            if (automaticRequested)
                return TryResolveAutomatic(values, primary, out resolved);

            if (Contains(values, TransmissionType.Manual))
            {
                resolved = TransmissionType.Manual;
                return true;
            }

            return false;
        }

        public static bool SupportsAutomatic(IReadOnlyList<TransmissionType>? supported)
        {
            if (supported == null)
                return false;
            for (var i = 0; i < supported.Count; i++)
            {
                if (TransmissionTypes.IsAutomaticFamily(supported[i]))
                    return true;
            }

            return false;
        }

        public static bool SupportsManual(IReadOnlyList<TransmissionType>? supported)
        {
            return Contains(supported, TransmissionType.Manual);
        }

        public static bool TryResolveSingleMode(
            TransmissionType primary,
            IReadOnlyList<TransmissionType>? supported,
            out bool automatic)
        {
            automatic = false;
            var values = NormalizeSupported(primary, supported);
            var supportsAutomatic = SupportsAutomatic(values);
            var supportsManual = SupportsManual(values);
            if (supportsAutomatic == supportsManual)
                return false;

            automatic = supportsAutomatic;
            return true;
        }

        private static bool TryResolveAutomatic(
            IReadOnlyList<TransmissionType> supported,
            TransmissionType primary,
            out TransmissionType resolved)
        {
            resolved = primary;
            if (TransmissionTypes.IsAutomaticFamily(primary) && Contains(supported, primary))
                return true;

            for (var i = 0; i < supported.Count; i++)
            {
                var type = supported[i];
                if (!TransmissionTypes.IsAutomaticFamily(type))
                    continue;

                resolved = type;
                return true;
            }

            return false;
        }

        private static TransmissionType[] NormalizeSupported(
            TransmissionType primary,
            IReadOnlyList<TransmissionType>? supported)
        {
            if (supported == null || supported.Count == 0)
                return new[] { primary };

            var copy = new TransmissionType[supported.Count];
            for (var i = 0; i < supported.Count; i++)
                copy[i] = supported[i];
            return copy;
        }

        private static bool Contains(IReadOnlyList<TransmissionType>? values, TransmissionType expected)
        {
            if (values == null)
                return false;

            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == expected)
                    return true;
            }

            return false;
        }
    }
}

