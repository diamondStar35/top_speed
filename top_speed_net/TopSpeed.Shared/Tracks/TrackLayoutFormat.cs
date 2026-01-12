using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TopSpeed.Data;

namespace TopSpeed.Tracks.Geometry
{
    public sealed class TrackLayoutError
    {
        public int LineNumber { get; }
        public string Message { get; }
        public string? LineText { get; }

        public TrackLayoutError(int lineNumber, string message, string? lineText = null)
        {
            LineNumber = lineNumber;
            Message = message;
            LineText = lineText;
        }

        public override string ToString()
        {
            return LineText == null ? $"{LineNumber}: {Message}" : $"{LineNumber}: {Message} -> {LineText}";
        }
    }

    public sealed class TrackLayoutParseResult
    {
        public TrackLayout? Layout { get; }
        public IReadOnlyList<TrackLayoutError> Errors { get; }
        public bool IsSuccess => Errors.Count == 0 && Layout != null;

        public TrackLayoutParseResult(TrackLayout? layout, IReadOnlyList<TrackLayoutError> errors)
        {
            Layout = layout;
            Errors = errors;
        }
    }

    public static class TrackLayoutFormat
    {
        public const string FileExtension = ".tslayout";
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static TrackLayoutParseResult ParseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            var lines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
            return ParseLines(lines, path);
        }

        public static TrackLayoutParseResult ParseLines(IEnumerable<string> lines, string? sourceName = null)
        {
            var errors = new List<TrackLayoutError>();
            var spans = new List<TrackGeometrySpan>();
            var surfaceZones = new List<TrackZone<TrackSurface>>();
            var noiseZones = new List<TrackZone<TrackNoise>>();
            var widthZones = new List<TrackWidthZone>();
            var speedZones = new List<TrackSpeedLimitZone>();
            var markers = new List<TrackMarker>();

            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var section = string.Empty;
            var lineNumber = 0;

            foreach (var rawLine in lines)
            {
                lineNumber++;
                var line = StripComment(rawLine);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (TryParseSection(line, out var nextSection))
                {
                    section = nextSection;
                    continue;
                }

                switch (section)
                {
                    case "meta":
                        ParseKeyValue(line, lineNumber, meta, errors);
                        break;
                    case "environment":
                        ParseKeyValue(line, lineNumber, environment, errors);
                        break;
                    case "geometry":
                        if (TryParseGeometryLine(line, lineNumber, spans, errors))
                        {
                        }
                        break;
                    case "width":
                        if (TryParseWidthZone(line, lineNumber, widthZones, errors))
                        {
                        }
                        break;
                    case "surface":
                        if (TryParseSurfaceZone(line, lineNumber, surfaceZones, errors))
                        {
                        }
                        break;
                    case "noise":
                        if (TryParseNoiseZone(line, lineNumber, noiseZones, errors))
                        {
                        }
                        break;
                    case "speed_limits":
                        if (TryParseSpeedLimit(line, lineNumber, speedZones, errors))
                        {
                        }
                        break;
                    case "markers":
                        if (TryParseMarker(line, lineNumber, markers, errors))
                        {
                        }
                        break;
                    default:
                        errors.Add(new TrackLayoutError(lineNumber, $"Unknown or missing section '{section}'.", rawLine));
                        break;
                }
            }

            if (spans.Count == 0)
                errors.Add(new TrackLayoutError(0, "Geometry section is empty or missing."));

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);

