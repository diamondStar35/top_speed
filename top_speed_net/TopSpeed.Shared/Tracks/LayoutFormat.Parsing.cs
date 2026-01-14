using System;
using System.Collections.Generic;
using System.IO;
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

    public static partial class TrackLayoutFormat
    {
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
            var startFinish = new Dictionary<string, StartFinishBuilder>(StringComparer.OrdinalIgnoreCase);
            var startFinishOrder = new List<string>();
            var pitLanes = new Dictionary<string, PitLaneBuilder>(StringComparer.OrdinalIgnoreCase);
            var pitLaneOrder = new List<string>();
            var cornerComplexes = new Dictionary<string, CornerComplexBuilder>(StringComparer.OrdinalIgnoreCase);
            var cornerOrder = new List<string>();

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
                                case "lane_groups":
                                case "lanegroups":
                                    TryParseIntersectionLaneGroup(line, lineNumber, node, errors);
                                    break;
                                case "lane_transitions":
                                case "lanetransitions":
                                case "lane_group_links":
                                case "lane_group_transitions":
                                    TryParseIntersectionLaneTransition(line, lineNumber, node, errors);
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
                    case "pit":
                        if (string.IsNullOrWhiteSpace(section.PitId))
                        {
                            errors.Add(new TrackLayoutError(lineNumber, "Pit section missing id.", rawLine));
                            break;
                        }
                        var pitLane = GetOrCreatePitLane(section.PitId!, pitLanes, pitLaneOrder);
                        if (string.IsNullOrWhiteSpace(section.Subsection))
                        {
                            ParsePitProperties(line, lineNumber, pitLane, errors);
                        }
                        else
                        {
                            switch (section.Subsection)
                            {
                                case "entry_lane":
                                case "entrylane":
                                    TryParsePitLaneSegment(line, lineNumber, pitLane, isEntry: true, errors);
                                    break;
                                case "exit_lane":
                                case "exitlane":
                                    TryParsePitLaneSegment(line, lineNumber, pitLane, isEntry: false, errors);
                                    break;
                                case "boxes":
                                case "pit_boxes":
                                case "pitboxes":
                                    TryParsePitBox(line, lineNumber, pitLane, errors);
                                    break;
                                case "speed_limits":
                                case "speed":
                                    TryParseSpeedLimit(line, lineNumber, pitLane.SpeedLimitZones, errors);
                                    break;
                                case "blend_line":
                                case "blendline":
                                    TryParsePitBlendLine(line, lineNumber, pitLane, errors);
                                    break;
                                default:
                                    errors.Add(new TrackLayoutError(lineNumber, $"Unknown pit section '{section.Subsection}'.", rawLine));
                                    break;
                            }
                        }
                        break;
                    case "corner":
                        if (string.IsNullOrWhiteSpace(section.CornerId))
                        {
                            errors.Add(new TrackLayoutError(lineNumber, "Corner section missing id.", rawLine));
                            break;
                        }
                        var corner = GetOrCreateCornerComplex(section.CornerId!, cornerComplexes, cornerOrder);
                        if (string.IsNullOrWhiteSpace(section.Subsection))
                        {
                            ParseCornerProperties(line, lineNumber, corner, errors);
                        }
                        else
                        {
                            switch (section.Subsection)
                            {
                                case "apexes":
                                case "apex":
                                    TryParseCornerApex(line, lineNumber, corner, errors);
                                    break;
                                case "braking":
                                case "braking_zone":
                                case "brake":
                                    ParseCornerBrakingZone(line, lineNumber, corner, errors);
                                    break;
                                case "acceleration":
                                case "accel":
                                case "accel_zone":
                                    ParseCornerAccelerationZone(line, lineNumber, corner, errors);
                                    break;
                                case "racing_lines":
                                case "racing_line":
                                case "racelines":
                                    TryParseRacingLine(line, lineNumber, corner, errors);
                                    break;
                                default:
                                    errors.Add(new TrackLayoutError(lineNumber, $"Unknown corner section '{section.Subsection}'.", rawLine));
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
                    case "start_finish":
                    case "startfinish":
                        if (string.IsNullOrWhiteSpace(section.StartFinishId))
                        {
                            TryParseStartFinishLine(line, lineNumber, startFinish, startFinishOrder, errors);
                            break;
                        }
                        var startFinishBuilder = GetOrCreateStartFinish(section.StartFinishId!, startFinish, startFinishOrder);
                        if (string.IsNullOrWhiteSpace(section.Subsection))
                        {
                            ParseStartFinishProperties(line, lineNumber, startFinishBuilder, errors);
                        }
                        else
                        {
                            switch (section.Subsection)
                            {
                                case "lanes":
                                    TryParseStartFinishLane(line, lineNumber, startFinishBuilder, errors);
                                    break;
                                case "lane_links":
                                case "lanelinks":
                                    TryParseStartFinishLaneLink(line, lineNumber, startFinishBuilder, errors);
                                    break;
                                case "lane_groups":
                                case "lanegroups":
                                    TryParseStartFinishLaneGroup(line, lineNumber, startFinishBuilder, errors);
                                    break;
                                case "lane_transitions":
                                case "lanetransitions":
                                case "lane_group_links":
                                case "lane_group_transitions":
                                    TryParseStartFinishLaneTransition(line, lineNumber, startFinishBuilder, errors);
                                    break;
                                case "areas":
                                    TryParseStartFinishArea(line, lineNumber, startFinishBuilder, errors);
                                    break;
                                default:
                                    errors.Add(new TrackLayoutError(lineNumber, $"Unknown start_finish section '{section.Subsection}'.", rawLine));
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

            foreach (var pitLane in pitLanes.Values)
            {
                if (string.IsNullOrWhiteSpace(pitLane.NodeId))
                {
                    errors.Add(new TrackLayoutError(0, $"Pit lane '{pitLane.Id}' missing node id."));
                    continue;
                }
                var node = GetOrCreateNode(pitLane.NodeId!, nodes);
                if (node.PitLane != null)
                {
                    errors.Add(new TrackLayoutError(0, $"Node '{pitLane.NodeId}' already has a pit lane."));
                    continue;
                }
                node.PitLane = pitLane;
            }

            foreach (var corner in cornerComplexes.Values)
            {
                if (string.IsNullOrWhiteSpace(corner.EdgeId))
                {
                    errors.Add(new TrackLayoutError(0, $"Corner complex '{corner.Id}' missing edge id."));
                    continue;
                }
                var edge = GetOrCreateEdge(corner.EdgeId!, edges, edgeOrder);
                edge.CornerComplexes.Add(corner);
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
                    node.Intersection?.Build(),
                    node.PitLane?.Build(errors)));
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
            var startFinishList = new List<TrackStartFinishSubgraph>(startFinishOrder.Count);
            foreach (var id in startFinishOrder)
            {
                if (!startFinish.TryGetValue(id, out var builder))
                    continue;
                var built = builder.Build(defaultSurface, errors);
                if (built != null)
                    startFinishList.Add(built);
            }

            if (errors.Count > 0)
                return new TrackLayoutParseResult(null, errors);
            var layout = new TrackLayout(
                graph,
                weather,
                ambience,
                defaultSurface,
                defaultNoise,
                defaultWidth,
                metadata,
                startFinishList);

            return new TrackLayoutParseResult(layout, errors);
        }
    }
}
