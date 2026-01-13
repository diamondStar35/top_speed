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
        public const string FileExtension = ".ttl";
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
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var nodes = new Dictionary<string, NodeBuilder>(StringComparer.OrdinalIgnoreCase);
            var edges = new Dictionary<string, EdgeBuilder>(StringComparer.OrdinalIgnoreCase);
            var routes = new List<RouteBuilder>();
            var edgeOrder = new List<string>();

            var section = SectionInfo.Empty;
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

                switch (section.Kind)
                {
                    case "meta":
                        ParseKeyValue(line, lineNumber, meta, errors);
                        break;
                    case "environment":
                        ParseKeyValue(line, lineNumber, environment, errors);
                        break;
                    case "nodes":
                        TryParseNodeLine(line, lineNumber, nodes, errors);
                        break;
                    case "edges":
                        TryParseEdgeLine(line, lineNumber, edges, edgeOrder, errors);
                        break;
                    case "node":
                        if (string.IsNullOrWhiteSpace(section.NodeId))
                        {
                            errors.Add(new TrackLayoutError(lineNumber, "Node section missing id.", rawLine));
                            break;
                        }
                        var node = GetOrCreateNode(section.NodeId!, nodes);
                        if (string.IsNullOrWhiteSpace(section.Subsection))
                        {
                            ParseNodeProperties(line, lineNumber, node, errors);
                        }
                        else
                        {
                            switch (section.Subsection)
                            {
                                case "intersection":
                                    ParseIntersectionProperties(line, lineNumber, node, errors);
                                    break;
                                case "legs":
                                    TryParseIntersectionLeg(line, lineNumber, node, errors);
                                    break;
                                case "connectors":
                                    TryParseIntersectionConnector(line, lineNumber, node, errors);
                                    break;
                                case "lanes":
                                    TryParseIntersectionLane(line, lineNumber, node, errors);
                                    break;
                                case "lane_links":
                                case "lanelinks":
                                    TryParseIntersectionLaneLink(line, lineNumber, node, errors);
                                    break;
                                case "areas":
                                    TryParseIntersectionArea(line, lineNumber, node, errors);
                                    break;
                                default:
                                    errors.Add(new TrackLayoutError(lineNumber, $"Unknown node section '{section.Subsection}'.", rawLine));
                                    break;
                            }
                        }
                        break;
                    case "routes":
                        TryParseRouteLine(line, lineNumber, routes, errors);
                        break;
                    case "edge":
                        if (string.IsNullOrWhiteSpace(section.EdgeId))
                        {
                            errors.Add(new TrackLayoutError(lineNumber, "Edge section missing id.", rawLine));
                            break;
                        }
                        var edge = GetOrCreateEdge(section.EdgeId!, edges, edgeOrder);
                        if (string.IsNullOrWhiteSpace(section.Subsection))
                        {
                            ParseEdgeProperties(line, lineNumber, edge, errors);
                        }
                        else
                        {
                            switch (section.Subsection)
                            {
                                case "geometry":
                                    TryParseGeometryLine(line, lineNumber, edge.GeometrySpans, errors);
                                    break;
                                case "width":
                                    TryParseWidthZone(line, lineNumber, edge.WidthZones, errors);
                                    break;
                                case "surface":
                                    TryParseSurfaceZone(line, lineNumber, edge.SurfaceZones, errors);
                                    break;
                                case "noise":
                                    TryParseNoiseZone(line, lineNumber, edge.NoiseZones, errors);
                                    break;
                                case "speed_limits":
                                    TryParseSpeedLimit(line, lineNumber, edge.SpeedZones, errors);
                                    break;
                                case "markers":
                                    TryParseMarker(line, lineNumber, edge.Markers, errors);
                                    break;
                                case "weather":
                                    TryParseWeatherZone(line, lineNumber, edge.WeatherZones, errors);
                                    break;
                                case "ambience":
                                    TryParseAmbienceZone(line, lineNumber, edge.AmbienceZones, errors);
                                    break;
                                case "hazards":
                                    TryParseHazard(line, lineNumber, edge.Hazards, errors);
                                    break;
                                case "checkpoints":
                                    TryParseCheckpoint(line, lineNumber, edge.Checkpoints, errors);
                                    break;
                                case "hit_lanes":
                                    TryParseHitLanes(line, lineNumber, edge.HitLanes, errors);
                                    break;
                                case "allowed_vehicles":
                                    TryParseAllowedVehicles(line, lineNumber, edge.AllowedVehicles, errors);
                                    break;
                                case "emitters":
                                    TryParseEmitter(line, lineNumber, edge.Emitters, errors);
                                    break;
                                case "triggers":
                                    TryParseTrigger(line, lineNumber, edge.Triggers, errors);
                                    break;
                                case "boundaries":
                                    TryParseBoundary(line, lineNumber, edge.Boundaries, errors);
                                    break;
                                default:
                                    errors.Add(new TrackLayoutError(lineNumber, $"Unknown edge section '{section.Subsection}'.", rawLine));
                                    break;
                            }
                        }
                        break;
                    default:
                        errors.Add(new TrackLayoutError(lineNumber, $"Unknown or missing section '{section.Kind}'.", rawLine));
                        break;
                }
            }

            if (edges.Count == 0)
                errors.Add(new TrackLayoutError(0, "No edges defined."));

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
            var primaryRouteId = ParseString(environment, "primary_route");

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);

            foreach (var edge in edges.Values)
            {
                if (!string.IsNullOrWhiteSpace(edge.FromNodeId))
                    EnsureNode(edge.FromNodeId!, nodes);
                if (!string.IsNullOrWhiteSpace(edge.ToNodeId))
                    EnsureNode(edge.ToNodeId!, nodes);
            }

            if (nodes.Count == 0)
                errors.Add(new TrackLayoutError(0, "No nodes defined."));

            if (routes.Count == 0)
            {
                var fallbackId = string.IsNullOrWhiteSpace(primaryRouteId) ? "primary" : primaryRouteId!;
                routes.Add(new RouteBuilder(fallbackId, new List<string>(edgeOrder), isLoop: null));
            }

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);

            var nodeList = new List<TrackGraphNode>(nodes.Count);
            foreach (var node in nodes.Values)
            {
                nodeList.Add(new TrackGraphNode(
                    node.Id,
                    node.Name,
                    node.ShortName,
                    node.Metadata,
                    node.Intersection?.Build()));
            }

            var edgeList = new List<TrackGraphEdge>(edgeOrder.Count);
            foreach (var edgeId in edgeOrder)
            {
                if (!edges.TryGetValue(edgeId, out var builder))
                    continue;
                var edge = builder.Build(defaultSurface, defaultNoise, defaultWidth, weather, ambience, sampleSpacing, enforceClosure, errors);
                if (edge != null)
                    edgeList.Add(edge);
            }

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);

            var routeList = new List<TrackGraphRoute>(routes.Count);
            foreach (var route in routes)
            {
                var missing = route.EdgeIds.Find(id => !edges.ContainsKey(id));
                if (missing != null)
                {
                    errors.Add(new TrackLayoutError(0, $"Route '{route.Id}' references missing edge '{missing}'."));
                    continue;
                }
                var isLoop = route.IsLoop ?? InferRouteLoop(route.EdgeIds, edges);
                routeList.Add(new TrackGraphRoute(route.Id, route.EdgeIds, isLoop));
            }

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);

            if (!string.IsNullOrWhiteSpace(primaryRouteId) &&
                !routeList.Exists(route => route.Id.Equals(primaryRouteId, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new TrackLayoutError(0, $"Primary route '{primaryRouteId}' not found."));
                return new TrackLayoutParseResult(null, errors);
            }

            var graph = new TrackGraph(nodeList, edgeList, routeList, primaryRouteId);
            var layout = new TrackLayout(
                graph,
                weather,
                ambience,
                defaultSurface,
                defaultNoise,
                defaultWidth,
                metadata);

            return new TrackLayoutParseResult(layout, errors);
        }
        public static string Write(TrackLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var sb = new StringBuilder();
            sb.AppendLine("# Top Speed track graph");
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
            sb.AppendLine($"default_width={FormatFloat(layout.DefaultWidthMeters)}");
            sb.AppendLine($"sample_spacing={FormatFloat(layout.Geometry.SampleSpacingMeters)}");
            sb.AppendLine($"enforce_closure={FormatBool(layout.Geometry.EnforceClosure)}");
            if (!string.IsNullOrWhiteSpace(layout.Graph.PrimaryRouteId))
                sb.AppendLine($"primary_route={layout.Graph.PrimaryRouteId}");

            if (layout.Graph.Nodes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[nodes]");
                foreach (var node in layout.Graph.Nodes)
                {
                    var line = new StringBuilder();
                    line.Append("id=").Append(node.Id);
                    AppendInlineValue(line, "name", node.Name);
                    AppendInlineValue(line, "short_name", node.ShortName);
                    foreach (var kvp in node.Metadata)
                        AppendInlineValue(line, kvp.Key, kvp.Value);
                    sb.AppendLine(line.ToString());
                }
            }

            foreach (var node in layout.Graph.Nodes)
            {
                if (node.Intersection == null)
                    continue;
                sb.AppendLine();
                sb.AppendLine($"[node {node.Id}.intersection]");
                var intersection = node.Intersection;
                if (intersection.Shape != TrackIntersectionShape.Unspecified)
                    WriteValue(sb, "shape", FormatIntersectionShape(intersection.Shape));
                if (intersection.RadiusMeters > 0f)
                    WriteValue(sb, "radius", FormatFloat(intersection.RadiusMeters));
                if (intersection.InnerRadiusMeters > 0f)
                    WriteValue(sb, "inner_radius", FormatFloat(intersection.InnerRadiusMeters));
                if (intersection.OuterRadiusMeters > 0f)
                    WriteValue(sb, "outer_radius", FormatFloat(intersection.OuterRadiusMeters));
                if (intersection.EntryLanes > 0)
                    WriteValue(sb, "entry_lanes", intersection.EntryLanes.ToString(Culture));
                if (intersection.ExitLanes > 0)
                    WriteValue(sb, "exit_lanes", intersection.ExitLanes.ToString(Culture));
                if (intersection.TurnLanes > 0)
                    WriteValue(sb, "turn_lanes", intersection.TurnLanes.ToString(Culture));
                if (intersection.SpeedLimitKph > 0f)
                    WriteValue(sb, "speed_limit", FormatFloat(intersection.SpeedLimitKph));
                if (intersection.Control != TrackIntersectionControl.None)
                    WriteValue(sb, "control", FormatIntersectionControl(intersection.Control));
                if (intersection.Priority != 0)
                    WriteValue(sb, "priority", intersection.Priority.ToString(Culture));
                foreach (var kvp in intersection.Metadata)
                    sb.AppendLine($"{kvp.Key}={EncodeValue(kvp.Value)}");

                if (intersection.Legs.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[node {node.Id}.legs]");
                    foreach (var leg in intersection.Legs)
                        sb.AppendLine(FormatIntersectionLeg(leg));
                }

                if (intersection.Connectors.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[node {node.Id}.connectors]");
                    foreach (var connector in intersection.Connectors)
                        sb.AppendLine(FormatIntersectionConnector(connector));
                }

                if (intersection.Lanes.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[node {node.Id}.lanes]");
                    foreach (var lane in intersection.Lanes)
                        sb.AppendLine(FormatIntersectionLane(lane));
                }

                if (intersection.LaneLinks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[node {node.Id}.lane_links]");
                    foreach (var link in intersection.LaneLinks)
                        sb.AppendLine(FormatIntersectionLaneLink(link));
                }

                if (intersection.Areas.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[node {node.Id}.areas]");
                    foreach (var area in intersection.Areas)
                        sb.AppendLine(FormatIntersectionArea(area));
                }
            }

            if (layout.Graph.Edges.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[edges]");
                foreach (var edge in layout.Graph.Edges)
                {
                    sb.AppendLine($"id={edge.Id} from={edge.FromNodeId} to={edge.ToNodeId}");
                }
            }

            if (layout.Graph.Routes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[routes]");
                foreach (var route in layout.Graph.Routes)
                {
                    var edges = string.Join(",", route.EdgeIds);
                    sb.AppendLine($"id={route.Id} edges={edges} is_loop={FormatBool(route.IsLoop)}");
                }
            }

            foreach (var edge in layout.Graph.Edges)
            {
                sb.AppendLine();
                sb.AppendLine($"[edge {edge.Id}]");
                WriteValue(sb, "name", edge.Name);
                WriteValue(sb, "short_name", edge.ShortName);
                if (edge.ConnectorFromEdgeIds.Count > 0)
                    WriteValue(sb, "connector_from", string.Join(",", edge.ConnectorFromEdgeIds));
                if (edge.TurnDirection != TrackTurnDirection.Unknown)
                    WriteValue(sb, "turn", FormatTurnDirection(edge.TurnDirection));
                sb.AppendLine($"from={edge.FromNodeId}");
                sb.AppendLine($"to={edge.ToNodeId}");
                sb.AppendLine($"default_surface={edge.Profile.DefaultSurface.ToString().ToLowerInvariant()}");
                sb.AppendLine($"default_noise={edge.Profile.DefaultNoise.ToString().ToLowerInvariant()}");
                sb.AppendLine($"default_width={FormatFloat(edge.Profile.DefaultWidthMeters)}");
                sb.AppendLine($"default_weather={edge.Profile.DefaultWeather.ToString().ToLowerInvariant()}");
                sb.AppendLine($"default_ambience={edge.Profile.DefaultAmbience.ToString().ToLowerInvariant()}");
                if (edge.Profile.AllowedVehicles.Count > 0)
                    sb.AppendLine($"allowed_vehicles={string.Join(",", edge.Profile.AllowedVehicles)}");
                foreach (var kvp in edge.Metadata)
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");

                sb.AppendLine();
                sb.AppendLine($"[edge {edge.Id}.geometry]");
                foreach (var span in edge.Geometry.Spans)
                {
                    sb.AppendLine(FormatSpan(span));
                }

                if (edge.Profile.WidthZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.width]");
                    foreach (var zone in edge.Profile.WidthZones)
                    {
                        sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {FormatFloat(zone.WidthMeters)} {FormatFloat(zone.ShoulderLeftMeters)} {FormatFloat(zone.ShoulderRightMeters)}");
                    }
                }

                if (edge.Profile.SurfaceZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.surface]");
                    foreach (var zone in edge.Profile.SurfaceZones)
                    {
                        sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {zone.Value.ToString().ToLowerInvariant()}");
                    }
                }

                if (edge.Profile.NoiseZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.noise]");
                    foreach (var zone in edge.Profile.NoiseZones)
                    {
                        sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {zone.Value.ToString().ToLowerInvariant()}");
                    }
                }

                if (edge.Profile.SpeedLimitZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.speed_limits]");
                    foreach (var zone in edge.Profile.SpeedLimitZones)
                    {
                        sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {FormatFloat(zone.MaxSpeedKph)}");
                    }
                }

                if (edge.Profile.Markers.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.markers]");
                    foreach (var marker in edge.Profile.Markers)
                    {
                        sb.AppendLine($"{marker.Name} {FormatFloat(marker.PositionMeters)}");
                    }
                }

                if (edge.Profile.WeatherZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.weather]");
                    foreach (var zone in edge.Profile.WeatherZones)
                    {
                        sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {zone.Weather.ToString().ToLowerInvariant()} {FormatFloat(zone.FadeInMeters)} {FormatFloat(zone.FadeOutMeters)}");
                    }
                }

                if (edge.Profile.AmbienceZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.ambience]");
                    foreach (var zone in edge.Profile.AmbienceZones)
                    {
                        sb.AppendLine($"{FormatFloat(zone.StartMeters)} {FormatFloat(zone.EndMeters)} {zone.Ambience.ToString().ToLowerInvariant()} {FormatFloat(zone.FadeInMeters)} {FormatFloat(zone.FadeOutMeters)}");
                    }
                }

                if (edge.Profile.Hazards.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.hazards]");
                    foreach (var hazard in edge.Profile.Hazards)
                    {
                        var name = string.IsNullOrWhiteSpace(hazard.Name) ? string.Empty : $" name={hazard.Name}";
                        sb.AppendLine($"{FormatFloat(hazard.StartMeters)} {FormatFloat(hazard.EndMeters)} {hazard.HazardType} {FormatFloat(hazard.Severity)}{name}");
                    }
                }

                if (edge.Profile.Checkpoints.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.checkpoints]");
                    foreach (var checkpoint in edge.Profile.Checkpoints)
                    {
                        var name = string.IsNullOrWhiteSpace(checkpoint.Name) ? string.Empty : $" name={checkpoint.Name}";
                        sb.AppendLine($"{FormatFloat(checkpoint.PositionMeters)} {checkpoint.Id}{name}");
                    }
                }

                if (edge.Profile.HitLanes.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.hit_lanes]");
                    foreach (var lane in edge.Profile.HitLanes)
                    {
                        var lanes = string.Join(",", lane.LaneIndices);
                        var effect = string.IsNullOrWhiteSpace(lane.Effect) ? string.Empty : $" effect={lane.Effect}";
                        sb.AppendLine($"{FormatFloat(lane.StartMeters)} {FormatFloat(lane.EndMeters)} lanes={lanes}{effect}");
                    }
                }

                if (edge.Profile.Emitters.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.emitters]");
                    foreach (var emitter in edge.Profile.Emitters)
                    {
                        var sound = string.IsNullOrWhiteSpace(emitter.SoundKey) ? string.Empty : $" sound={emitter.SoundKey}";
                        sb.AppendLine($"id={emitter.Id} pos={FormatFloat(emitter.PositionMeters)} radius={FormatFloat(emitter.RadiusMeters)} loop={FormatBool(emitter.Loop)} volume={FormatFloat(emitter.Volume)}{sound}");
                    }
                }

                if (edge.Profile.Triggers.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.triggers]");
                    foreach (var trigger in edge.Profile.Triggers)
                    {
                        var action = string.IsNullOrWhiteSpace(trigger.Action) ? string.Empty : $" action={trigger.Action}";
                        var payload = string.IsNullOrWhiteSpace(trigger.Payload) ? string.Empty : $" payload={trigger.Payload}";
                        sb.AppendLine($"id={trigger.Id} {FormatFloat(trigger.StartMeters)} {FormatFloat(trigger.EndMeters)}{action}{payload}");
                    }
                }

                if (edge.Profile.BoundaryZones.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[edge {edge.Id}.boundaries]");
                    foreach (var boundary in edge.Profile.BoundaryZones)
                    {
                        sb.AppendLine(FormatBoundary(boundary));
                    }
                }
            }

            return sb.ToString();
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

        private static void WriteValue(StringBuilder sb, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            sb.AppendLine($"{key}={EncodeValue(value!)}");
        }

        private static void AppendInlineValue(StringBuilder sb, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            sb.Append(' ').Append(key).Append('=').Append(EncodeValue(value!));
        }

        private static string EncodeValue(string value)
        {
            var encoded = value;
            if (NeedsQuoting(encoded))
                encoded = $"\"{encoded.Replace("\"", "\\\"")}\"";
            return encoded;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", Culture);
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatSpan(TrackGeometrySpan span)
        {
            var sb = new StringBuilder();
            sb.Append($"kind={span.Kind.ToString().ToLowerInvariant()}");
            sb.Append($" length={FormatFloat(span.LengthMeters)}");

            if (span.Kind == TrackGeometrySpanKind.Arc)
                sb.Append($" radius={FormatFloat(span.RadiusMeters)}");
            else if (span.Kind == TrackGeometrySpanKind.Clothoid)
                sb.Append($" start={FormatFloat(span.StartRadiusMeters)} end={FormatFloat(span.EndRadiusMeters)}");

            if (span.Direction != TrackCurveDirection.Straight)
                sb.Append($" direction={span.Direction.ToString().ToLowerInvariant()}");
            if (span.CurveSeverity.HasValue)
                sb.Append($" severity={span.CurveSeverity.Value.ToString().ToLowerInvariant()}");

            var slopeDelta = Math.Abs(span.StartSlope - span.EndSlope);
            if (slopeDelta > 0.0001f)
            {
                sb.Append($" slope_start={FormatFloat(span.StartSlope)}");
                sb.Append($" slope_end={FormatFloat(span.EndSlope)}");
            }
            else if (Math.Abs(span.StartSlope) > 0.0001f)
            {
                sb.Append($" slope={FormatFloat(span.StartSlope)}");
            }

            var bankDelta = Math.Abs(span.BankStartDegrees - span.BankEndDegrees);
            if (bankDelta > 0.0001f)
            {
                sb.Append($" bank_start={FormatFloat(span.BankStartDegrees)}");
                sb.Append($" bank_end={FormatFloat(span.BankEndDegrees)}");
            }
            else if (Math.Abs(span.BankStartDegrees) > 0.0001f)
            {
                sb.Append($" bank={FormatFloat(span.BankStartDegrees)}");
            }

            return sb.ToString();
        }

        private static string FormatBoundary(TrackBoundaryZone boundary)
        {
            var sb = new StringBuilder();
            sb.Append("start=").Append(FormatFloat(boundary.StartMeters));
            sb.Append(" end=").Append(FormatFloat(boundary.EndMeters));
            sb.Append(" side=").Append(FormatBoundarySide(boundary.Side));
            sb.Append(" type=").Append(FormatBoundaryType(boundary.BoundaryType));
            sb.Append(" offset=").Append(FormatFloat(boundary.OffsetMeters));
            if (boundary.WidthMeters > 0f)
                sb.Append(" width=").Append(FormatFloat(boundary.WidthMeters));
            if (boundary.HeightMeters > 0f)
                sb.Append(" height=").Append(FormatFloat(boundary.HeightMeters));
            if (!boundary.IsSolid)
                sb.Append(" solid=false");
            if (Math.Abs(boundary.Severity - 1f) > 0.0001f)
                sb.Append(" severity=").Append(FormatFloat(boundary.Severity));
            foreach (var kvp in boundary.Metadata)
                sb.Append(' ').Append(kvp.Key).Append('=').Append(EncodeValue(kvp.Value));
            return sb.ToString();
        }

        private static string FormatIntersectionLeg(TrackIntersectionLeg leg)
        {
            var sb = new StringBuilder();
            sb.Append("id=").Append(leg.Id);
            sb.Append(" edge=").Append(leg.EdgeId);
            sb.Append(" type=").Append(FormatLegType(leg.LegType));
            if (leg.LaneCount > 0)
                sb.Append(" lanes=").Append(leg.LaneCount.ToString(Culture));
            if (Math.Abs(leg.HeadingDegrees) > 0.0001f)
                sb.Append(" heading=").Append(FormatFloat(leg.HeadingDegrees));
            if (Math.Abs(leg.OffsetXMeters) > 0.0001f || Math.Abs(leg.OffsetZMeters) > 0.0001f)
                sb.Append(" offset=").Append(FormatPoint(leg.OffsetXMeters, leg.OffsetZMeters));
            if (leg.WidthMeters > 0f)
                sb.Append(" width=").Append(FormatFloat(leg.WidthMeters));
            if (leg.ApproachLengthMeters > 0f)
                sb.Append(" approach_length=").Append(FormatFloat(leg.ApproachLengthMeters));
            if (leg.SpeedLimitKph > 0f)
                sb.Append(" speed_limit=").Append(FormatFloat(leg.SpeedLimitKph));
            if (leg.Priority != 0)
                sb.Append(" priority=").Append(leg.Priority.ToString(Culture));
            foreach (var kvp in leg.Metadata)
                sb.Append(' ').Append(kvp.Key).Append('=').Append(EncodeValue(kvp.Value));
            return sb.ToString();
        }

        private static string FormatIntersectionConnector(TrackIntersectionConnector connector)
        {
            var sb = new StringBuilder();
            sb.Append("id=").Append(connector.Id);
            sb.Append(" from=").Append(connector.FromLegId);
            sb.Append(" to=").Append(connector.ToLegId);
            if (connector.TurnDirection != TrackTurnDirection.Unknown)
                sb.Append(" turn=").Append(FormatTurnDirection(connector.TurnDirection));
            if (connector.RadiusMeters > 0f)
                sb.Append(" radius=").Append(FormatFloat(connector.RadiusMeters));
            if (connector.LengthMeters > 0f)
                sb.Append(" length=").Append(FormatFloat(connector.LengthMeters));
            if (connector.SpeedLimitKph > 0f)
                sb.Append(" speed_limit=").Append(FormatFloat(connector.SpeedLimitKph));
            if (connector.LaneCount > 0)
                sb.Append(" lanes=").Append(connector.LaneCount.ToString(Culture));
            if (connector.Priority != 0)
                sb.Append(" priority=").Append(connector.Priority.ToString(Culture));
            if (connector.PathPoints.Count > 0)
                sb.Append(" points=").Append(FormatPointList(connector.PathPoints));
            foreach (var kvp in connector.Metadata)
                sb.Append(' ').Append(kvp.Key).Append('=').Append(EncodeValue(kvp.Value));
            return sb.ToString();
        }

        private static string FormatIntersectionLane(TrackLane lane)
        {
            var sb = new StringBuilder();
            sb.Append("id=").Append(lane.Id);
            sb.Append(" owner_kind=").Append(FormatLaneOwnerKind(lane.OwnerKind));
            sb.Append(" owner=").Append(lane.OwnerId);
            sb.Append(" index=").Append(lane.Index.ToString(Culture));
            sb.Append(" width=").Append(FormatFloat(lane.WidthMeters));
            if (Math.Abs(lane.CenterOffsetMeters) > 0.0001f)
                sb.Append(" center_offset=").Append(FormatFloat(lane.CenterOffsetMeters));
            if (lane.ShoulderLeftMeters > 0f)
                sb.Append(" shoulder_left=").Append(FormatFloat(lane.ShoulderLeftMeters));
            if (lane.ShoulderRightMeters > 0f)
                sb.Append(" shoulder_right=").Append(FormatFloat(lane.ShoulderRightMeters));
            sb.Append(" type=").Append(FormatLaneType(lane.LaneType));
            sb.Append(" direction=").Append(FormatLaneDirection(lane.Direction));
            sb.Append(" marking_left=").Append(FormatLaneMarking(lane.MarkingLeft));
            sb.Append(" marking_right=").Append(FormatLaneMarking(lane.MarkingRight));
            if (Math.Abs(lane.EntryHeadingDegrees) > 0.0001f)
                sb.Append(" heading_in=").Append(FormatFloat(lane.EntryHeadingDegrees));
            if (Math.Abs(lane.ExitHeadingDegrees) > 0.0001f)
                sb.Append(" heading_out=").Append(FormatFloat(lane.ExitHeadingDegrees));
            if (lane.CenterlinePoints.Count > 0)
                sb.Append(" centerline=").Append(FormatPointList(lane.CenterlinePoints));
            if (lane.LeftEdgePoints.Count > 0)
                sb.Append(" left_edge=").Append(FormatPointList(lane.LeftEdgePoints));
            if (lane.RightEdgePoints.Count > 0)
                sb.Append(" right_edge=").Append(FormatPointList(lane.RightEdgePoints));
            if (lane.SpeedLimitKph > 0f)
                sb.Append(" speed_limit=").Append(FormatFloat(lane.SpeedLimitKph));
            sb.Append(" surface=").Append(lane.Surface.ToString().ToLowerInvariant());
            if (lane.Priority != 0)
                sb.Append(" priority=").Append(lane.Priority.ToString(Culture));
            if (lane.AllowedVehicles.Count > 0)
                sb.Append(" allowed_vehicles=").Append(string.Join(", ", lane.AllowedVehicles));
            foreach (var kvp in lane.Metadata)
                sb.Append(' ').Append(kvp.Key).Append('=').Append(EncodeValue(kvp.Value));
            return sb.ToString();
        }

        private static string FormatIntersectionLaneLink(TrackLaneLink link)
        {
            var sb = new StringBuilder();
            sb.Append("id=").Append(link.Id);
            sb.Append(" from=").Append(link.FromLaneId);
            sb.Append(" to=").Append(link.ToLaneId);
            if (link.TurnDirection != TrackTurnDirection.Unknown)
                sb.Append(" turn=").Append(FormatTurnDirection(link.TurnDirection));
            if (link.AllowsLaneChange)
                sb.Append(" lane_change=true");
            if (link.ChangeLengthMeters > 0f)
                sb.Append(" change_length=").Append(FormatFloat(link.ChangeLengthMeters));
            if (link.Priority != 0)
                sb.Append(" priority=").Append(link.Priority.ToString(Culture));
            foreach (var kvp in link.Metadata)
                sb.Append(' ').Append(kvp.Key).Append('=').Append(EncodeValue(kvp.Value));
            return sb.ToString();
        }

        private static string FormatIntersectionArea(TrackIntersectionArea area)
        {
            var sb = new StringBuilder();
            sb.Append("id=").Append(area.Id);
            sb.Append(" shape=").Append(FormatIntersectionAreaShape(area.Shape));
            sb.Append(" kind=").Append(FormatIntersectionAreaKind(area.Kind));
            if (area.RadiusMeters > 0f)
                sb.Append(" radius=").Append(FormatFloat(area.RadiusMeters));
            if (area.WidthMeters > 0f)
                sb.Append(" width=").Append(FormatFloat(area.WidthMeters));
            if (area.LengthMeters > 0f)
                sb.Append(" length=").Append(FormatFloat(area.LengthMeters));
            if (Math.Abs(area.OffsetXMeters) > 0.0001f)
                sb.Append(" offset_x=").Append(FormatFloat(area.OffsetXMeters));
            if (Math.Abs(area.OffsetZMeters) > 0.0001f)
                sb.Append(" offset_z=").Append(FormatFloat(area.OffsetZMeters));
            if (Math.Abs(area.HeadingDegrees) > 0.0001f)
                sb.Append(" heading=").Append(FormatFloat(area.HeadingDegrees));
            if (Math.Abs(area.ElevationMeters) > 0.0001f)
                sb.Append(" elevation=").Append(FormatFloat(area.ElevationMeters));
            if (area.Surface != TrackSurface.Asphalt)
                sb.Append(" surface=").Append(area.Surface.ToString().ToLowerInvariant());
            if (area.Points.Count > 0)
                sb.Append(" points=").Append(FormatPointList(area.Points));
            foreach (var kvp in area.Metadata)
                sb.Append(' ').Append(kvp.Key).Append('=').Append(EncodeValue(kvp.Value));
            return sb.ToString();
        }

        private static bool NeedsQuoting(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsWhiteSpace(c) || c == '#' || c == ';')
                    return true;
            }
            return value.Contains('"');
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
            var bankParsed = GetFloat(named, "bank", "bankdeg", positional, -1, out var bankValue);
            var bank = bankParsed ? bankValue : 0f;

            var bankStartParsed = GetFloat(named, "bank_start", "bankstart", positional, -1, out var bankStartValue);
            var bankStart = bankStartParsed ? bankStartValue : (bankParsed ? bank : 0f);
            var bankEndParsed = GetFloat(named, "bank_end", "bankend", positional, -1, out var bankEndValue);
            var bankEnd = bankEndParsed ? bankEndValue : (bankStartParsed ? bankStart : (bankParsed ? bank : 0f));

            var slopeFound = TryParseSlope(named, "slope", "grade", out var slopeCommon);
            var slopeStartFound = TryParseSlope(named, "slope_start", "grade_start", out var slopeStart);
            var slopeEndFound = TryParseSlope(named, "slope_end", "grade_end", out var slopeEnd);

            var useSlopes = slopeFound || slopeStartFound || slopeEndFound;
            float startSlope;
            float endSlope;
            float elevationDelta;
            if (useSlopes)
            {
                startSlope = slopeStartFound ? slopeStart : (slopeFound ? slopeCommon : 0f);
                endSlope = slopeEndFound ? slopeEnd : (slopeFound ? slopeCommon : startSlope);
                elevationDelta = length * (startSlope + endSlope) * 0.5f;
            }
            else
            {
                elevationDelta = elevation;
                startSlope = length > 0f ? elevationDelta / length : 0f;
                endSlope = startSlope;
            }

            try
            {
                switch (spanKind.Value)
                {
                    case TrackGeometrySpanKind.Straight:
                        spans.Add(TrackGeometrySpan.StraightWithProfile(
                            length,
                            elevationDelta,
                            startSlope,
                            endSlope,
                            bankStart,
                            bankEnd));
                        break;
                    case TrackGeometrySpanKind.Arc:
                        var radius = GetFloat(named, "radius", "r", positional, 1, errors, lineNumber, line);
                        spans.Add(TrackGeometrySpan.ArcWithProfile(
                            length,
                            radius,
                            direction,
                            severity,
                            elevationDelta,
                            startSlope,
                            endSlope,
                            bankStart,
                            bankEnd));
                        break;
                    case TrackGeometrySpanKind.Clothoid:
                        var startRadius = GetFloat(named, "start", "startRadius", positional, 1, errors, lineNumber, line);
                        var endRadius = GetFloat(named, "end", "endRadius", positional, 2, errors, lineNumber, line);
                        spans.Add(TrackGeometrySpan.ClothoidWithProfile(
                            length,
                            startRadius,
                            endRadius,
                            direction,
                            severity,
                            elevationDelta,
                            startSlope,
                            endSlope,
                            bankStart,
                            bankEnd));
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

        private static bool TryParseWeatherZone(string line, int lineNumber, List<TrackWeatherZone> zones, List<TrackLayoutError> errors)
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
            var weatherToken = GetString(named, "weather", "w", positional, 2, errors, lineNumber, line);
            var weatherValue = weatherToken?.Trim();
            if (string.IsNullOrWhiteSpace(weatherValue))
                return false;

            var weather = ParseEnum<TrackWeather>(weatherValue!, lineNumber, errors, line);
            if (weather == null)
                return false;

            var fadeIn = GetFloat(named, "fade_in", "fi", positional, 3, out var fadeInValue) ? fadeInValue : 0f;
            var fadeOut = GetFloat(named, "fade_out", "fo", positional, 4, out var fadeOutValue) ? fadeOutValue : 0f;

            try
            {
                zones.Add(new TrackWeatherZone(start, end, weather.Value, fadeIn, fadeOut));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseAmbienceZone(string line, int lineNumber, List<TrackAmbienceZone> zones, List<TrackLayoutError> errors)
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
            var ambienceToken = GetString(named, "ambience", "a", positional, 2, errors, lineNumber, line);
            var ambienceValue = ambienceToken?.Trim();
            if (string.IsNullOrWhiteSpace(ambienceValue))
                return false;

            var ambience = ParseEnum<TrackAmbience>(ambienceValue!, lineNumber, errors, line);
            if (ambience == null)
                return false;

            var fadeIn = GetFloat(named, "fade_in", "fi", positional, 3, out var fadeInValue) ? fadeInValue : 0f;
            var fadeOut = GetFloat(named, "fade_out", "fo", positional, 4, out var fadeOutValue) ? fadeOutValue : 0f;

            try
            {
                zones.Add(new TrackAmbienceZone(start, end, ambience.Value, fadeIn, fadeOut));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseHazard(string line, int lineNumber, List<TrackHazardZone> hazards, List<TrackLayoutError> errors)
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
            var type = GetString(named, "type", "hazard", positional, 2, errors, lineNumber, line);
            var typeValue = type?.Trim();
            if (string.IsNullOrWhiteSpace(typeValue))
                return false;

            var severity = GetFloat(named, "severity", "sev", positional, 3, out var sevValue) ? sevValue : 1f;
            named.TryGetValue("name", out var name);
            var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            var metadata = CollectMetadata(named, "start", "s", "end", "e", "type", "hazard", "severity", "sev", "name");

            try
            {
                hazards.Add(new TrackHazardZone(start, end, typeValue!, severity, trimmedName, metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseCheckpoint(string line, int lineNumber, List<TrackCheckpoint> checkpoints, List<TrackLayoutError> errors)
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

            var position = GetFloat(named, "pos", "position", positional, 0, errors, lineNumber, line);
            var id = GetString(named, "id", "checkpoint", positional, 1, errors, lineNumber, line);
            named.TryGetValue("name", out var name);
            if (string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                id = name;
            if (string.IsNullOrWhiteSpace(id))
                return false;

            try
            {
                var trimmedId = id!.Trim();
                var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
                checkpoints.Add(new TrackCheckpoint(trimmedId, position, trimmedName));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseHitLanes(string line, int lineNumber, List<TrackHitLaneZone> zones, List<TrackLayoutError> errors)
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
            var lanesToken = GetString(named, "lanes", "lane", positional, 2, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(lanesToken))
                return false;

            named.TryGetValue("effect", out var effect);
            var lanes = ParseIntList(lanesToken, lineNumber, errors, line);
            if (lanes.Count == 0)
                return false;

            try
            {
                zones.Add(new TrackHitLaneZone(start, end, lanes, effect));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseAllowedVehicles(string line, int lineNumber, List<string> vehicles, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count == 0)
                return false;

            foreach (var token in tokens)
            {
                if (TrySplitKeyValue(token, out _, out var value))
                {
                    foreach (var entry in SplitList(value))
                        AddIfNotEmpty(vehicles, entry);
                }
                else
                {
                    AddIfNotEmpty(vehicles, token);
                }
            }

            return true;
        }

        private static bool TryParseEmitter(string line, int lineNumber, List<TrackAudioEmitter> emitters, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "name", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var position = GetFloat(named, "pos", "position", positional, 1, errors, lineNumber, line);
            var radius = GetFloat(named, "radius", "r", positional, 2, errors, lineNumber, line);
            named.TryGetValue("sound", out var sound);
            var volume = GetFloat(named, "volume", "vol", positional, 3, out var volumeValue) ? volumeValue : 1f;
            var loop = ParseBoolValue(named.TryGetValue("loop", out var loopValue) ? loopValue : null, true, out var loopParsed);
            if (loopParsed == false && named.ContainsKey("loop"))
            {
                errors.Add(new TrackLayoutError(lineNumber, "Invalid loop value.", line));
                return false;
            }

            var metadata = CollectMetadata(named, "id", "name", "pos", "position", "radius", "r", "sound", "volume", "vol", "loop");
            try
            {
                emitters.Add(new TrackAudioEmitter(id!.Trim(), position, radius, sound, loop, volume, metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }
            return true;
        }

        private static bool TryParseTrigger(string line, int lineNumber, List<TrackTriggerZone> triggers, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "name", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var start = GetFloat(named, "start", "s", positional, 1, errors, lineNumber, line);
            var end = GetFloat(named, "end", "e", positional, 2, errors, lineNumber, line);
            named.TryGetValue("action", out var action);
            named.TryGetValue("payload", out var payload);
            var metadata = CollectMetadata(named, "id", "name", "start", "s", "end", "e", "action", "payload");

            try
            {
                triggers.Add(new TrackTriggerZone(id!.Trim(), start, end, action, payload, metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }
            return true;
        }

        private static bool TryParseBoundary(string line, int lineNumber, List<TrackBoundaryZone> boundaries, List<TrackLayoutError> errors)
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

            var sideValue = named.TryGetValue("side", out var sideText) ? sideText : (positional.Count > 2 ? positional[2] : "both");
            if (!TryParseBoundarySide(sideValue, out var side))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid boundary side '{sideValue}'.", line));
                return false;
            }

            var typeValue = named.TryGetValue("type", out var typeText) ? typeText : (positional.Count > 3 ? positional[3] : "unknown");
            if (!TryParseBoundaryType(typeValue, out var boundaryType))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid boundary type '{typeValue}'.", line));
                return false;
            }

            var offset = GetFloat(named, "offset", "d", positional, 4, out var offsetValue) ? offsetValue : 0f;
            var width = GetFloat(named, "width", "w", positional, 5, out var widthValue) ? widthValue : 0f;
            var height = GetFloat(named, "height", "h", positional, 6, out var heightValue) ? heightValue : 0f;

            var solid = ParseBoolValue(named.TryGetValue("solid", out var solidValue) ? solidValue : null, true, out var solidParsed);
            if (!solidParsed && named.ContainsKey("solid"))
            {
                errors.Add(new TrackLayoutError(lineNumber, "Invalid solid value.", line));
                return false;
            }

            var severity = GetFloat(named, "severity", "impact", positional, 7, out var severityValue) ? severityValue : 1f;

            var metadata = CollectMetadata(named,
                "start", "s", "end", "e",
                "side", "type",
                "offset", "d",
                "width", "w",
                "height", "h",
                "solid", "severity", "impact");

            try
            {
                boundaries.Add(new TrackBoundaryZone(
                    start,
                    end,
                    side,
                    boundaryType,
                    offset,
                    width,
                    height,
                    solid,
                    severity,
                    metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static EdgeBuilder GetOrCreateEdge(string edgeId, Dictionary<string, EdgeBuilder> edges, List<string> edgeOrder)
        {
            if (!edges.TryGetValue(edgeId, out var edge))
            {
                edge = new EdgeBuilder(edgeId);
                edges.Add(edgeId, edge);
                edgeOrder.Add(edgeId);
            }
            return edge;
        }

        private static NodeBuilder GetOrCreateNode(string nodeId, Dictionary<string, NodeBuilder> nodes)
        {
            if (!nodes.TryGetValue(nodeId, out var node))
            {
                node = new NodeBuilder(nodeId);
                nodes.Add(nodeId, node);
            }
            return node;
        }

        private static void EnsureNode(string nodeId, Dictionary<string, NodeBuilder> nodes)
        {
            if (!nodes.ContainsKey(nodeId))
                nodes.Add(nodeId, new NodeBuilder(nodeId));
        }

        private static bool TryParseNodeLine(string line, int lineNumber, Dictionary<string, NodeBuilder> nodes, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "node", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var nodeId = id!.Trim();
            named.TryGetValue("name", out var name);
            named.TryGetValue("short_name", out var shortName);
            if (string.IsNullOrWhiteSpace(shortName) && named.TryGetValue("short", out var shortAlt))
                shortName = shortAlt;
            if (string.IsNullOrWhiteSpace(name) && positional.Count > 1)
                name = positional[1];

            if (!nodes.TryGetValue(nodeId, out var node))
            {
                node = new NodeBuilder(nodeId);
                nodes.Add(nodeId, node);
            }

            if (!string.IsNullOrWhiteSpace(name))
                node.Name = name;
            if (!string.IsNullOrWhiteSpace(shortName))
                node.ShortName = shortName;

            foreach (var kvp in named)
            {
                if (kvp.Key.Equals("id", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("node", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("name", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("short_name", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("short", StringComparison.OrdinalIgnoreCase))
                    continue;
                node.Metadata[kvp.Key] = kvp.Value;
            }

            return true;
        }

        private static void ParseNodeProperties(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count == 0)
                return;

            foreach (var token in tokens)
            {
                if (!TrySplitKeyValue(token, out var key, out var value))
                {
                    errors.Add(new TrackLayoutError(lineNumber, "Expected key=value in node section.", line));
                    continue;
                }

                switch (key.ToLowerInvariant())
                {
                    case "name":
                        node.Name = value;
                        break;
                    case "short_name":
                    case "short":
                        node.ShortName = value;
                        break;
                    default:
                        node.Metadata[key] = value;
                        break;
                }
            }
        }

        private static void ParseIntersectionProperties(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count == 0)
                return;

            var intersection = node.Intersection ??= new IntersectionBuilder();

            foreach (var token in tokens)
            {
                if (!TrySplitKeyValue(token, out var key, out var value))
                {
                    errors.Add(new TrackLayoutError(lineNumber, "Expected key=value in intersection section.", line));
                    continue;
                }

                switch (key.ToLowerInvariant())
                {
                    case "shape":
                        if (TryParseIntersectionShape(value, out var shape))
                            intersection.SetShape(shape);
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid intersection shape '{value}'.", line));
                        break;
                    case "radius":
                        intersection.SetRadius(ParseFloat(value, lineNumber, errors, line));
                        break;
                    case "inner_radius":
                    case "inner":
                        intersection.SetInnerRadius(ParseFloat(value, lineNumber, errors, line));
                        break;
                    case "outer_radius":
                    case "outer":
                        intersection.SetOuterRadius(ParseFloat(value, lineNumber, errors, line));
                        break;
                    case "entry_lanes":
                    case "entry":
                        if (int.TryParse(value, NumberStyles.Integer, Culture, out var entryLanes))
                            intersection.SetEntryLanes(entryLanes);
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid entry_lanes '{value}'.", line));
                        break;
                    case "exit_lanes":
                    case "exit":
                        if (int.TryParse(value, NumberStyles.Integer, Culture, out var exitLanes))
                            intersection.SetExitLanes(exitLanes);
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid exit_lanes '{value}'.", line));
                        break;
                    case "turn_lanes":
                    case "turn":
                        if (int.TryParse(value, NumberStyles.Integer, Culture, out var turnLanes))
                            intersection.SetTurnLanes(turnLanes);
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid turn_lanes '{value}'.", line));
                        break;
                    case "speed_limit":
                    case "speed":
                        intersection.SetSpeedLimit(ParseFloat(value, lineNumber, errors, line));
                        break;
                    case "control":
                        if (TryParseIntersectionControl(value, out var control))
                            intersection.SetControl(control);
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid intersection control '{value}'.", line));
                        break;
                    case "priority":
                        if (int.TryParse(value, NumberStyles.Integer, Culture, out var priority))
                            intersection.SetPriority(priority);
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid priority '{value}'.", line));
                        break;
                    default:
                        intersection.Metadata[key] = value;
                        break;
                }
            }
        }

        private static bool TryParseIntersectionLeg(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "leg", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var edgeId = GetString(named, "edge", "edge_id", positional, 1, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(edgeId))
                return false;

            var typeValue = named.TryGetValue("type", out var typeText) ? typeText : (positional.Count > 2 ? positional[2] : "both");
            if (!TryParseLegType(typeValue, out var legType))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid leg type '{typeValue}'.", line));
                return false;
            }

            var laneCount = GetFloat(named, "lanes", "lane_count", positional, 3, out var lanesValue)
                ? (int)Math.Round(lanesValue)
                : 0;
            var heading = GetFloat(named, "heading", "heading_deg", positional, 4, out var headingValue)
                ? headingValue
                : 0f;
            float offsetX = 0f;
            float offsetZ = 0f;
            if (named.TryGetValue("offset", out var offsetValue) || named.TryGetValue("center", out offsetValue))
            {
                if (!TryParsePoint(offsetValue, lineNumber, errors, line, out offsetX, out _, out offsetZ))
                    return false;
            }
            else
            {
                if (GetFloat(named, "offset_x", "x", positional, -1, out var offsetXValue))
                    offsetX = offsetXValue;
                if (GetFloat(named, "offset_z", "z", positional, -1, out var offsetZValue))
                    offsetZ = offsetZValue;
            }
            var width = GetFloat(named, "width", "w", positional, -1, out var widthValue)
                ? widthValue
                : 0f;
            var approachLength = GetFloat(named, "approach_length", "approach", positional, -1, out var approachValue)
                ? approachValue
                : 0f;
            var speedLimit = GetFloat(named, "speed_limit", "speed", positional, 5, out var speedValue)
                ? speedValue
                : 0f;
            var priority = GetFloat(named, "priority", "prio", positional, 6, out var priorityValue)
                ? (int)Math.Round(priorityValue)
                : 0;

            var metadata = CollectMetadata(named,
                "id", "leg",
                "edge", "edge_id",
                "type",
                "lanes", "lane_count",
                "heading", "heading_deg",
                "offset", "center",
                "offset_x", "x",
                "offset_z", "z",
                "width", "w",
                "approach_length", "approach",
                "speed_limit", "speed",
                "priority", "prio");

            try
            {
                var intersection = node.Intersection ??= new IntersectionBuilder();
                intersection.Legs.Add(new TrackIntersectionLeg(
                    id!.Trim(),
                    edgeId!.Trim(),
                    legType,
                    laneCount,
                    heading,
                    offsetX,
                    offsetZ,
                    width,
                    approachLength,
                    speedLimit,
                    priority,
                    metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseIntersectionConnector(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "connector", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var fromLeg = GetString(named, "from", "src", positional, 1, errors, lineNumber, line);
            var toLeg = GetString(named, "to", "dst", positional, 2, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(fromLeg) || string.IsNullOrWhiteSpace(toLeg))
                return false;

            var turnValue = named.TryGetValue("turn", out var turnText) ? turnText : (positional.Count > 3 ? positional[3] : string.Empty);
            var turnDirection = TrackTurnDirection.Unknown;
            if (!string.IsNullOrWhiteSpace(turnValue))
            {
                if (!TryParseTurnDirection(turnValue, out turnDirection))
                {
                    errors.Add(new TrackLayoutError(lineNumber, $"Invalid turn direction '{turnValue}'.", line));
                    return false;
                }
            }

            var radius = GetFloat(named, "radius", "r", positional, 4, out var radiusValue) ? radiusValue : 0f;
            var length = GetFloat(named, "length", "len", positional, 5, out var lengthValue) ? lengthValue : 0f;
            var speedLimit = GetFloat(named, "speed_limit", "speed", positional, 6, out var speedValue) ? speedValue : 0f;
            var laneCount = GetFloat(named, "lanes", "lane_count", positional, 7, out var lanesValue)
                ? (int)Math.Round(lanesValue)
                : 0;
            var priority = GetFloat(named, "priority", "prio", positional, 8, out var priorityValue)
                ? (int)Math.Round(priorityValue)
                : 0;
            IReadOnlyList<TrackPoint3> pathPoints = Array.Empty<TrackPoint3>();
            if (named.TryGetValue("points", out var pointsValue) ||
                named.TryGetValue("path", out pointsValue) ||
                named.TryGetValue("polyline", out pointsValue))
            {
                pathPoints = ParsePointList(pointsValue, lineNumber, errors, line);
                if (pathPoints.Count == 0 && !string.IsNullOrWhiteSpace(pointsValue))
                    return false;
            }

            var metadata = CollectMetadata(named,
                "id", "connector",
                "from", "src",
                "to", "dst",
                "turn",
                "radius", "r",
                "length", "len",
                "speed_limit", "speed",
                "lanes", "lane_count",
                "priority", "prio",
                "points", "path", "polyline");

            try
            {
                var intersection = node.Intersection ??= new IntersectionBuilder();
                intersection.Connectors.Add(new TrackIntersectionConnector(
                    id!.Trim(),
                    fromLeg!.Trim(),
                    toLeg!.Trim(),
                    turnDirection,
                    radius,
                    length,
                    speedLimit,
                    laneCount,
                    priority,
                    pathPoints,
                    metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseIntersectionLane(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "lane", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            TrackLaneOwnerKind ownerKind;
            string? ownerId = null;
            var hasLeg = named.TryGetValue("leg", out var legId);
            var hasConnector = named.TryGetValue("connector", out var connectorId);
            if (hasLeg && hasConnector)
            {
                errors.Add(new TrackLayoutError(lineNumber, "Lane cannot declare both leg and connector owners.", line));
                return false;
            }
            if (hasLeg)
            {
                ownerKind = TrackLaneOwnerKind.Leg;
                ownerId = legId;
            }
            else if (hasConnector)
            {
                ownerKind = TrackLaneOwnerKind.Connector;
                ownerId = connectorId;
            }
            else
            {
                var ownerKindValue = named.TryGetValue("owner_kind", out var kindText)
                    ? kindText
                    : (named.TryGetValue("kind", out kindText) ? kindText : (positional.Count > 1 ? positional[1] : string.Empty));
                if (string.IsNullOrWhiteSpace(ownerKindValue) || !TryParseLaneOwnerKind(ownerKindValue, out ownerKind))
                {
                    errors.Add(new TrackLayoutError(lineNumber, "Lane owner_kind is required (leg or connector).", line));
                    return false;
                }

                ownerId = GetString(named, "owner", "owner_id", positional, 2, errors, lineNumber, line);
                if (string.IsNullOrWhiteSpace(ownerId))
                    return false;
            }

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                errors.Add(new TrackLayoutError(lineNumber, "Lane owner id is required.", line));
                return false;
            }

            var index = GetFloat(named, "index", "idx", positional, 3, out var indexValue)
                ? (int)Math.Round(indexValue)
                : 0;
            var width = GetFloat(named, "width", "w", positional, 4, errors, lineNumber, line);
            if (width <= 0f)
                return false;
            var centerOffset = GetFloat(named, "center_offset", "offset", positional, 5, out var offsetValue)
                ? offsetValue
                : 0f;
            var shoulderLeft = GetFloat(named, "shoulder_left", "sl", positional, 6, out var shoulderLeftValue)
                ? shoulderLeftValue
                : 0f;
            var shoulderRight = GetFloat(named, "shoulder_right", "sr", positional, 7, out var shoulderRightValue)
                ? shoulderRightValue
                : 0f;

            var laneTypeValue = named.TryGetValue("type", out var typeText) || named.TryGetValue("lane_type", out typeText)
                ? typeText
                : (positional.Count > 8 ? positional[8] : "travel");
            if (!TryParseLaneType(laneTypeValue, out var laneType))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid lane type '{laneTypeValue}'.", line));
                return false;
            }

            var directionValue = named.TryGetValue("direction", out var directionText) || named.TryGetValue("dir", out directionText)
                ? directionText
                : (positional.Count > 9 ? positional[9] : "forward");
            if (!TryParseLaneDirection(directionValue, out var direction))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid lane direction '{directionValue}'.", line));
                return false;
            }

            var markingLeft = TrackLaneMarking.None;
            if (named.TryGetValue("marking_left", out var markLeft) ||
                named.TryGetValue("mark_left", out markLeft) ||
                named.TryGetValue("ml", out markLeft) ||
                (positional.Count > 10 && (markLeft = positional[10]) != null))
            {
                if (!string.IsNullOrWhiteSpace(markLeft))
                {
                    if (!TryParseLaneMarking(markLeft!, out markingLeft))
                    {
                        errors.Add(new TrackLayoutError(lineNumber, $"Invalid left lane marking '{markLeft}'.", line));
                        return false;
                    }
                }
            }

            var markingRight = TrackLaneMarking.None;
            if (named.TryGetValue("marking_right", out var markRight) ||
                named.TryGetValue("mark_right", out markRight) ||
                named.TryGetValue("mr", out markRight) ||
                (positional.Count > 11 && (markRight = positional[11]) != null))
            {
                if (!string.IsNullOrWhiteSpace(markRight))
                {
                    if (!TryParseLaneMarking(markRight!, out markingRight))
                    {
                        errors.Add(new TrackLayoutError(lineNumber, $"Invalid right lane marking '{markRight}'.", line));
                        return false;
                    }
                }
            }

            var entryHeading = GetFloat(named, "heading_in", "entry_heading", positional, -1, out var entryHeadingValue)
                ? entryHeadingValue
                : 0f;
            var exitHeading = GetFloat(named, "heading_out", "exit_heading", positional, -1, out var exitHeadingValue)
                ? exitHeadingValue
                : 0f;

            IReadOnlyList<TrackPoint3> centerlinePoints = Array.Empty<TrackPoint3>();
            if (named.TryGetValue("centerline", out var centerlineValue) ||
                named.TryGetValue("points", out centerlineValue))
            {
                centerlinePoints = ParsePointList(centerlineValue, lineNumber, errors, line);
                if (centerlinePoints.Count == 0 && !string.IsNullOrWhiteSpace(centerlineValue))
                    return false;
            }

            IReadOnlyList<TrackPoint3> leftEdgePoints = Array.Empty<TrackPoint3>();
            if (named.TryGetValue("left_edge", out var leftEdgeValue) ||
                named.TryGetValue("left_points", out leftEdgeValue))
            {
                leftEdgePoints = ParsePointList(leftEdgeValue, lineNumber, errors, line);
                if (leftEdgePoints.Count == 0 && !string.IsNullOrWhiteSpace(leftEdgeValue))
                    return false;
            }

            IReadOnlyList<TrackPoint3> rightEdgePoints = Array.Empty<TrackPoint3>();
            if (named.TryGetValue("right_edge", out var rightEdgeValue) ||
                named.TryGetValue("right_points", out rightEdgeValue))
            {
                rightEdgePoints = ParsePointList(rightEdgeValue, lineNumber, errors, line);
                if (rightEdgePoints.Count == 0 && !string.IsNullOrWhiteSpace(rightEdgeValue))
                    return false;
            }

            var speedLimit = GetFloat(named, "speed_limit", "speed", positional, 12, out var speedValue)
                ? speedValue
                : 0f;

            var surface = TrackSurface.Asphalt;
            if (named.TryGetValue("surface", out var surfaceValue) || (positional.Count > 13 && (surfaceValue = positional[13]) != null))
            {
                if (!string.IsNullOrWhiteSpace(surfaceValue))
                {
                    var parsedSurface = ParseEnum<TrackSurface>(surfaceValue!, lineNumber, errors, line);
                    if (parsedSurface == null)
                        return false;
                    surface = parsedSurface.Value;
                }
            }

            var priority = GetFloat(named, "priority", "prio", positional, 14, out var priorityValue)
                ? (int)Math.Round(priorityValue)
                : 0;

            var allowedVehicles = new List<string>();
            if (named.TryGetValue("allowed_vehicles", out var vehiclesValue) ||
                named.TryGetValue("vehicles", out vehiclesValue))
            {
                foreach (var entry in SplitList(vehiclesValue))
                    AddIfNotEmpty(allowedVehicles, entry);
            }

            var metadata = CollectMetadata(named,
                "id", "lane",
                "owner_kind", "kind",
                "owner", "owner_id",
                "leg", "connector",
                "index", "idx",
                "width", "w",
                "center_offset", "offset",
                "shoulder_left", "sl",
                "shoulder_right", "sr",
                "type", "lane_type",
                "direction", "dir",
                "marking_left", "mark_left", "ml",
                "marking_right", "mark_right", "mr",
                "heading_in", "entry_heading",
                "heading_out", "exit_heading",
                "centerline", "points",
                "left_edge", "left_points",
                "right_edge", "right_points",
                "speed_limit", "speed",
                "surface",
                "priority", "prio",
                "allowed_vehicles", "vehicles");

            try
            {
                var intersection = node.Intersection ??= new IntersectionBuilder();
                intersection.Lanes.Add(new TrackLane(
                    id!.Trim(),
                    ownerKind,
                    ownerId!.Trim(),
                    index,
                    width,
                    centerOffset,
                    shoulderLeft,
                    shoulderRight,
                    laneType,
                    direction,
                    markingLeft,
                    markingRight,
                    entryHeading,
                    exitHeading,
                    centerlinePoints,
                    leftEdgePoints,
                    rightEdgePoints,
                    speedLimit,
                    surface,
                    priority,
                    allowedVehicles,
                    metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseIntersectionLaneLink(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "link", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var from = GetString(named, "from", "src", positional, 1, errors, lineNumber, line);
            var to = GetString(named, "to", "dst", positional, 2, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return false;

            var turnValue = named.TryGetValue("turn", out var turnText) ? turnText : (positional.Count > 3 ? positional[3] : string.Empty);
            var turnDirection = TrackTurnDirection.Unknown;
            if (!string.IsNullOrWhiteSpace(turnValue))
            {
                if (!TryParseTurnDirection(turnValue, out turnDirection))
                {
                    errors.Add(new TrackLayoutError(lineNumber, $"Invalid turn direction '{turnValue}'.", line));
                    return false;
                }
            }

            var allowsLaneChange = false;
            if (named.TryGetValue("lane_change", out var laneChangeValue) ||
                named.TryGetValue("allow_change", out laneChangeValue) ||
                named.TryGetValue("change", out laneChangeValue))
            {
                allowsLaneChange = ParseBoolValue(laneChangeValue, false, out var parsed);
                if (!parsed)
                {
                    errors.Add(new TrackLayoutError(lineNumber, "Invalid lane_change value.", line));
                    return false;
                }
            }

            var changeLength = GetFloat(named, "change_length", "lane_change_length", positional, -1, out var changeLengthValue)
                ? changeLengthValue
                : 0f;

            var priority = GetFloat(named, "priority", "prio", positional, 4, out var priorityValue)
                ? (int)Math.Round(priorityValue)
                : 0;

            var metadata = CollectMetadata(named,
                "id", "link",
                "from", "src",
                "to", "dst",
                "turn",
                "lane_change", "allow_change", "change",
                "change_length", "lane_change_length",
                "priority", "prio");

            try
            {
                var intersection = node.Intersection ??= new IntersectionBuilder();
                intersection.LaneLinks.Add(new TrackLaneLink(
                    id!.Trim(),
                    from!.Trim(),
                    to!.Trim(),
                    turnDirection,
                    allowsLaneChange,
                    changeLength,
                    priority,
                    metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseIntersectionArea(string line, int lineNumber, NodeBuilder node, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "area", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var shapeValue = named.TryGetValue("shape", out var shapeText) ? shapeText : (positional.Count > 1 ? positional[1] : string.Empty);
            if (!TryParseIntersectionAreaShape(shapeValue, out var shape))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid area shape '{shapeValue}'.", line));
                return false;
            }

            var kindValue = named.TryGetValue("kind", out var kindText) ||
                            named.TryGetValue("type", out kindText)
                ? kindText
                : (positional.Count > 2 ? positional[2] : "core");
            if (!TryParseIntersectionAreaKind(kindValue, out var kind))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid area kind '{kindValue}'.", line));
                return false;
            }

            var radius = GetFloat(named, "radius", "r", positional, 3, out var radiusValue) ? radiusValue : 0f;
            var width = GetFloat(named, "width", "w", positional, 4, out var widthValue) ? widthValue : 0f;
            var length = GetFloat(named, "length", "len", positional, 5, out var lengthValue) ? lengthValue : 0f;

            float offsetX = 0f;
            float offsetZ = 0f;
            if (named.TryGetValue("offset", out var offsetValue) || named.TryGetValue("center", out offsetValue))
            {
                if (!TryParsePoint(offsetValue, lineNumber, errors, line, out offsetX, out _, out offsetZ))
                    return false;
            }
            else
            {
                if (GetFloat(named, "offset_x", "x", positional, 6, out var offsetXValue))
                    offsetX = offsetXValue;
                if (GetFloat(named, "offset_z", "z", positional, 7, out var offsetZValue))
                    offsetZ = offsetZValue;
            }

            var heading = GetFloat(named, "heading", "heading_deg", positional, 8, out var headingValue)
                ? headingValue
                : 0f;

            var elevation = GetFloat(named, "elevation", "elev", positional, -1, out var elevationValue)
                ? elevationValue
                : 0f;

            var surface = TrackSurface.Asphalt;
            if (named.TryGetValue("surface", out var surfaceValue))
            {
                if (!string.IsNullOrWhiteSpace(surfaceValue))
                {
                    var parsedSurface = ParseEnum<TrackSurface>(surfaceValue, lineNumber, errors, line);
                    if (parsedSurface == null)
                        return false;
                    surface = parsedSurface.Value;
                }
            }

            IReadOnlyList<TrackPoint3> points = Array.Empty<TrackPoint3>();
            if (named.TryGetValue("points", out var pointsValue) || named.TryGetValue("pts", out pointsValue))
            {
                points = ParsePointList(pointsValue, lineNumber, errors, line);
                if (points.Count == 0 && !string.IsNullOrWhiteSpace(pointsValue))
                    return false;
            }

            var metadata = CollectMetadata(named,
                "id", "area",
                "shape",
                "kind", "type",
                "radius", "r",
                "width", "w",
                "length", "len",
                "offset", "center",
                "offset_x", "x",
                "offset_z", "z",
                "heading", "heading_deg",
                "elevation", "elev",
                "surface",
                "points", "pts");

            try
            {
                var intersection = node.Intersection ??= new IntersectionBuilder();
                intersection.Areas.Add(new TrackIntersectionArea(
                    id!.Trim(),
                    shape,
                    kind,
                    radius,
                    width,
                    length,
                    offsetX,
                    offsetZ,
                    heading,
                    elevation,
                    surface,
                    points,
                    metadata));
            }
            catch (Exception ex)
            {
                errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                return false;
            }

            return true;
        }

        private static bool TryParseEdgeLine(string line, int lineNumber, Dictionary<string, EdgeBuilder> edges, List<string> edgeOrder, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "edge", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var edge = GetOrCreateEdge(id!, edges, edgeOrder);
            var from = GetString(named, "from", "src", positional, 1, errors, lineNumber, line);
            var to = GetString(named, "to", "dst", positional, 2, errors, lineNumber, line);
            if (!string.IsNullOrWhiteSpace(from))
                edge.FromNodeId = from;
            if (!string.IsNullOrWhiteSpace(to))
                edge.ToNodeId = to;

            if (named.TryGetValue("name", out var edgeName))
                edge.Name = edgeName;
            if (named.TryGetValue("short_name", out var edgeShort))
                edge.ShortName = edgeShort;
            if (string.IsNullOrWhiteSpace(edge.ShortName) && named.TryGetValue("short", out var edgeShortAlt))
                edge.ShortName = edgeShortAlt;

            if (named.TryGetValue("connector_from", out var connectorFrom) ||
                named.TryGetValue("from_edge", out connectorFrom) ||
                named.TryGetValue("from_edges", out connectorFrom))
            {
                foreach (var entry in SplitList(connectorFrom))
                    AddIfNotEmpty(edge.ConnectorFromEdgeIds, entry);
            }

            if (named.TryGetValue("turn", out var turnValue) || named.TryGetValue("turn_direction", out turnValue))
            {
                if (TryParseTurnDirection(turnValue, out var turnDirection))
                    edge.TurnDirection = turnDirection;
                else
                    errors.Add(new TrackLayoutError(lineNumber, $"Invalid turn direction '{turnValue}'.", line));
            }

            if (named.TryGetValue("allowed_vehicles", out var vehicleList) || named.TryGetValue("vehicles", out vehicleList))
            {
                foreach (var entry in SplitList(vehicleList))
                    AddIfNotEmpty(edge.AllowedVehicles, entry);
            }

            foreach (var kvp in named)
            {
                if (kvp.Key.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("edge", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("from", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("src", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("to", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("dst", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("short_name", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("short", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("allowed_vehicles", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("vehicles", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("connector_from", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("from_edge", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("from_edges", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("turn", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("turn_direction", StringComparison.OrdinalIgnoreCase))
                    continue;
                edge.Metadata[kvp.Key] = kvp.Value;
            }

            return true;
        }

        private static bool TryParseRouteLine(string line, int lineNumber, List<RouteBuilder> routes, List<TrackLayoutError> errors)
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

            var id = GetString(named, "id", "route", positional, 0, errors, lineNumber, line);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            string? edgesToken = null;
            if (named.TryGetValue("edges", out var edgesValue) || named.TryGetValue("path", out edgesValue))
                edgesToken = edgesValue;

            var edgeIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(edgesToken))
            {
                foreach (var entry in SplitList(edgesToken))
                    AddIfNotEmpty(edgeIds, entry);
            }
            else if (positional.Count > 1)
            {
                for (var i = 1; i < positional.Count; i++)
                    AddIfNotEmpty(edgeIds, positional[i]);
            }

            if (edgeIds.Count == 0)
            {
                errors.Add(new TrackLayoutError(lineNumber, "Route requires edge list.", line));
                return false;
            }

            bool? isLoop = null;
            if (named.TryGetValue("is_loop", out var loopValue) || named.TryGetValue("loop", out loopValue))
            {
                isLoop = ParseBoolValue(loopValue, false, out var parsed);
                if (!parsed)
                {
                    errors.Add(new TrackLayoutError(lineNumber, "Invalid loop value.", line));
                    return false;
                }
            }

            routes.Add(new RouteBuilder(id!, edgeIds, isLoop));
            return true;
        }

        private static void ParseEdgeProperties(string line, int lineNumber, EdgeBuilder edge, List<TrackLayoutError> errors)
        {
            var tokens = SplitTokens(line);
            if (tokens.Count == 0)
                return;

            foreach (var token in tokens)
            {
                if (!TrySplitKeyValue(token, out var key, out var value))
                {
                    errors.Add(new TrackLayoutError(lineNumber, "Expected key=value in edge section.", line));
                    continue;
                }

                switch (key.ToLowerInvariant())
                {
                    case "name":
                        edge.Name = value;
                        break;
                    case "short_name":
                    case "short":
                        edge.ShortName = value;
                        break;
                    case "from":
                        edge.FromNodeId = value;
                        break;
                    case "to":
                        edge.ToNodeId = value;
                        break;
                    case "connector_from":
                    case "from_edge":
                    case "from_edges":
                        foreach (var entry in SplitList(value))
                            AddIfNotEmpty(edge.ConnectorFromEdgeIds, entry);
                        break;
                    case "turn":
                    case "turn_direction":
                        if (TryParseTurnDirection(value, out var turnDirection))
                            edge.TurnDirection = turnDirection;
                        else
                            errors.Add(new TrackLayoutError(lineNumber, $"Invalid turn direction '{value}'.", line));
                        break;
                    case "default_surface":
                    case "surface":
                        var surface = ParseEnum<TrackSurface>(value, lineNumber, errors, line);
                        if (surface.HasValue)
                        {
                            edge.DefaultSurface = surface.Value;
                            edge.HasDefaultSurface = true;
                        }
                        break;
                    case "default_noise":
                    case "noise":
                        var noise = ParseEnum<TrackNoise>(value, lineNumber, errors, line);
                        if (noise.HasValue)
                        {
                            edge.DefaultNoise = noise.Value;
                            edge.HasDefaultNoise = true;
                        }
                        break;
                    case "default_width":
                    case "width":
                        edge.DefaultWidthMeters = ParseFloat(value, lineNumber, errors, line);
                        edge.HasDefaultWidth = true;
                        break;
                    case "default_weather":
                    case "weather":
                        var weather = ParseEnum<TrackWeather>(value, lineNumber, errors, line);
                        if (weather.HasValue)
                        {
                            edge.DefaultWeather = weather.Value;
                            edge.HasDefaultWeather = true;
                        }
                        break;
                    case "default_ambience":
                    case "ambience":
                        var ambience = ParseEnum<TrackAmbience>(value, lineNumber, errors, line);
                        if (ambience.HasValue)
                        {
                            edge.DefaultAmbience = ambience.Value;
                            edge.HasDefaultAmbience = true;
                        }
                        break;
                    case "sample_spacing":
                        edge.SampleSpacingMeters = ParseFloat(value, lineNumber, errors, line);
                        edge.HasSampleSpacing = true;
                        break;
                    case "enforce_closure":
                        edge.EnforceClosure = ParseBoolValue(value, true, out var parsedClosure);
                        edge.HasEnforceClosure = parsedClosure;
                        if (!parsedClosure)
                            errors.Add(new TrackLayoutError(lineNumber, "Invalid enforce_closure value.", line));
                        break;
                    case "allowed_vehicles":
                    case "vehicles":
                        foreach (var entry in SplitList(value))
                            AddIfNotEmpty(edge.AllowedVehicles, entry);
                        break;
                    default:
                        edge.Metadata[key] = value;
                        break;
                }
            }
        }

        private static bool InferRouteLoop(IReadOnlyList<string> edgeIds, Dictionary<string, EdgeBuilder> edges)
        {
            if (edgeIds == null || edgeIds.Count == 0)
                return false;
            if (!edges.TryGetValue(edgeIds[0], out var firstEdge))
                return false;
            if (!edges.TryGetValue(edgeIds[edgeIds.Count - 1], out var lastEdge))
                return false;
            if (edgeIds.Count == 1)
                return string.Equals(firstEdge.FromNodeId, firstEdge.ToNodeId, StringComparison.OrdinalIgnoreCase);
            return string.Equals(firstEdge.FromNodeId, lastEdge.ToNodeId, StringComparison.OrdinalIgnoreCase);
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

        private static bool TryParseTurnDirection(string value, out TrackTurnDirection direction)
        {
            direction = TrackTurnDirection.Unknown;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim();
            if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                direction = TrackTurnDirection.Unknown;
                return true;
            }
            if (trimmed.Equals("left", StringComparison.OrdinalIgnoreCase))
            {
                direction = TrackTurnDirection.Left;
                return true;
            }
            if (trimmed.Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                direction = TrackTurnDirection.Right;
                return true;
            }
            if (trimmed.Equals("straight", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("forward", StringComparison.OrdinalIgnoreCase))
            {
                direction = TrackTurnDirection.Straight;
                return true;
            }
            if (trimmed.Equals("uturn", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("u_turn", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("u-turn", StringComparison.OrdinalIgnoreCase))
            {
                direction = TrackTurnDirection.UTurn;
                return true;
            }
            return false;
        }

        private static string FormatTurnDirection(TrackTurnDirection direction)
        {
            switch (direction)
            {
                case TrackTurnDirection.Left:
                    return "left";
                case TrackTurnDirection.Right:
                    return "right";
                case TrackTurnDirection.Straight:
                    return "straight";
                case TrackTurnDirection.UTurn:
                    return "uturn";
                default:
                    return "unknown";
            }
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

        private static bool TryParseBoundarySide(string value, out TrackBoundarySide side)
        {
            side = TrackBoundarySide.Both;
            if (value.Equals("left", StringComparison.OrdinalIgnoreCase))
            {
                side = TrackBoundarySide.Left;
                return true;
            }
            if (value.Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                side = TrackBoundarySide.Right;
                return true;
            }
            if (value.Equals("both", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("lr", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("rl", StringComparison.OrdinalIgnoreCase))
            {
                side = TrackBoundarySide.Both;
                return true;
            }
            return false;
        }

        private static bool TryParseBoundaryType(string value, out TrackBoundaryType boundaryType)
        {
            boundaryType = TrackBoundaryType.Unknown;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (Enum.TryParse(value, true, out TrackBoundaryType parsed))
            {
                boundaryType = parsed;
                return true;
            }
            return false;
        }

        private static bool TryParseIntersectionShape(string value, out TrackIntersectionShape shape)
        {
            shape = TrackIntersectionShape.Unspecified;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (Enum.TryParse(value, true, out TrackIntersectionShape parsed))
            {
                shape = parsed;
                return true;
            }
            return false;
        }

        private static bool TryParseIntersectionControl(string value, out TrackIntersectionControl control)
        {
            control = TrackIntersectionControl.None;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (Enum.TryParse(value, true, out TrackIntersectionControl parsed))
            {
                control = parsed;
                return true;
            }
            return false;
        }

        private static bool TryParseLegType(string value, out TrackIntersectionLegType legType)
        {
            legType = TrackIntersectionLegType.Both;
            if (value.Equals("entry", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                legType = TrackIntersectionLegType.Entry;
                return true;
            }
            if (value.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("out", StringComparison.OrdinalIgnoreCase))
            {
                legType = TrackIntersectionLegType.Exit;
                return true;
            }
            if (value.Equals("both", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("bi", StringComparison.OrdinalIgnoreCase))
            {
                legType = TrackIntersectionLegType.Both;
                return true;
            }
            return false;
        }

        private static bool TryParseLaneOwnerKind(string value, out TrackLaneOwnerKind ownerKind)
        {
            ownerKind = TrackLaneOwnerKind.Leg;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = NormalizeToken(value);
            if (normalized == "leg")
            {
                ownerKind = TrackLaneOwnerKind.Leg;
                return true;
            }
            if (normalized == "connector" || normalized == "conn")
            {
                ownerKind = TrackLaneOwnerKind.Connector;
                return true;
            }
            return false;
        }

        private static bool TryParseLaneDirection(string value, out TrackLaneDirection direction)
        {
            direction = TrackLaneDirection.Forward;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = NormalizeToken(value);
            switch (normalized)
            {
                case "forward":
                case "fwd":
                case "outbound":
                    direction = TrackLaneDirection.Forward;
                    return true;
                case "reverse":
                case "rev":
                case "backward":
                case "inbound":
                    direction = TrackLaneDirection.Reverse;
                    return true;
                case "both":
                case "bi":
                case "bidirectional":
                case "two":
                case "twoWay":
                case "twoway":
                    direction = TrackLaneDirection.Both;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseLaneType(string value, out TrackLaneType laneType)
        {
            laneType = TrackLaneType.Travel;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = NormalizeToken(value);
            switch (normalized)
            {
                case "travel":
                case "through":
                    laneType = TrackLaneType.Travel;
                    return true;
                case "turnleft":
                case "leftturn":
                case "left":
                    laneType = TrackLaneType.TurnLeft;
                    return true;
                case "turnright":
                case "rightturn":
                case "right":
                    laneType = TrackLaneType.TurnRight;
                    return true;
                case "merge":
                    laneType = TrackLaneType.Merge;
                    return true;
                case "exit":
                    laneType = TrackLaneType.Exit;
                    return true;
                case "entry":
                    laneType = TrackLaneType.Entry;
                    return true;
                case "shoulder":
                    laneType = TrackLaneType.Shoulder;
                    return true;
                case "bike":
                case "bicycle":
                    laneType = TrackLaneType.Bike;
                    return true;
                case "bus":
                    laneType = TrackLaneType.Bus;
                    return true;
                case "pit":
                case "pitlane":
                    laneType = TrackLaneType.Pit;
                    return true;
                case "parking":
                    laneType = TrackLaneType.Parking;
                    return true;
                case "emergency":
                    laneType = TrackLaneType.Emergency;
                    return true;
                case "custom":
                    laneType = TrackLaneType.Custom;
                    return true;
                default:
                    if (Enum.TryParse(value, true, out TrackLaneType parsed))
                    {
                        laneType = parsed;
                        return true;
                    }
                    return false;
            }
        }

        private static bool TryParseLaneMarking(string value, out TrackLaneMarking marking)
        {
            marking = TrackLaneMarking.None;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = NormalizeToken(value);
            switch (normalized)
            {
                case "none":
                    marking = TrackLaneMarking.None;
                    return true;
                case "solid":
                    marking = TrackLaneMarking.Solid;
                    return true;
                case "dashed":
                    marking = TrackLaneMarking.Dashed;
                    return true;
                case "doublesolid":
                    marking = TrackLaneMarking.DoubleSolid;
                    return true;
                case "doubledashed":
                    marking = TrackLaneMarking.DoubleDashed;
                    return true;
                case "soliddashed":
                    marking = TrackLaneMarking.SolidDashed;
                    return true;
                case "dashedsolid":
                    marking = TrackLaneMarking.DashedSolid;
                    return true;
                default:
                    if (Enum.TryParse(value, true, out TrackLaneMarking parsed))
                    {
                        marking = parsed;
                        return true;
                    }
                    return false;
            }
        }

        private static bool TryParseIntersectionAreaShape(string value, out TrackIntersectionAreaShape shape)
        {
            shape = TrackIntersectionAreaShape.Circle;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = NormalizeToken(value);
            switch (normalized)
            {
                case "circle":
                    shape = TrackIntersectionAreaShape.Circle;
                    return true;
                case "box":
                case "rect":
                case "rectangle":
                    shape = TrackIntersectionAreaShape.Box;
                    return true;
                case "poly":
                case "polygon":
                    shape = TrackIntersectionAreaShape.Polygon;
                    return true;
                default:
                    if (Enum.TryParse(value, true, out TrackIntersectionAreaShape parsed))
                    {
                        shape = parsed;
                        return true;
                    }
                    return false;
            }
        }

        private static bool TryParseIntersectionAreaKind(string value, out TrackIntersectionAreaKind kind)
        {
            kind = TrackIntersectionAreaKind.Core;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = NormalizeToken(value);
            switch (normalized)
            {
                case "core":
                    kind = TrackIntersectionAreaKind.Core;
                    return true;
                case "conflict":
                    kind = TrackIntersectionAreaKind.Conflict;
                    return true;
                case "island":
                    kind = TrackIntersectionAreaKind.Island;
                    return true;
                case "crosswalk":
                    kind = TrackIntersectionAreaKind.Crosswalk;
                    return true;
                case "stopline":
                case "stop_line":
                    kind = TrackIntersectionAreaKind.StopLine;
                    return true;
                case "median":
                    kind = TrackIntersectionAreaKind.Median;
                    return true;
                case "sidewalk":
                    kind = TrackIntersectionAreaKind.Sidewalk;
                    return true;
                case "shoulder":
                    kind = TrackIntersectionAreaKind.Shoulder;
                    return true;
                case "custom":
                    kind = TrackIntersectionAreaKind.Custom;
                    return true;
                default:
                    if (Enum.TryParse(value, true, out TrackIntersectionAreaKind parsed))
                    {
                        kind = parsed;
                        return true;
                    }
                    return false;
            }
        }

        private static string FormatBoundarySide(TrackBoundarySide side)
        {
            switch (side)
            {
                case TrackBoundarySide.Left:
                    return "left";
                case TrackBoundarySide.Right:
                    return "right";
                default:
                    return "both";
            }
        }

        private static string FormatBoundaryType(TrackBoundaryType boundaryType)
        {
            return boundaryType.ToString().ToLowerInvariant();
        }

        private static string FormatIntersectionShape(TrackIntersectionShape shape)
        {
            return shape.ToString().ToLowerInvariant();
        }

        private static string FormatIntersectionControl(TrackIntersectionControl control)
        {
            return control.ToString().ToLowerInvariant();
        }

        private static string FormatLegType(TrackIntersectionLegType legType)
        {
            switch (legType)
            {
                case TrackIntersectionLegType.Entry:
                    return "entry";
                case TrackIntersectionLegType.Exit:
                    return "exit";
                default:
                    return "both";
            }
        }

        private static string FormatLaneOwnerKind(TrackLaneOwnerKind ownerKind)
        {
            return ownerKind == TrackLaneOwnerKind.Connector ? "connector" : "leg";
        }

        private static string FormatLaneDirection(TrackLaneDirection direction)
        {
            switch (direction)
            {
                case TrackLaneDirection.Reverse:
                    return "reverse";
                case TrackLaneDirection.Both:
                    return "both";
                default:
                    return "forward";
            }
        }

        private static string FormatLaneType(TrackLaneType laneType)
        {
            switch (laneType)
            {
                case TrackLaneType.TurnLeft:
                    return "turn_left";
                case TrackLaneType.TurnRight:
                    return "turn_right";
                case TrackLaneType.Merge:
                    return "merge";
                case TrackLaneType.Exit:
                    return "exit";
                case TrackLaneType.Entry:
                    return "entry";
                case TrackLaneType.Shoulder:
                    return "shoulder";
                case TrackLaneType.Bike:
                    return "bike";
                case TrackLaneType.Bus:
                    return "bus";
                case TrackLaneType.Pit:
                    return "pit";
                case TrackLaneType.Parking:
                    return "parking";
                case TrackLaneType.Emergency:
                    return "emergency";
                case TrackLaneType.Custom:
                    return "custom";
                default:
                    return "travel";
            }
        }

        private static string FormatLaneMarking(TrackLaneMarking marking)
        {
            switch (marking)
            {
                case TrackLaneMarking.Solid:
                    return "solid";
                case TrackLaneMarking.Dashed:
                    return "dashed";
                case TrackLaneMarking.DoubleSolid:
                    return "double_solid";
                case TrackLaneMarking.DoubleDashed:
                    return "double_dashed";
                case TrackLaneMarking.SolidDashed:
                    return "solid_dashed";
                case TrackLaneMarking.DashedSolid:
                    return "dashed_solid";
                default:
                    return "none";
            }
        }

        private static string FormatIntersectionAreaShape(TrackIntersectionAreaShape shape)
        {
            switch (shape)
            {
                case TrackIntersectionAreaShape.Box:
                    return "box";
                case TrackIntersectionAreaShape.Polygon:
                    return "polygon";
                default:
                    return "circle";
            }
        }

        private static string FormatIntersectionAreaKind(TrackIntersectionAreaKind kind)
        {
            switch (kind)
            {
                case TrackIntersectionAreaKind.Conflict:
                    return "conflict";
                case TrackIntersectionAreaKind.Island:
                    return "island";
                case TrackIntersectionAreaKind.Crosswalk:
                    return "crosswalk";
                case TrackIntersectionAreaKind.StopLine:
                    return "stop_line";
                case TrackIntersectionAreaKind.Median:
                    return "median";
                case TrackIntersectionAreaKind.Sidewalk:
                    return "sidewalk";
                case TrackIntersectionAreaKind.Shoulder:
                    return "shoulder";
                case TrackIntersectionAreaKind.Custom:
                    return "custom";
                default:
                    return "core";
            }
        }

        private readonly struct SectionInfo
        {
            public static readonly SectionInfo Empty = new SectionInfo(string.Empty, null, null, null);
            public string Kind { get; }
            public string? NodeId { get; }
            public string? EdgeId { get; }
            public string? Subsection { get; }

            public SectionInfo(string kind, string? nodeId, string? edgeId, string? subsection)
            {
                Kind = kind;
                NodeId = nodeId;
                EdgeId = edgeId;
                Subsection = subsection;
            }
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

        private static string? ParseString(Dictionary<string, string> named, string key)
        {
            if (named.TryGetValue(key, out var value))
            {
                var trimmed = value?.Trim();
                return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
            }
            return null;
        }

        private static string? GetString(Dictionary<string, string> named, string key, string altKey, List<string> positional, int positionalIndex, List<TrackLayoutError> errors, int lineNumber, string line)
        {
            if (named.TryGetValue(key, out var value) || named.TryGetValue(altKey, out value))
            {
                var trimmed = value?.Trim();
                return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
            }

            if (positionalIndex >= 0 && positionalIndex < positional.Count)
            {
                var trimmed = positional[positionalIndex].Trim();
                return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
            }

            errors.Add(new TrackLayoutError(lineNumber, $"Missing {key}.", line));
            return null;
        }

        private static List<int> ParseIntList(string? value, int lineNumber, List<TrackLayoutError> errors, string line)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(value))
                return result;
            foreach (var part in SplitList(value))
            {
                if (int.TryParse(part, NumberStyles.Integer, Culture, out var parsed))
                    result.Add(parsed);
                else
                    errors.Add(new TrackLayoutError(lineNumber, $"Invalid integer '{part}'.", line));
            }
            return result;
        }

        private static IReadOnlyList<string> SplitList(string? value)
        {
            if (value == null)
                return Array.Empty<string>();
            var trimmedValue = value.Trim();
            if (trimmedValue.Length == 0)
                return Array.Empty<string>();
            var parts = trimmedValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>(parts.Length);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    list.Add(trimmed);
            }
            return list;
        }

        private static void AddIfNotEmpty(List<string> list, string? value)
        {
            var trimmed = value?.Trim();
            if (trimmed == null || trimmed.Length == 0)
                return;
            list.Add(trimmed);
        }

        private static Dictionary<string, string> CollectMetadata(Dictionary<string, string> named, params string[] reservedKeys)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var reserved = new HashSet<string>(reservedKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in named)
            {
                if (reserved.Contains(kvp.Key))
                    continue;
                metadata[kvp.Key] = kvp.Value;
            }
            return metadata;
        }

        private static bool ParseBoolValue(string? value, bool defaultValue, out bool parsed)
        {
            parsed = true;
            if (string.IsNullOrWhiteSpace(value))
            {
                parsed = false;
                return defaultValue;
            }

            var normalized = value!.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                    return true;
                case "false":
                case "0":
                case "no":
                case "n":
                    return false;
                default:
                    parsed = false;
                    return defaultValue;
            }
        }

        private static string NormalizeToken(string value)
        {
            return value.Trim()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

        private static IReadOnlyList<TrackPoint3> ParsePointList(string? value, int lineNumber, List<TrackLayoutError> errors, string line)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<TrackPoint3>();

            var list = new List<TrackPoint3>();
            var hasError = false;
            var segments = value!.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (!TryParsePoint(segment, lineNumber, errors, line, out var x, out var y, out var z))
                {
                    hasError = true;
                    continue;
                }
                try
                {
                    list.Add(new TrackPoint3(x, y, z));
                }
                catch (Exception ex)
                {
                    errors.Add(new TrackLayoutError(lineNumber, ex.Message, line));
                    hasError = true;
                }
            }

            return hasError ? Array.Empty<TrackPoint3>() : list;
        }

        private static bool TryParsePoint(string value, int lineNumber, List<TrackLayoutError> errors, string line, out float x, out float y, out float z)
        {
            x = 0f;
            y = 0f;
            z = 0f;
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new TrackLayoutError(lineNumber, "Point value is empty.", line));
                return false;
            }

            var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid point '{value}'. Expected x,z or x,y,z.", line));
                return false;
            }
            if (parts.Length > 3)
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid point '{value}'. Expected x,z or x,y,z.", line));
                return false;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, Culture, out x))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid point '{value}'. Expected numeric x,z or x,y,z.", line));
                return false;
            }

            if (parts.Length == 2)
            {
                if (!float.TryParse(parts[1], NumberStyles.Float, Culture, out z))
                {
                    errors.Add(new TrackLayoutError(lineNumber, $"Invalid point '{value}'. Expected numeric x,z.", line));
                    return false;
                }
                y = 0f;
                return true;
            }

            if (!float.TryParse(parts[1], NumberStyles.Float, Culture, out y) ||
                !float.TryParse(parts[2], NumberStyles.Float, Culture, out z))
            {
                errors.Add(new TrackLayoutError(lineNumber, $"Invalid point '{value}'. Expected numeric x,y,z.", line));
                return false;
            }

            return true;
        }

        private static string FormatPointList(IReadOnlyList<TrackPoint3> points)
        {
            if (points == null || points.Count == 0)
                return string.Empty;
            var hasY = false;
            for (var i = 0; i < points.Count; i++)
            {
                if (Math.Abs(points[i].Y) > 0.0001f)
                {
                    hasY = true;
                    break;
                }
            }
            var sb = new StringBuilder();
            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                    sb.Append('|');
                sb.Append(FormatFloat(points[i].X));
                if (hasY)
                {
                    sb.Append(',');
                    sb.Append(FormatFloat(points[i].Y));
                }
                sb.Append(',');
                sb.Append(FormatFloat(points[i].Z));
            }
            return sb.ToString();
        }

        private static string FormatPoint(float x, float z)
        {
            return $"{FormatFloat(x)},{FormatFloat(z)}";
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

        private static bool TryParseSection(string line, out SectionInfo section)
        {
            section = SectionInfo.Empty;
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
                return false;

            var content = trimmed.Substring(1, trimmed.Length - 2).Trim();
            if (string.IsNullOrWhiteSpace(content))
                return false;

            if (content.StartsWith("edge", StringComparison.OrdinalIgnoreCase))
            {
                var rest = content.Substring(4).Trim();
                if (rest.StartsWith(":", StringComparison.Ordinal))
                    rest = rest.Substring(1).Trim();
                if (string.IsNullOrWhiteSpace(rest))
                {
                    section = new SectionInfo("edge", null, null, null);
                    return true;
                }

                string edgeId = rest;
                string? subsection = null;
                var dot = rest.IndexOf('.');
                if (dot >= 0)
                {
                    edgeId = rest.Substring(0, dot);
                    subsection = rest.Substring(dot + 1);
                }
                section = new SectionInfo("edge", null, edgeId.Trim(), subsection?.Trim().ToLowerInvariant());
                return true;
            }

            if (content.StartsWith("node", StringComparison.OrdinalIgnoreCase))
            {
                var rest = content.Substring(4).Trim();
                if (rest.StartsWith(":", StringComparison.Ordinal))
                    rest = rest.Substring(1).Trim();
                if (string.IsNullOrWhiteSpace(rest))
                {
                    section = new SectionInfo("node", null, null, null);
                    return true;
                }

                string nodeId = rest;
                string? subsection = null;
                var dot = rest.IndexOf('.');
                if (dot >= 0)
                {
                    nodeId = rest.Substring(0, dot);
                    subsection = rest.Substring(dot + 1);
                }
                section = new SectionInfo("node", nodeId.Trim(), null, subsection?.Trim().ToLowerInvariant());
                return true;
            }

            section = new SectionInfo(content.ToLowerInvariant(), null, null, null);
            return true;
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
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(line))
                return tokens;

            var current = new StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '\"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens;
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

        private static bool TryParseSlope(Dictionary<string, string> named, string key, string altKey, out float slope)
        {
            if (named.TryGetValue(key, out var value) || named.TryGetValue(altKey, out value))
            {
                if (TryParsePercent(value, out var percent))
                {
                    slope = percent / 100f;
                    return true;
                }
            }

            slope = 0f;
            return false;
        }

        private static bool TryParsePercent(string value, out float percent)
        {
            percent = 0f;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim();
            if (trimmed.EndsWith("%", StringComparison.Ordinal))
                trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();
            return float.TryParse(trimmed, NumberStyles.Float, Culture, out percent);
        }

        private static string FormatPercent(float slope)
        {
            return (slope * 100f).ToString("0.###", Culture);
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

        private sealed class NodeBuilder
        {
            public string Id { get; }
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public Dictionary<string, string> Metadata { get; }
            public IntersectionBuilder? Intersection { get; set; }

            public NodeBuilder(string id)
            {
                Id = id;
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private sealed class IntersectionBuilder
        {
            public TrackIntersectionShape Shape = TrackIntersectionShape.Unspecified;
            public float RadiusMeters;
            public float InnerRadiusMeters;
            public float OuterRadiusMeters;
            public int EntryLanes;
            public int ExitLanes;
            public int TurnLanes;
            public float SpeedLimitKph;
            public TrackIntersectionControl Control = TrackIntersectionControl.None;
            public int Priority;
            public readonly List<TrackIntersectionLeg> Legs = new List<TrackIntersectionLeg>();
            public readonly List<TrackIntersectionConnector> Connectors = new List<TrackIntersectionConnector>();
            public readonly List<TrackLane> Lanes = new List<TrackLane>();
            public readonly List<TrackLaneLink> LaneLinks = new List<TrackLaneLink>();
            public readonly List<TrackIntersectionArea> Areas = new List<TrackIntersectionArea>();
            public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private bool _hasShape;
            private bool _hasRadius;
            private bool _hasInnerRadius;
            private bool _hasOuterRadius;
            private bool _hasEntryLanes;
            private bool _hasExitLanes;
            private bool _hasTurnLanes;
            private bool _hasSpeedLimit;
            private bool _hasControl;
            private bool _hasPriority;

            public void SetShape(TrackIntersectionShape shape)
            {
                Shape = shape;
                _hasShape = true;
            }

            public void SetRadius(float radiusMeters)
            {
                RadiusMeters = radiusMeters;
                _hasRadius = true;
            }

            public void SetInnerRadius(float innerRadiusMeters)
            {
                InnerRadiusMeters = innerRadiusMeters;
                _hasInnerRadius = true;
            }

            public void SetOuterRadius(float outerRadiusMeters)
            {
                OuterRadiusMeters = outerRadiusMeters;
                _hasOuterRadius = true;
            }

            public void SetEntryLanes(int entryLanes)
            {
                EntryLanes = entryLanes;
                _hasEntryLanes = true;
            }

            public void SetExitLanes(int exitLanes)
            {
                ExitLanes = exitLanes;
                _hasExitLanes = true;
            }

            public void SetTurnLanes(int turnLanes)
            {
                TurnLanes = turnLanes;
                _hasTurnLanes = true;
            }

            public void SetSpeedLimit(float speedLimitKph)
            {
                SpeedLimitKph = speedLimitKph;
                _hasSpeedLimit = true;
            }

            public void SetControl(TrackIntersectionControl control)
            {
                Control = control;
                _hasControl = true;
            }

            public void SetPriority(int priority)
            {
                Priority = priority;
                _hasPriority = true;
            }

            public bool HasAnyData =>
                _hasShape || _hasRadius || _hasInnerRadius || _hasOuterRadius ||
                _hasEntryLanes || _hasExitLanes || _hasTurnLanes ||
                _hasSpeedLimit || _hasControl || _hasPriority ||
                Legs.Count > 0 || Connectors.Count > 0 ||
                Lanes.Count > 0 || LaneLinks.Count > 0 || Areas.Count > 0 ||
                Metadata.Count > 0;

            public TrackIntersectionProfile? Build()
            {
                if (!HasAnyData)
                    return null;
                return new TrackIntersectionProfile(
                    Shape,
                    RadiusMeters,
                    InnerRadiusMeters,
                    OuterRadiusMeters,
                    EntryLanes,
                    ExitLanes,
                    TurnLanes,
                    SpeedLimitKph,
                    Control,
                    Priority,
                    Legs,
                    Connectors,
                    Lanes,
                    LaneLinks,
                    Areas,
                    Metadata);
            }
        }

        private sealed class EdgeBuilder
        {
            public string Id { get; }
            public string? FromNodeId { get; set; }
            public string? ToNodeId { get; set; }
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public readonly List<TrackGeometrySpan> GeometrySpans = new List<TrackGeometrySpan>();
            public readonly List<TrackZone<TrackSurface>> SurfaceZones = new List<TrackZone<TrackSurface>>();
            public readonly List<TrackZone<TrackNoise>> NoiseZones = new List<TrackZone<TrackNoise>>();
            public readonly List<TrackWidthZone> WidthZones = new List<TrackWidthZone>();
            public readonly List<TrackSpeedLimitZone> SpeedZones = new List<TrackSpeedLimitZone>();
            public readonly List<TrackMarker> Markers = new List<TrackMarker>();
            public readonly List<TrackWeatherZone> WeatherZones = new List<TrackWeatherZone>();
            public readonly List<TrackAmbienceZone> AmbienceZones = new List<TrackAmbienceZone>();
            public readonly List<TrackHazardZone> Hazards = new List<TrackHazardZone>();
            public readonly List<TrackCheckpoint> Checkpoints = new List<TrackCheckpoint>();
            public readonly List<TrackHitLaneZone> HitLanes = new List<TrackHitLaneZone>();
            public readonly List<string> AllowedVehicles = new List<string>();
            public readonly List<TrackAudioEmitter> Emitters = new List<TrackAudioEmitter>();
            public readonly List<TrackTriggerZone> Triggers = new List<TrackTriggerZone>();
            public readonly List<TrackBoundaryZone> Boundaries = new List<TrackBoundaryZone>();
            public readonly Dictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> ConnectorFromEdgeIds = new List<string>();
            public TrackTurnDirection TurnDirection = TrackTurnDirection.Unknown;

            public TrackSurface DefaultSurface;
            public TrackNoise DefaultNoise;
            public float DefaultWidthMeters;
            public TrackWeather DefaultWeather;
            public TrackAmbience DefaultAmbience;
            public float SampleSpacingMeters;
            public bool EnforceClosure;

            public bool HasDefaultSurface;
            public bool HasDefaultNoise;
            public bool HasDefaultWidth;
            public bool HasDefaultWeather;
            public bool HasDefaultAmbience;
            public bool HasSampleSpacing;
            public bool HasEnforceClosure;

            public EdgeBuilder(string id)
            {
                Id = id;
            }

            public TrackGraphEdge? Build(
                TrackSurface defaultSurface,
                TrackNoise defaultNoise,
                float defaultWidth,
                TrackWeather defaultWeather,
                TrackAmbience defaultAmbience,
                float defaultSampleSpacing,
                bool defaultEnforceClosure,
                List<TrackLayoutError> errors)
            {
                if (string.IsNullOrWhiteSpace(FromNodeId) || string.IsNullOrWhiteSpace(ToNodeId))
                {
                    errors.Add(new TrackLayoutError(0, $"Edge '{Id}' is missing from/to node."));
                    return null;
                }

                if (GeometrySpans.Count == 0)
                {
                    errors.Add(new TrackLayoutError(0, $"Edge '{Id}' has no geometry spans."));
                    return null;
                }

                var surface = HasDefaultSurface ? DefaultSurface : defaultSurface;
                var noise = HasDefaultNoise ? DefaultNoise : defaultNoise;
                var width = HasDefaultWidth ? DefaultWidthMeters : defaultWidth;
                var weather = HasDefaultWeather ? DefaultWeather : defaultWeather;
                var ambience = HasDefaultAmbience ? DefaultAmbience : defaultAmbience;
                var spacing = HasSampleSpacing ? SampleSpacingMeters : defaultSampleSpacing;
                var closure = HasEnforceClosure ? EnforceClosure : defaultEnforceClosure;

                if (!TrackGraphValidation.IsFinite(width) || width <= 0f)
                {
                    errors.Add(new TrackLayoutError(0, $"Edge '{Id}' has invalid width."));
                    return null;
                }

                TrackGeometrySpec geometry;
                try
                {
                    geometry = new TrackGeometrySpec(GeometrySpans, spacing, closure);
                }
                catch (Exception ex)
                {
                    errors.Add(new TrackLayoutError(0, $"Edge '{Id}' geometry error: {ex.Message}"));
                    return null;
                }

                var profile = new TrackEdgeProfile(
                    surface,
                    noise,
                    width,
                    weather,
                    ambience,
                    SurfaceZones,
                    NoiseZones,
                    WidthZones,
                    SpeedZones,
                    Markers,
                    WeatherZones,
                    AmbienceZones,
                    Hazards,
                    Checkpoints,
                    HitLanes,
                    AllowedVehicles,
                    Emitters,
                    Triggers,
                    Boundaries);

                return new TrackGraphEdge(
                    Id,
                    FromNodeId!,
                    ToNodeId!,
                    Name,
                    ShortName,
                    geometry,
                    profile,
                    ConnectorFromEdgeIds,
                    TurnDirection,
                    Metadata);
            }
        }

        private sealed class RouteBuilder
        {
            public string Id { get; }
            public List<string> EdgeIds { get; }
            public bool? IsLoop { get; }

            public RouteBuilder(string id, List<string> edgeIds, bool? isLoop)
            {
                Id = id;
                EdgeIds = edgeIds;
                IsLoop = isLoop;
            }
        }
    }
}
