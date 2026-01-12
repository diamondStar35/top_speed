using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Geometry
{
    public enum TrackLayoutIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    public sealed class TrackLayoutIssue
    {
        public TrackLayoutIssueSeverity Severity { get; }
        public string Message { get; }
        public int? SpanIndex { get; }
        public string? Section { get; }

        public TrackLayoutIssue(TrackLayoutIssueSeverity severity, string message, int? spanIndex = null, string? section = null)
        {
            Severity = severity;
            Message = message;
            SpanIndex = spanIndex;
            Section = section;
        }

        public override string ToString()
        {
            var location = string.Empty;
            if (!string.IsNullOrWhiteSpace(Section))
                location = $"[{Section}] ";
            if (SpanIndex.HasValue)
                location += $"Span {SpanIndex.Value}: ";
            return $"{Severity}: {location}{Message}";
        }
    }

    public sealed class TrackLayoutValidationOptions
    {
        public float MinSpanLengthMeters { get; set; } = 5f;
        public float MinRadiusMeters { get; set; } = 15f;
        public float MaxRadiusMeters { get; set; } = 20000f;
        public float WarningBankDegrees { get; set; } = 8f;
        public float MaxBankDegrees { get; set; } = 15f;
        public float WarningSlopePercent { get; set; } = 6f;
        public float MaxSlopePercent { get; set; } = 12f;
        public float WarningCurvatureJump { get; set; } = 0.005f;
        public float MaxCurvatureJump { get; set; } = 0.01f;
        public float MinWidthMeters { get; set; } = 6f;
        public float WarningWidthMeters { get; set; } = 8f;
        public float WarningClothoidRatioMin { get; set; } = 0.1f;
        public float WarningClothoidRatioMax { get; set; } = 3.0f;
        public bool AllowZoneOverlap { get; set; } = false;
    }

    public sealed class TrackLayoutValidationResult
    {
        public IReadOnlyList<TrackLayoutIssue> Issues { get; }
        public bool IsValid { get; }

        public TrackLayoutValidationResult(IReadOnlyList<TrackLayoutIssue> issues)
        {
            Issues = issues;
            var valid = true;
            foreach (var issue in issues)
            {
                if (issue.Severity == TrackLayoutIssueSeverity.Error)
                {
                    valid = false;
                    break;
                }
            }
            IsValid = valid;
        }
    }

    public static class TrackLayoutValidator
    {
        public static TrackLayoutValidationResult Validate(TrackLayout layout, TrackLayoutValidationOptions? options = null)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var opts = options ?? new TrackLayoutValidationOptions();
            var issues = new List<TrackLayoutIssue>();

            ValidateGeometry(layout, opts, issues);
            ValidateZones(layout, opts, issues);
            ValidateMarkers(layout, issues);

            return new TrackLayoutValidationResult(issues);
        }

        private static void ValidateGeometry(TrackLayout layout, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            var spans = layout.Geometry.Spans;
            if (spans.Count == 0)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error, "Geometry spans are missing.", section: "geometry"));
                return;
            }

            var minRadius = float.MaxValue;
            var totalLength = 0f;

            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                totalLength += span.LengthMeters;

                if (span.LengthMeters < opts.MinSpanLengthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Span length {span.LengthMeters:0.###}m is very short.",
                        i,
                        "geometry"));
                }

                var slopePercent = span.LengthMeters > 0f ? Math.Abs(span.ElevationDeltaMeters / span.LengthMeters) * 100f : 0f;
                if (slopePercent > opts.MaxSlopePercent)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Slope {slopePercent:0.##}% exceeds max {opts.MaxSlopePercent:0.##}%.",
                        i,
                        "geometry"));
                }
                else if (slopePercent > opts.WarningSlopePercent)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Slope {slopePercent:0.##}% exceeds warning {opts.WarningSlopePercent:0.##}%.",
                        i,
                        "geometry"));
                }

                var bank = Math.Abs(span.BankDegrees);
                if (bank > opts.MaxBankDegrees)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Bank {bank:0.##}° exceeds max {opts.MaxBankDegrees:0.##}°.",
                        i,
                        "geometry"));
                }
                else if (bank > opts.WarningBankDegrees)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Bank {bank:0.##}° exceeds warning {opts.WarningBankDegrees:0.##}°.",
                        i,
                        "geometry"));
                }

                switch (span.Kind)
                {
                    case TrackGeometrySpanKind.Arc:
                        ValidateRadius(span.RadiusMeters, opts, i, issues);
                        minRadius = Math.Min(minRadius, span.RadiusMeters);
                        break;
                    case TrackGeometrySpanKind.Clothoid:
                        if (span.StartRadiusMeters > 0f)
                        {
                            ValidateRadius(span.StartRadiusMeters, opts, i, issues);
                            minRadius = Math.Min(minRadius, span.StartRadiusMeters);
                        }
                        if (span.EndRadiusMeters > 0f)
                        {
                            ValidateRadius(span.EndRadiusMeters, opts, i, issues);
                            minRadius = Math.Min(minRadius, span.EndRadiusMeters);
                        }
                        ValidateClothoidLength(span, opts, i, issues);
                        break;
                }

                if (span.Kind != TrackGeometrySpanKind.Straight &&
                    span.CurveSeverity == null)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Curved span has no curve severity for announcements.",
                        i,
                        "geometry"));
                }
            }

            if (totalLength < 200f)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Total length {totalLength:0.###}m is very short.",
                    section: "geometry"));
            }

            for (var i = 0; i < spans.Count; i++)
            {
                var current = spans[i];
                var next = spans[(i + 1) % spans.Count];
                var delta = Math.Abs(current.EndCurvature - next.StartCurvature);
                if (delta > opts.MaxCurvatureJump)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Curvature jump {delta:0.#####} 1/m exceeds max {opts.MaxCurvatureJump:0.#####}.",
                        i,
                        "geometry"));
                }
                else if (delta > opts.WarningCurvatureJump)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Curvature jump {delta:0.#####} 1/m exceeds warning {opts.WarningCurvatureJump:0.#####}.",
                        i,
                        "geometry"));
                }
            }

            if (layout.Geometry.SampleSpacingMeters <= 0f)
                return;

            if (minRadius < float.MaxValue)
            {
                var spacing = layout.Geometry.SampleSpacingMeters;
                if (spacing > minRadius / 2f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Sample spacing {spacing:0.###}m is too coarse for min radius {minRadius:0.###}m.",
                        section: "environment"));
                }
                else if (spacing > minRadius / 4f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Sample spacing {spacing:0.###}m may be too coarse for min radius {minRadius:0.###}m.",
                        section: "environment"));
                }
            }
        }

        private static void ValidateRadius(float radius, TrackLayoutValidationOptions opts, int spanIndex, List<TrackLayoutIssue> issues)
        {
            if (radius < opts.MinRadiusMeters)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                    $"Radius {radius:0.###}m below minimum {opts.MinRadiusMeters:0.###}m.",
                    spanIndex,
                    "geometry"));
            }
            else if (radius > opts.MaxRadiusMeters)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Radius {radius:0.###}m above recommended max {opts.MaxRadiusMeters:0.###}m.",
                    spanIndex,
                    "geometry"));
            }
        }

        private static void ValidateClothoidLength(TrackGeometrySpan span, TrackLayoutValidationOptions opts, int spanIndex, List<TrackLayoutIssue> issues)
        {
            var start = span.StartRadiusMeters;
            var end = span.EndRadiusMeters;
            if (start <= 0f && end <= 0f)
                return;

            var avg = 0f;
            if (start > 0f && end > 0f)
                avg = (start + end) * 0.5f;
            else
                avg = Math.Max(start, end);

            if (avg <= 0f)
                return;

            var ratio = span.LengthMeters / avg;
            if (ratio < opts.WarningClothoidRatioMin)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Clothoid length ratio {ratio:0.###} (length/radius) is very short.",
                    spanIndex,
                    "geometry"));
            }
            else if (ratio > opts.WarningClothoidRatioMax)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Clothoid length ratio {ratio:0.###} (length/radius) is very long.",
                    spanIndex,
                    "geometry"));
            }
        }

        private static void ValidateZones(TrackLayout layout, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            var length = TrackGeometry.Build(layout.Geometry).LengthMeters;

            ValidateZoneRange(layout.SurfaceZones, length, "surface", issues);
            ValidateZoneRange(layout.NoiseZones, length, "noise", issues);
            ValidateWidthZones(layout.WidthZones, length, opts, issues);
            ValidateSpeedZones(layout.SpeedLimitZones, length, issues);

            if (!opts.AllowZoneOverlap)
            {
                CheckOverlaps(layout.SurfaceZones, "surface", issues, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(layout.NoiseZones, "noise", issues, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(layout.WidthZones, "width", issues, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(layout.SpeedLimitZones, "speed_limits", issues, zone => (zone.StartMeters, zone.EndMeters));
            }
        }

        private static void ValidateZoneRange<T>(IReadOnlyList<TrackZone<T>> zones, float length, string section, List<TrackLayoutIssue> issues)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Zone has negative start/end.",
                        i,
                        section));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Zone extends beyond track length.",
                        i,
                        section));
                }
            }
        }

        private static void ValidateWidthZones(IReadOnlyList<TrackWidthZone> zones, float length, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Width zone has negative start/end.",
                        i,
                        "width"));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Width zone extends beyond track length.",
                        i,
                        "width"));
                }
                if (zone.WidthMeters < opts.MinWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Width {zone.WidthMeters:0.###}m below minimum {opts.MinWidthMeters:0.###}m.",
                        i,
                        "width"));
                }
                else if (zone.WidthMeters < opts.WarningWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Width {zone.WidthMeters:0.###}m below recommended {opts.WarningWidthMeters:0.###}m.",
                        i,
                        "width"));
                }
            }
        }

        private static void ValidateSpeedZones(IReadOnlyList<TrackSpeedLimitZone> zones, float length, List<TrackLayoutIssue> issues)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Speed limit zone has negative start/end.",
                        i,
                        "speed_limits"));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Speed limit zone extends beyond track length.",
                        i,
                        "speed_limits"));
                }
                if (zone.MaxSpeedKph < 20f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Speed limit {zone.MaxSpeedKph:0.###} kph is very low.",
                        i,
                        "speed_limits"));
                }
            }
        }

        private static void ValidateMarkers(TrackLayout layout, List<TrackLayoutIssue> issues)
        {
            var length = TrackGeometry.Build(layout.Geometry).LengthMeters;
            for (var i = 0; i < layout.Markers.Count; i++)
            {
                var marker = layout.Markers[i];
                if (marker.PositionMeters < 0f || marker.PositionMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Marker '{marker.Name}' is outside track bounds.",
                        i,
                        "markers"));
                }
            }
        }

        private static void CheckOverlaps<T>(IReadOnlyList<T> zones, string section, List<TrackLayoutIssue> issues, Func<T, (float start, float end)> getRange)
        {
            var ranges = new List<(float start, float end, int index)>();
            for (var i = 0; i < zones.Count; i++)
            {
                var range = getRange(zones[i]);
                ranges.Add((range.start, range.end, i));
            }

            ranges.Sort((a, b) => a.start.CompareTo(b.start));
            for (var i = 1; i < ranges.Count; i++)
            {
                var prev = ranges[i - 1];
                var current = ranges[i];
                if (current.start < prev.end)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Zones {prev.index} and {current.index} overlap.",
                        current.index,
                        section));
                }
            }
        }
    }
}