            var metadata = BuildMetadata(meta);
            var weather = ParseEnum(environment, "weather", TrackWeather.Sunny, errors);
            var ambience = ParseEnum(environment, "ambience", TrackAmbience.NoAmbience, errors);
            var defaultSurface = ParseEnum(environment, "default_surface", TrackSurface.Asphalt, errors);
            var defaultNoise = ParseEnum(environment, "default_noise", TrackNoise.NoNoise, errors);
            var defaultWidth = ParseFloat(environment, "default_width", 12f, errors);
            var sampleSpacing = ParseFloat(environment, "sample_spacing", 1f, errors);
            var enforceClosure = ParseBool(environment, "enforce_closure", true, errors);

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);

            var geometrySpec = new TrackGeometrySpec(spans, sampleSpacingMeters: sampleSpacing, enforceClosure: enforceClosure);
            var layout = new TrackLayout(
                geometrySpec,
                weather,
                ambience,
                defaultSurface,
                defaultNoise,
                defaultWidth,
                metadata,
                surfaceZones,
                noiseZones,
                widthZones,
                speedZones,
                markers);

            return new TrackLayoutParseResult(layout, errors);
        }

        public static string Write(TrackLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var sb = new StringBuilder();
            sb.AppendLine("# Top Speed track layout");
            sb.AppendLine("[meta]");
            WriteValue(sb, "name", layout.Metadata.Name);
            WriteValue(sb, "short_name", layout.Metadata.ShortName);
            WriteValue(sb, "description", layout.Metadata.Description);
            WriteValue(sb, "author", layout.Metadata.Author);
            WriteValue(sb, "version", layout.Metadata.Version);
            WriteValue(sb, "source", layout.Metadata.Source);
            if (layout.Metadata.Tags.Count > 0)
                WriteValue(sb, "tags", string.Join(", ", layout.Metadata.Tags));

            sb.AppendLine();
            sb.AppendLine("[environment]");
            sb.AppendLine($"weather={layout.Weather.ToString().ToLowerInvariant()}");
            sb.AppendLine($"ambience={layout.Ambience.ToString().ToLowerInvariant()}");
            sb.AppendLine($"default_surface={layout.DefaultSurface.ToString().ToLowerInvariant()}");
            sb.AppendLine($"default_noise={layout.DefaultNoise.ToString().ToLowerInvariant()}");
            sb.AppendLine($"default_width={layout.DefaultWidthMeters.ToString("0.###", Culture)}");
            sb.AppendLine($"sample_spacing={layout.Geometry.SampleSpacingMeters.ToString("0.###", Culture)}");
            sb.AppendLine($"enforce_closure={(layout.Geometry.EnforceClosure ? "true" : "false")}");

            sb.AppendLine();
            sb.AppendLine("[geometry]");
            foreach (var span in layout.Geometry.Spans)
            {
                sb.AppendLine(FormatSpan(span));
            }

            if (layout.WidthZones.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[width]");
                foreach (var zone in layout.WidthZones)
                {
                    sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {FormatFloat(zone.WidthMeters)} {FormatFloat(zone.ShoulderLeftMeters)} {FormatFloat(zone.ShoulderRightMeters)}");
                }
            }

            if (layout.SurfaceZones.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[surface]");
                foreach (var zone in layout.SurfaceZones)
                {
                    sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {zone.Value.ToString().ToLowerInvariant()}");
                }
            }

            if (layout.NoiseZones.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[noise]");
                foreach (var zone in layout.NoiseZones)
                {
                    sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {zone.Value.ToString().ToLowerInvariant()}");
                }
            }

            if (layout.SpeedLimitZones.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[speed_limits]");
                foreach (var zone in layout.SpeedLimitZones)
                {
                    sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {FormatFloat(zone.MaxSpeedKph)}");
                }
            }

            if (layout.Markers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[markers]");
                foreach (var marker in layout.Markers)
                {
                    sb.AppendLine($"{marker.Name} {FormatFloat(marker.PositionMeters)}");
                }
            }

            return sb.ToString();
        }

        private static string FormatSpan(TrackGeometrySpan span)
        {
            switch (span.Kind)
            {
                case TrackGeometrySpanKind.Straight:
                    return $"straight {FormatFloat(span.LengthMeters)}";
                case TrackGeometrySpanKind.Arc:
                    return $"arc length={FormatFloat(span.LengthMeters)} radius={FormatFloat(span.RadiusMeters)} dir={span.Direction.ToString().ToLowerInvariant()}{FormatSeverity(span.CurveSeverity)}{FormatOptional(span)}";
                case TrackGeometrySpanKind.Clothoid:
                    return $"clothoid length={FormatFloat(span.LengthMeters)} start={FormatFloat(span.StartRadiusMeters)} end={FormatFloat(span.EndRadiusMeters)} dir={span.Direction.ToString().ToLowerInvariant()}{FormatSeverity(span.CurveSeverity)}{FormatOptional(span)}";
                default:
                    return $"straight {FormatFloat(span.LengthMeters)}";
            }
        }

        private static string FormatSeverity(TrackCurveSeverity? severity)
        {
            return severity.HasValue ? $" severity={severity.Value.ToString().ToLowerInvariant()}" : string.Empty;
        }

        private static string FormatOptional(TrackGeometrySpan span)
        {
            var parts = new List<string>();
            if (Math.Abs(span.ElevationDeltaMeters) > 0.0001f)
                parts.Add($"elevation={FormatFloat(span.ElevationDeltaMeters)}");
            if (Math.Abs(span.BankDegrees) > 0.0001f)
                parts.Add($"bank={FormatFloat(span.BankDegrees)}");
            return parts.Count == 0 ? string.Empty : " " + string.Join(" ", parts);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", Culture);
        }

        private static void WriteValue(StringBuilder sb, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{key}={value}");
        }

        private static TrackLayoutMetadata BuildMetadata(Dictionary<string, string> meta)
        {
            meta.TryGetValue("name", out var name);
            meta.TryGetValue("short_name", out var shortName);
            meta.TryGetValue("description", out var description);
            meta.TryGetValue("author", out var author);
            meta.TryGetValue("version", out var version);
            meta.TryGetValue("source", out var source);
            var tags = ParseTags(meta.TryGetValue("tags", out var tagValue) ? tagValue : null);

            return new TrackLayoutMetadata(name, shortName, description, author, version, source, tags);
        }

        private static IReadOnlyList<string> ParseTags(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var parts = value!.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool TryParseGeometryLine(string line, int lineNumber, List<TrackGeometrySpan> spans, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count == 0)
                return false;

            var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var positional = new List<string>();

            foreach (var token in tokens)
            {
                if (TrySplitKeyValue(token, out var key, out var value))
                    named[key] = value;
                else
                    positional.Add(token);
            }

            string? kind = null;
            var kindFromPositional = false;
            if (named.TryGetValue("kind", out var kindValue) || named.TryGetValue("type", out kindValue))
                kind = kindValue;
            else if (positional.Count > 0 && !LooksNumeric(positional[0]))
            {
                kind = positional[0];
                kindFromPositional = true;
            }

            if (string.IsNullOrWhiteSpace(kind))
            {
                errors.Add(new TrackLayoutError(lineNumber, "Geometry span missing kind.", line));
                return false;
            }

            var spanKind = ParseSpanKind(kind!);
            if (spanKind == null)
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Unknown geometry kind '{kind}'.", line));
                return false;
            }

            if (kindFromPositional)
                positional.RemoveAt(0);

            var length = GetFloat(named, "length", "len", positional, 0, errors, lineNumber, line);
            if (length <= 0f)
                return false;

            var direction = ParseDirection(named, positional);
            var severity = ParseSeverity(named, positional);
            var elevation = GetFloat(named, "elevation", "elev", positional, -1, out var elevFound) ? elevFound : 0f;
            var bank = GetFloat(named, "bank", "bankdeg", positional, -1, out var bankFound) ? bankFound : 0f;

            try
            {
                switch (spanKind.Value)
                {
                    case TrackGeometrySpanKind.Straight:
                        spans.Add(TrackGeometrySpan.Straight(length, elevationDeltaMeters: elevation, bankDegrees: bank));
                        break;
                    case TrackGeometrySpanKind.Arc:
                        var radius = GetFloat(named, "radius", "r", positional, 1, errors, lineNumber, line);
                        spans.Add(TrackGeometrySpan.Arc(length, radius, direction, severity, elevation, bank));
                        break;
                    case TrackGeometrySpanKind.Clothoid:
                        var startRadius = GetFloat(named, "start", "startRadius", positional, 1, errors, lineNumber, line);
                        var endRadius = GetFloat(named, "end", "endRadius", positional, 2, errors, lineNumber, line);
                        spans.Add(TrackGeometrySpan.Clothoid(length, startRadius, endRadius, direction, severity, elevation, bank));
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseWidthZone(string line, int lineNumber, List<TrackWidthZone> zones, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count == 0)
                return false;

            var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var positional = new List<string>();
            foreach (var token in tokens)
            {
                if (TrySplitKeyValue(token, out var key, out var value))
                    named[key] = value;
                else
                    positional.Add(token);
            }

            var start = GetFloat(named, "start", "s", positional, 0, errors, lineNumber, line);
            var end = GetFloat(named, "end", "e", positional, 1, errors, lineNumber, line);
            var width = GetFloat(named, "width", "w", positional, 2, errors, lineNumber, line);
            var shoulderLeft = GetFloat(named, "shoulder_left", "sl", positional, 3, out var leftFound) ? leftFound : 0f;
            var shoulderRight = GetFloat(named, "shoulder_right", "sr", positional, 4, out var rightFound) ? rightFound : 0f;

            try
            {
                zones.Add(new TrackWidthZone(start, end, width, shoulderLeft, shoulderRight));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseSurfaceZone(string line, int lineNumber, List<TrackZone<TrackSurface>> zones, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count < 3)
            {
                errors.Add(new TrackLayoutError(lineNumber, "Surface zone requires: start end surface.", line));
                return false;
            }

            var start = ParseFloat(tokens[0], lineNumber, errors, line);
            var end = ParseFloat(tokens[1], lineNumber, errors, line);
            var surface = ParseEnum<TrackSurface>(tokens[2], lineNumber, errors, line);
            if (surface == null)
                return false;

            try
            {
                zones.Add(new TrackZone<TrackSurface>(start, end, surface.Value));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }
            return true;
        }

        private static bool TryParseNoiseZone(string line, int lineNumber, List<TrackZone<TrackNoise>> zones, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count < 3)
            {
                errors.Add(new TrackLayoutError(lineNumber, "Noise zone requires: start end noise.", line));
                return false;
            }

            var start = ParseFloat(tokens[0], lineNumber, errors, line);
            var end = ParseFloat(tokens[1], lineNumber, errors, line);
            var noise = ParseEnum<TrackNoise>(tokens[2], lineNumber, errors, line);
            if (noise == null)
                return false;

            try
            {
                zones.Add(new TrackZone<TrackNoise>(start, end, noise.Value));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }
            return true;
        }

        private static bool TryParseSpeedLimit(string line, int lineNumber, List<TrackSpeedLimitZone> zones, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count < 3)
            {
                errors.Add(new TrackLayoutError(lineNumber, "Speed limit requires: start end kph.", line));
                return false;
            }

            var start = ParseFloat(tokens[0], lineNumber, errors, line);
            var end = ParseFloat(tokens[1], lineNumber, errors, line);
            var limit = ParseFloat(tokens[2], lineNumber, errors, line);
            if (tokens.Count >= 4 && tokens[3].Equals("mps", StringComparison.OrdinalIgnoreCase))
                limit *= 3.6f;

            try
            {
                zones.Add(new TrackSpeedLimitZone(start, end, limit));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }
            return true;
        }

        private static bool TryParseMarker(string line, int lineNumber, List<TrackMarker> markers, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count < 2)
            {
                errors.Add(new TrackLayoutError(lineNumber, "Marker requires: name position.", line));
                return false;
            }

            var name = tokens[0];
            var position = ParseFloat(tokens[1], lineNumber, errors, line);
            try
            {
                markers.Add(new TrackMarker(name, position));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }
            return true;
        }

        private static TrackCurveDirection ParseDirection(Dictionary<string, string> named, List<string> positional)
        {
            if (named.TryGetValue("dir", out var dirValue) || named.TryGetValue("direction", out dirValue))
                return ParseDirection(dirValue);

            foreach (var token in positional)
            {
                var dir = ParseDirection(token);
                if (dir != TrackCurveDirection.Straight)
                    return dir;
            }

            return TrackCurveDirection.Straight;
        }

        private static TrackCurveDirection ParseDirection(string value)
        {
            if (value.Equals("left", StringComparison.OrdinalIgnoreCase))
                return TrackCurveDirection.Left;
            if (value.Equals("right", StringComparison.OrdinalIgnoreCase))
                return TrackCurveDirection.Right;
            return TrackCurveDirection.Straight;
        }

        private static TrackCurveSeverity? ParseSeverity(Dictionary<string, string> named, List<string> positional)
        {
            if (named.TryGetValue("severity", out var severityValue) || named.TryGetValue("curve", out severityValue))
                return ParseSeverity(severityValue);

            foreach (var token in positional)
            {
                var severity = ParseSeverity(token);
                if (severity.HasValue)
                    return severity;
            }

            return null;
        }

        private static TrackCurveSeverity? ParseSeverity(string value)
        {
            if (value.Equals("easy", StringComparison.OrdinalIgnoreCase))
                return TrackCurveSeverity.Easy;
            if (value.Equals("normal", StringComparison.OrdinalIgnoreCase))
                return TrackCurveSeverity.Normal;
            if (value.Equals("hard", StringComparison.OrdinalIgnoreCase))
                return TrackCurveSeverity.Hard;
            if (value.Equals("hairpin", StringComparison.OrdinalIgnoreCase))
                return TrackCurveSeverity.Hairpin;
            return null;
        }

        private static TrackGeometrySpanKind? ParseSpanKind(string value)
        {
            if (value.Equals("straight", StringComparison.OrdinalIgnoreCase))
                return TrackGeometrySpanKind.Straight;
            if (value.Equals("arc", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("curve", StringComparison.OrdinalIgnoreCase))
                return TrackGeometrySpanKind.Arc;
            if (value.Equals("clothoid", StringComparison.OrdinalIgnoreCase))
                return TrackGeometrySpanKind.Clothoid;
            return null;
        }

        private static void ParseKeyValue(string line, int lineNumber, Dictionary<string, string> target, List<TrackLayoutError> errors)
        {
            if (!TrySplitKeyValue(line, out var key, out var value))
            {
                errors.Add(new TrackLayoutError(lineNumber, "Expected key=value.", line));
                return;
            }
            target[key] = value;
        }

        private static bool TrySplitKeyValue(string token, out string key, out string value)
        {
            var idx = token.IndexOf('=');
            if (idx < 0)
                idx = token.IndexOf(':');
            if (idx <= 0)
            {
                key = string.Empty;
                value = string.Empty;
                return false;
            }
            key = token.Substring(0, idx).Trim();
            value = token.Substring(idx + 1).Trim();
            return !string.IsNullOrWhiteSpace(key);
        }

        private static bool TryParseSection(string line, out string section)
        {
            section = string.Empty;
            var trimmed = line.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                section = trimmed.Substring(1, trimmed.Length - 2).Trim().ToLowerInvariant();
                return true;
            }
            return false;
        }

        private static string StripComment(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
                return string.Empty;

            return line.Trim();
        }

        private static List<string> SplitTokens(string line)
        {
            var tokens = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            return tokens.ToList();
        }

        private static bool LooksNumeric(string token)
        {
            return float.TryParse(token, NumberStyles.Float, Culture, out _);
        }

        private static float GetFloat(Dictionary<string, string> named, string key, string altKey, List<string> positional, int positionalIndex, List<TrackLayoutError> errors, int lineNumber, string line)
        {
            if (named.TryGetValue(key, out var value) || named.TryGetValue(altKey, out value))
                return ParseFloat(value, lineNumber, errors, line);

            if (positionalIndex >= 0 && positionalIndex < positional.Count)
                return ParseFloat(positional[positionalIndex], lineNumber, errors, line);

            errors.Add(new TrackLayoutError(lineNumber, $"Missing {key}.", line));
            return 0f;
        }

        private static bool GetFloat(Dictionary<string, string> named, string key, string altKey, List<string> positional, int positionalIndex, out float value)
        {
            if (named.TryGetValue(key, out var namedValue) || named.TryGetValue(altKey, out namedValue))
            {
                if (float.TryParse(namedValue, NumberStyles.Float, Culture, out value))
                    return true;
            }

            if (positionalIndex >= 0 && positionalIndex < positional.Count &&
                float.TryParse(positional[positionalIndex], NumberStyles.Float, Culture, out value))
                return true;

            value = 0f;
            return false;
        }

        private static float ParseFloat(string value, int lineNumber, List<TrackLayoutError> errors, string line)
        {
            if (!float.TryParse(value, NumberStyles.Float, Culture, out var result))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid number '{value}'.", line));
                return 0f;
            }
            return result;
        }

        private static T? ParseEnum<T>(string value, int lineNumber, List<TrackLayoutError> errors, string line) where T : struct
        {
            if (Enum.TryParse<T>(value, true, out var result))
                return result;
            errors.Add(new TrackLayoutError(lineNumber, $"Unknown value '{value}'.", line));
            return null;
        }

        private static T ParseEnum<T>(Dictionary<string, string> named, string key, T fallback, List<TrackLayoutError> errors) where T : struct
        {
            if (!named.TryGetValue(key, out var value))
                return fallback;
            var parsed = ParseEnum<T>(value, 0, errors, value);
            return parsed ?? fallback;
        }

        private static float ParseFloat(Dictionary<string, string> named, string key, float fallback, List<TrackLayoutError> errors)
        {
            if (!named.TryGetValue(key, out var value))
                return fallback;
            if (!float.TryParse(value, NumberStyles.Float, Culture, out var result))
            {
                errors.Add(new TrackLayoutError(0, $"Invalid number '{value}' for {key}."));
                return fallback;
            }
            return result;
        }

        private static bool ParseBool(Dictionary<string, string> named, string key, bool fallback, List<TrackLayoutError> errors)
        {
            if (!named.TryGetValue(key, out var value))
                return fallback;
            if (bool.TryParse(value, out var result))
                return result;
            if (value.Equals("1", StringComparison.OrdinalIgnoreCase))
                return true;
            if (value.Equals("0", StringComparison.OrdinalIgnoreCase))
                return false;
            errors.Add(new TrackLayoutError(0, $"Invalid bool '{value}' for {key}."));
            return fallback;
        }
    }
}
