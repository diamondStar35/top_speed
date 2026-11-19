using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles
{
    internal sealed partial class TorqueCurve
    {
        public static TorqueCurve? TryParse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (!TryParsePairs(text, out var pairs))
                return null;
            return FromPairs(pairs);
        }

        public static TorqueCurve? FromPowerCurve(string? text, PowerCurveUnit unit)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (!TryParsePairs(text, out var pairs))
                return null;

            var rpm = new float[pairs.Count];
            var torque = new float[pairs.Count];
            for (var i = 0; i < pairs.Count; i++)
            {
                var r = pairs[i].rpm;
                var power = pairs[i].value;
                rpm[i] = r;
                torque[i] = PowerToTorque(power, r, unit);
            }

            return new TorqueCurve(rpm, torque);
        }

        private static float PowerToTorque(float power, float rpm, PowerCurveUnit unit)
        {
            if (rpm <= 0f)
                return 0f;
            switch (unit)
            {
                case PowerCurveUnit.Horsepower:
                    return (power * 7127f) / rpm;
                case PowerCurveUnit.MetricHorsepower:
                    return (power * 7023f) / rpm;
                default:
                    return (power * 9549f) / rpm;
            }
        }

        private static bool TryParsePairs(string? text, out List<(float rpm, float value)> pairs)
        {
            pairs = new List<(float rpm, float value)>();
            var raw = text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var entries = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var trimmed = entry.Trim();
                if (trimmed.Length == 0)
                    continue;

                var parts = trimmed.Split(new[] { ':', '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    continue;
                if (!float.TryParse(parts[0].Trim(), out var rpm))
                    continue;
                if (!float.TryParse(parts[1].Trim(), out var value))
                    continue;

                pairs.Add((rpm, value));
            }

            return pairs.Count >= 2;
        }

        private static TorqueCurve? FromPairs(List<(float rpm, float value)> pairs)
        {
            if (pairs == null || pairs.Count < 2)
                return null;

            pairs.Sort((a, b) => a.rpm.CompareTo(b.rpm));
            var rpm = new float[pairs.Count];
            var torque = new float[pairs.Count];
            for (var i = 0; i < pairs.Count; i++)
            {
                rpm[i] = pairs[i].rpm;
                torque[i] = pairs[i].value;
            }

            return new TorqueCurve(rpm, torque);
        }
    }
}

