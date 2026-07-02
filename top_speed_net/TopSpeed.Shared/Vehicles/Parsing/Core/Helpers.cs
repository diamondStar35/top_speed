using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static string StripInlineComment(string raw)
        {
            var text = raw ?? string.Empty;
            var index = text.IndexOf('#');
            return index >= 0 ? text.Substring(0, index) : text;
        }

        private static bool TryParseSectionHeader(string line, out string sectionName)
        {
            sectionName = string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            if (!trimmed.StartsWith("[", StringComparison.Ordinal) ||
                !trimmed.EndsWith("]", StringComparison.Ordinal))
                return false;

            var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
            if (inner.Length == 0)
                return false;

            sectionName = NormalizeKey(inner);
            return sectionName.Length > 0;
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var index = line.IndexOf('=');
            if (index <= 0 || index >= line.Length - 1)
                return false;

            key = line.Substring(0, index).Trim();
            value = line.Substring(index + 1).Trim();
            return key.Length > 0;
        }

        private static string NormalizeKey(string key)
        {
            var text = (key ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length == 0)
                return string.Empty;
            return text.Replace('-', '_').Replace(' ', '_');
        }

        private static IReadOnlyList<string> ParseCsvStrings(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw
                .Split(',')
                .Select(token => token.Trim())
                .Where(token => token.Length > 0)
                .ToArray();
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var token = raw.Trim().ToLowerInvariant();
            switch (token)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasErrors(IReadOnlyList<VehicleTsvIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return false;

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == VehicleTsvIssueSeverity.Error)
                    return true;
            }

            return false;
        }

        private static float CalculateTireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var widthM = Math.Max(0, widthMm) / 1000f;
            var aspect = Math.Max(0, aspectPercent) / 100f;
            var sidewallHeightM = widthM * aspect;
            var rimDiameterM = Math.Max(0, rimInches) * 0.0254f;
            var tireDiameterM = rimDiameterM + (2f * sidewallHeightM);
            return tireDiameterM * (float)Math.PI;
        }
    }
}

