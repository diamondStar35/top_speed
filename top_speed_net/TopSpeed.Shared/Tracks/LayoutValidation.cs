using System;
using System.Collections.Generic;
using System.Numerics;

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
        public string? EdgeId { get; }

        public TrackLayoutIssue(
            TrackLayoutIssueSeverity severity,
            string message,
            int? spanIndex = null,
            string? section = null,
            string? edgeId = null)
        {
            Severity = severity;
            Message = message;
            SpanIndex = spanIndex;
            Section = section;
            EdgeId = edgeId;
        }

        public override string ToString()
        {
            var location = string.Empty;
            if (!string.IsNullOrWhiteSpace(EdgeId))
                location = $"[edge {EdgeId}] ";
            if (!string.IsNullOrWhiteSpace(Section))
                location = $"[{Section}] " + location;
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
        public float WarningBankDegrees { get; set; } = 15f;
        public float WarningCrossSlopeDegrees { get; set; } = 15f;
        public float MaxCrossSlopeDegrees { get; set; } = 25f;
        public float MaxBankDegrees { get; set; } = 25f;
        public float WarningSlopePercent { get; set; } = 6f;
        public float MaxSlopePercent { get; set; } = 12f;
        public float WarningCurvatureJump { get; set; } = 0.005f;
        public float MaxCurvatureJump { get; set; } = 0.01f;
        public float MinWidthMeters { get; set; } = 6f;
        public float WarningWidthMeters { get; set; } = 8f;
        public float WarningClothoidRatioMin { get; set; } = 0.1f;
        public float WarningClothoidRatioMax { get; set; } = 3.0f;
        public float WarningClosureDistanceMeters { get; set; } = 2f;
        public float MaxClosureDistanceMeters { get; set; } = 5f;
        public float WarningClosureHeadingDegrees { get; set; } = 5f;
        public float MaxClosureHeadingDegrees { get; set; } = 15f;
        public float WarningTotalTurnDegrees { get; set; } = 5f;
        public float MaxTotalTurnDegrees { get; set; } = 15f;
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

            ValidateRouteGeometry(layout, opts, issues);
            ValidateGraphEdges(layout, opts, issues);
            ValidateIntersections(layout, opts, issues);
            ValidateStartFinishSubgraphs(layout, opts, issues);

            return new TrackLayoutValidationResult(issues);
        }

                private static void ValidateRouteGeometry(TrackLayout layout, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            var routeId = layout.PrimaryRoute != null ? layout.PrimaryRoute.Id : "primary";
            ValidateGeometry(layout.Geometry, opts, issues, $"route:{routeId}", layout.PrimaryRoute?.IsLoop ?? true);
        }

        private static void ValidateGraphEdges(TrackLayout layout, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            if (layout.Graph == null || layout.Graph.Edges.Count == 0)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                    "Graph edges are missing.",
                    section: "graph"));
                return;
            }

            for (var i = 0; i < layout.Graph.Edges.Count; i++)
            {
                var edge = layout.Graph.Edges[i];
                var checkClosure = edge.FromNodeId == edge.ToNodeId && edge.Geometry.EnforceClosure;
                ValidateGeometry(edge.Geometry, opts, issues, edge.Id, checkClosure);
                var defaultWidth = edge.Profile.DefaultWidthMeters;
                if (defaultWidth < opts.MinWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Default width {defaultWidth:0.###}m below minimum {opts.MinWidthMeters:0.###}m.",
                        section: "width",
                        edgeId: edge.Id));
                }
                else if (defaultWidth < opts.WarningWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Default width {defaultWidth:0.###}m below recommended {opts.WarningWidthMeters:0.###}m.",
                        section: "width",
                        edgeId: edge.Id));
                }
                ValidateZones(edge.Profile, edge.LengthMeters, opts, issues, edge.Id);
                ValidateMarkers(edge.Profile, edge.LengthMeters, issues, edge.Id);
            }
        }

        private static void ValidateIntersections(TrackLayout layout, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            if (layout.Graph == null || layout.Graph.Nodes.Count == 0)
                return;

            var edgeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var edge in layout.Graph.Edges)
                edgeIds.Add(edge.Id);

            foreach (var node in layout.Graph.Nodes)
            {
                var intersection = node.Intersection;
                if (intersection == null)
                    continue;

                if (intersection.Legs.Count == 0 && intersection.Connectors.Count > 0)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{node.Id}' defines connectors but no legs.",
                        section: "intersection"));
                }

                var legIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var legLookup = new Dictionary<string, TrackIntersectionLeg>(StringComparer.OrdinalIgnoreCase);
                var entryCount = 0;
                var exitCount = 0;
                foreach (var leg in intersection.Legs)
                {
                    if (!legIds.Add(leg.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' has duplicate leg id '{leg.Id}'.",
                            section: "intersection"));
                    }

                    legLookup[leg.Id] = leg;

                    if (!edgeIds.Contains(leg.EdgeId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' leg '{leg.Id}' references missing edge '{leg.EdgeId}'.",
                            section: "intersection"));
                    }

                    switch (leg.LegType)
                    {
                        case TrackIntersectionLegType.Entry:
                            entryCount++;
                            break;
                        case TrackIntersectionLegType.Exit:
                            exitCount++;
                            break;
                        case TrackIntersectionLegType.Both:
                            entryCount++;
                            exitCount++;
                            break;
                    }

                    if (leg.LaneCount > 0 && leg.WidthMeters <= 0f)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' leg '{leg.Id}' is missing width for {leg.LaneCount} lanes.",
                            section: "intersection"));
                    }

                    if (leg.LaneCount > 0 && leg.ApproachLengthMeters <= 0f)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' leg '{leg.Id}' is missing approach length.",
                            section: "intersection"));
                    }

                }
                if (intersection.Legs.Count > 0 && (entryCount == 0 || exitCount == 0))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{node.Id}' should define at least one entry and one exit leg.",
                        section: "intersection"));
                }

                var connectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var connectorLookup = new Dictionary<string, TrackIntersectionConnector>(StringComparer.OrdinalIgnoreCase);
                foreach (var connector in intersection.Connectors)
                {
                    if (!connectorIds.Add(connector.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' has duplicate connector id '{connector.Id}'.",
                            section: "intersection"));
                    }

                    connectorLookup[connector.Id] = connector;

                    if (intersection.Legs.Count > 0)
                    {
                        if (!legIds.Contains(connector.FromLegId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{node.Id}' connector '{connector.Id}' references missing from-leg '{connector.FromLegId}'.",
                                section: "intersection"));
                        }
                        if (!legIds.Contains(connector.ToLegId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{node.Id}' connector '{connector.Id}' references missing to-leg '{connector.ToLegId}'.",
                                section: "intersection"));
                        }
                    }

                    if (connector.FromLegId.Equals(connector.ToLegId, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' loops from and to the same leg.",
                            section: "intersection"));
                    }

                    if (connector.TurnDirection == TrackTurnDirection.Unknown)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' has no turn direction.",
                            section: "intersection"));
                    }

                    if (connector.PathPoints.Count > 0 && connector.PathPoints.Count < 2)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' has too few path points.",
                            section: "intersection"));
                    }

                    var connectorBankAbs = Math.Abs(connector.BankDegrees);
                    if (connectorBankAbs > opts.MaxBankDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' connector '{connector.Id}' bank {connector.BankDegrees:0.##}� exceeds max {opts.MaxBankDegrees:0.##}�.",
                            section: "intersection"));
                    }
                    else if (connectorBankAbs > opts.WarningBankDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' bank {connector.BankDegrees:0.##}� exceeds warning {opts.WarningBankDegrees:0.##}�.",
                            section: "intersection"));
                    }

                    var connectorCrossAbs = Math.Abs(connector.CrossSlopeDegrees);
                    if (connectorCrossAbs > opts.MaxCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' connector '{connector.Id}' cross slope {connector.CrossSlopeDegrees:0.##}� exceeds max {opts.MaxCrossSlopeDegrees:0.##}�.",
                            section: "intersection"));
                    }
                    else if (connectorCrossAbs > opts.WarningCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' cross slope {connector.CrossSlopeDegrees:0.##}� exceeds warning {opts.WarningCrossSlopeDegrees:0.##}�.",
                            section: "intersection"));
                    }

                    if (connector.Profile.Count > 0)
                    {
                        var lastS = -1f;
                        for (var i = 0; i < connector.Profile.Count; i++)
                        {
                            var profilePoint = connector.Profile[i];
                            if (profilePoint.SMeters < 0f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile point {i} has negative s.",
                                    section: "intersection"));
                            }
                            if (profilePoint.SMeters < lastS)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile points are not sorted by s.",
                                    section: "intersection"));
                                break;
                            }
                            lastS = profilePoint.SMeters;

                            var bankAbs = Math.Abs(profilePoint.BankDegrees);
                            if (bankAbs > opts.MaxBankDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile bank {profilePoint.BankDegrees:0.##}� exceeds max {opts.MaxBankDegrees:0.##}�.",
                                    section: "intersection"));
                            }
                            else if (bankAbs > opts.WarningBankDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile bank {profilePoint.BankDegrees:0.##}� exceeds warning {opts.WarningBankDegrees:0.##}�.",
                                    section: "intersection"));
                            }

                            var crossAbs = Math.Abs(profilePoint.CrossSlopeDegrees);
                            if (crossAbs > opts.MaxCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}� exceeds max {opts.MaxCrossSlopeDegrees:0.##}�.",
                                    section: "intersection"));
                            }
                            else if (crossAbs > opts.WarningCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}� exceeds warning {opts.WarningCrossSlopeDegrees:0.##}�.",
                                    section: "intersection"));
                            }
                        }
                    }

                    if (connector.PathPoints.Count > 0)
                    {
                        if (legLookup.TryGetValue(connector.FromLegId, out var fromLeg))
                        {
                            var startPoint = connector.PathPoints[0];
                            var legPoint = new TrackPoint3(fromLeg.OffsetXMeters, fromLeg.ElevationMeters, fromLeg.OffsetZMeters);
                            var dist = Distance(startPoint, legPoint);
                            if (dist > 2f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' start is {dist:0.##}m from from-leg '{fromLeg.Id}'.",
                                    section: "intersection"));
                            }
                        }
                        if (legLookup.TryGetValue(connector.ToLegId, out var toLeg))
                        {
                            var endPoint = connector.PathPoints[connector.PathPoints.Count - 1];
                            var legPoint = new TrackPoint3(toLeg.OffsetXMeters, toLeg.ElevationMeters, toLeg.OffsetZMeters);
                            var dist = Distance(endPoint, legPoint);
                            if (dist > 2f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' end is {dist:0.##}m from to-leg '{toLeg.Id}'.",
                                    section: "intersection"));
                            }
                        }
                    }

                    if (connector.Profile.Count > 0 && connector.PathPoints.Count >= 2)
                    {
                        var pathLength = ComputePolylineLength(connector.PathPoints);
                        var lastS = connector.Profile[connector.Profile.Count - 1].SMeters;
                        if (lastS > 0f)
                        {
                            var delta = Math.Abs(lastS - pathLength);
                            var tolerance = Math.Max(2f, pathLength * 0.1f);
                            if (delta > tolerance)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection ''{node.Id}'' connector ''{connector.Id}'' profile length {lastS:0.##}m differs from path length {pathLength:0.##}m.",
                                    section: "intersection"));
                            }
                        }
                    }
                }
                if (intersection.Lanes.Count > 0 && intersection.Legs.Count == 0 && intersection.Connectors.Count == 0)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{node.Id}' defines lanes but no legs or connectors.",
                        section: "intersection"));
                }

                if (intersection.LaneLinks.Count > 0 && intersection.Lanes.Count == 0)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{node.Id}' defines lane links but no lanes.",
                        section: "intersection"));
                }

                var laneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var lane in intersection.Lanes)
                {
                    if (!laneIds.Add(lane.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' has duplicate lane id '{lane.Id}'.",
                            section: "intersection"));
                    }

                    if (lane.OwnerKind == TrackLaneOwnerKind.Leg)
                    {
                        if (!legIds.Contains(lane.OwnerId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{node.Id}' lane '{lane.Id}' references missing leg '{lane.OwnerId}'.",
                                section: "intersection"));
                        }
                    }
                    else if (lane.OwnerKind == TrackLaneOwnerKind.Connector)
                    {
                        if (!connectorIds.Contains(lane.OwnerId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{node.Id}' lane '{lane.Id}' references missing connector '{lane.OwnerId}'.",
                                section: "intersection"));
                        }
                    }

                    if (lane.Index < 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane '{lane.Id}' has negative index.",
                            section: "intersection"));
                    }

                    if (lane.WidthMeters <= 0f)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane '{lane.Id}' has non-positive width.",
                            section: "intersection"));
                    }

                    if (lane.CenterlinePoints.Count > 0 && lane.CenterlinePoints.Count < 2)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' centerline needs at least 2 points.",
                            section: "intersection"));
                    }

                    if (lane.LeftEdgePoints.Count > 0 && lane.LeftEdgePoints.Count < 2)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' left edge needs at least 2 points.",
                            section: "intersection"));
                    }

                    if (lane.RightEdgePoints.Count > 0 && lane.RightEdgePoints.Count < 2)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' right edge needs at least 2 points.",
                            section: "intersection"));
                    }

                    if (lane.LeftEdgePoints.Count > 0 && lane.RightEdgePoints.Count > 0 &&
                        lane.LeftEdgePoints.Count != lane.RightEdgePoints.Count)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' left/right edge point counts differ.",
                            section: "intersection"));
                    }
                    var laneBankAbs = Math.Abs(lane.BankDegrees);
                    if (laneBankAbs > opts.MaxBankDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane '{lane.Id}' bank {lane.BankDegrees:0.##}� exceeds max {opts.MaxBankDegrees:0.##}�.",
                            section: "intersection"));
                    }
                    else if (laneBankAbs > opts.WarningBankDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' bank {lane.BankDegrees:0.##}� exceeds warning {opts.WarningBankDegrees:0.##}�.",
                            section: "intersection"));
                    }

                    var laneCrossAbs = Math.Abs(lane.CrossSlopeDegrees);
                    if (laneCrossAbs > opts.MaxCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane '{lane.Id}' cross slope {lane.CrossSlopeDegrees:0.##}� exceeds max {opts.MaxCrossSlopeDegrees:0.##}�.",
                            section: "intersection"));
                    }
                    else if (laneCrossAbs > opts.WarningCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' cross slope {lane.CrossSlopeDegrees:0.##}� exceeds warning {opts.WarningCrossSlopeDegrees:0.##}�.",
                            section: "intersection"));
                    }

                    if (lane.Profile.Count > 0)
                    {
                        var lastS = -1f;
                        for (var i = 0; i < lane.Profile.Count; i++)
                        {
                            var profilePoint = lane.Profile[i];
                            if (profilePoint.SMeters < 0f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile point {i} has negative s.",
                                    section: "intersection"));
                            }
                            if (profilePoint.SMeters < lastS)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile points are not sorted by s.",
                                    section: "intersection"));
                                break;
                            }
                            lastS = profilePoint.SMeters;

                            var bankAbs = Math.Abs(profilePoint.BankDegrees);
                            if (bankAbs > opts.MaxBankDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile bank {profilePoint.BankDegrees:0.##}� exceeds max {opts.MaxBankDegrees:0.##}�.",
                                    section: "intersection"));
                            }
                            else if (bankAbs > opts.WarningBankDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile bank {profilePoint.BankDegrees:0.##}� exceeds warning {opts.WarningBankDegrees:0.##}�.",
                                    section: "intersection"));
                            }

                            var crossAbs = Math.Abs(profilePoint.CrossSlopeDegrees);
                            if (crossAbs > opts.MaxCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}� exceeds max {opts.MaxCrossSlopeDegrees:0.##}�.",
                                    section: "intersection"));
                            }
                            else if (crossAbs > opts.WarningCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}� exceeds warning {opts.WarningCrossSlopeDegrees:0.##}�.",
                                    section: "intersection"));
                            }
                        }
                    }

                    if (lane.CenterlinePoints.Count >= 2)
                    {
                        var entryHeading = HeadingDegreesFromPoints(lane.CenterlinePoints[0], lane.CenterlinePoints[1]);
                        var exitHeading = HeadingDegreesFromPoints(lane.CenterlinePoints[lane.CenterlinePoints.Count - 2], lane.CenterlinePoints[lane.CenterlinePoints.Count - 1]);
                        if (Math.Abs(lane.EntryHeadingDegrees) > 0.0001f)
                        {
                            var delta = AngleDeltaDegrees(lane.EntryHeadingDegrees, entryHeading);
                            if (delta > 30f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' entry heading differs from centerline by {delta:0.##}�.",
                                    section: "intersection"));
                            }
                        }
                        if (Math.Abs(lane.ExitHeadingDegrees) > 0.0001f)
                        {
                            var delta = AngleDeltaDegrees(lane.ExitHeadingDegrees, exitHeading);
                            if (delta > 30f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' exit heading differs from centerline by {delta:0.##}�.",
                                    section: "intersection"));
                            }
                        }
                    }

                    if (lane.CenterlinePoints.Count > 0)
                    {
                        if (lane.OwnerKind == TrackLaneOwnerKind.Leg && legLookup.TryGetValue(lane.OwnerId, out var laneLeg))
                        {
                            var startPoint = lane.CenterlinePoints[0];
                            var legPoint = new TrackPoint3(laneLeg.OffsetXMeters, laneLeg.ElevationMeters, laneLeg.OffsetZMeters);
                            var dist = Distance(startPoint, legPoint);
                            if (dist > 2f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' start is {dist:0.##}m from leg '{laneLeg.Id}'.",
                                    section: "intersection"));
                            }
                        }
                        else if (lane.OwnerKind == TrackLaneOwnerKind.Connector && connectorLookup.TryGetValue(lane.OwnerId, out var laneConnector))
                        {
                            if (laneConnector.PathPoints.Count > 0)
                            {
                                var startPoint = lane.CenterlinePoints[0];
                                var connectorPoint = laneConnector.PathPoints[0];
                                var dist = Distance(startPoint, connectorPoint);
                                if (dist > 2f)
                                {
                                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                        $"Intersection '{node.Id}' lane '{lane.Id}' start is {dist:0.##}m from connector '{laneConnector.Id}'.",
                                        section: "intersection"));
                                }
                            }
                        }
                    }

                    if (lane.Profile.Count > 0 && lane.CenterlinePoints.Count >= 2)
                    {
                        var laneLength = ComputePolylineLength(lane.CenterlinePoints);
                        var lastS = lane.Profile[lane.Profile.Count - 1].SMeters;
                        if (lastS > 0f)
                        {
                            var delta = Math.Abs(lastS - laneLength);
                            var tolerance = Math.Max(2f, laneLength * 0.1f);
                            if (delta > tolerance)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile length {lastS:0.##}m differs from centerline length {laneLength:0.##}m.",
                                    section: "intersection"));
                            }
                        }
                    }

                }
                var laneLinkIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var link in intersection.LaneLinks)
                {
                    if (!laneLinkIds.Add(link.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' has duplicate lane link id '{link.Id}'.",
                            section: "intersection"));
                    }

                    if (!laneIds.Contains(link.FromLaneId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane link '{link.Id}' references missing from-lane '{link.FromLaneId}'.",
                            section: "intersection"));
                    }
                    if (!laneIds.Contains(link.ToLaneId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane link '{link.Id}' references missing to-lane '{link.ToLaneId}'.",
                            section: "intersection"));
                    }

                    if (link.FromLaneId.Equals(link.ToLaneId, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane link '{link.Id}' loops from and to the same lane.",
                            section: "intersection"));
                    }

                    if (link.TurnDirection == TrackTurnDirection.Unknown)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane link '{link.Id}' has no turn direction.",
                            section: "intersection"));
                    }

                    if (link.ChangeLengthMeters > 0f && !link.AllowsLaneChange)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane link '{link.Id}' sets change_length but lane_change is false.",
                            section: "intersection"));
                    }

                    if (link.AllowsLaneChange && link.ChangeLengthMeters <= 0f)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane link '{link.Id}' allows lane changes but has no change_length.",
                            section: "intersection"));
                    }

                }
                var laneGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in intersection.LaneGroups)
                    laneGroupIds.Add(group.Id);
                var areaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var hasStopLine = false;
                var hasConflict = false;
                var hasCore = false;
                var hasCrosswalk = false;
                foreach (var area in intersection.Areas)
                {
                    if (!areaIds.Add(area.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' has duplicate area id '{area.Id}'.",
                            section: "intersection"));
                    }

                    switch (area.Shape)
                    {
                        case TrackIntersectionAreaShape.Circle:
                            if (area.RadiusMeters <= 0f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' area '{area.Id}' circle radius must be > 0.",
                                    section: "intersection"));
                            }
                            break;
                        case TrackIntersectionAreaShape.Box:
                            if (area.WidthMeters <= 0f || area.LengthMeters <= 0f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' area '{area.Id}' box width/length must be > 0.",
                                    section: "intersection"));
                            }
                            break;
                        case TrackIntersectionAreaShape.Polygon:
                            if (area.Points.Count < 3)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' area '{area.Id}' polygon must define at least 3 points.",
                                    section: "intersection"));
                            }
                            break;
                    }

                    if (area.OwnerKind != TrackIntersectionAreaOwnerKind.None)
                    {
                        var ownerId = area.OwnerId;
                        if (string.IsNullOrWhiteSpace(ownerId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{node.Id}' area '{area.Id}' missing owner id for {area.OwnerKind}.",
                                section: "intersection"));
                        }
                        else
                        {
                            switch (area.OwnerKind)
                            {
                                case TrackIntersectionAreaOwnerKind.Leg:
                                    if (!legLookup.ContainsKey(ownerId!))
                                    {
                                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                            $"Intersection '{node.Id}' area '{area.Id}' references missing leg '{ownerId}'.",
                                            section: "intersection"));
                                    }
                                    break;
                                case TrackIntersectionAreaOwnerKind.Connector:
                                    if (!connectorLookup.ContainsKey(ownerId!))
                                    {
                                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                            $"Intersection '{node.Id}' area '{area.Id}' references missing connector '{ownerId}'.",
                                            section: "intersection"));
                                    }
                                    break;
                                case TrackIntersectionAreaOwnerKind.LaneGroup:
                                    if (!laneGroupIds.Contains(ownerId!))
                                    {
                                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                            $"Intersection '{node.Id}' area '{area.Id}' references missing lane group '{ownerId}'.",
                                            section: "intersection"));
                                    }
                                    break;
                            }
                        }
                    }

                    if (area.LaneIds.Count > 0)
                    {
                        foreach (var laneId in area.LaneIds)
                        {
                            if (!laneIds.Contains(laneId))
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' area '{area.Id}' references missing lane '{laneId}'.",
                                    section: "intersection"));
                            }
                        }
                    }

                    switch (area.Kind)
                    {
                        case TrackIntersectionAreaKind.StopLine:
                            hasStopLine = true;
                            if (area.OwnerKind == TrackIntersectionAreaOwnerKind.None && area.LaneIds.Count == 0)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' area '{area.Id}' stop line has no owner or lane_ids.",
                                    section: "intersection"));
                            }
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' area '{area.Id}' stop line should not be circular.",
                                    section: "intersection"));
                            }
                            break;
                        case TrackIntersectionAreaKind.StartLine:
                        case TrackIntersectionAreaKind.FinishLine:
                        case TrackIntersectionAreaKind.TimingGate:
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' area '{area.Id}' line/gate should not be circular.",
                                    section: "intersection"));
                            }
                            break;
                        case TrackIntersectionAreaKind.GridBox:
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' area '{area.Id}' grid box should not be circular.",
                                    section: "intersection"));
                            }
                            break;
                        case TrackIntersectionAreaKind.Conflict:
                            hasConflict = true;
                            if (area.OwnerKind == TrackIntersectionAreaOwnerKind.None && area.LaneIds.Count == 0)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' area '{area.Id}' conflict zone has no owner or lane_ids.",
                                    section: "intersection"));
                            }
                            break;
                        case TrackIntersectionAreaKind.Core:
                            hasCore = true;
                            break;
                        case TrackIntersectionAreaKind.Crosswalk:
                            hasCrosswalk = true;
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' area '{area.Id}' crosswalk should not be circular.",
                                    section: "intersection"));
                            }
                            break;
                    }
                }

                if (intersection.Control != TrackIntersectionControl.None && (entryCount > 0 || exitCount > 0))
                {
                    if (!hasStopLine)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' uses {intersection.Control} control but has no stop_line area.",
                            section: "intersection"));
                    }
                }

                if (intersection.Connectors.Count > 1 && !hasConflict)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{node.Id}' has multiple connectors but no conflict area.",
                        section: "intersection"));
                }

                if ((intersection.Shape == TrackIntersectionShape.Circle ||
                     intersection.Shape == TrackIntersectionShape.Roundabout) && !hasCore)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{node.Id}' uses {intersection.Shape} shape but has no core area.",
                        section: "intersection"));
                }

                if (hasCrosswalk && intersection.Control == TrackIntersectionControl.None)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{node.Id}' defines crosswalk areas but has no control.",
                        section: "intersection"));
                }

                ValidateLaneSubgraphs(intersection, node.Id, legLookup, connectorLookup, issues);
            }
        }

        private static void ValidateStartFinishSubgraphs(TrackLayout layout, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues)
        {
            if (layout.StartFinishSubgraphs == null || layout.StartFinishSubgraphs.Count == 0)
                return;

            var edgeLookup = new Dictionary<string, TrackGraphEdge>(StringComparer.OrdinalIgnoreCase);
            foreach (var edge in layout.Graph.Edges)
                edgeLookup[edge.Id] = edge;

            var subgraphIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var subgraph in layout.StartFinishSubgraphs)
            {
                if (!subgraphIds.Add(subgraph.Id))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Start/finish '{subgraph.Id}' has duplicate id.",
                        section: "start_finish"));
                    continue;
                }

                if (!edgeLookup.TryGetValue(subgraph.EdgeId, out var edge))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Start/finish '{subgraph.Id}' references missing edge '{subgraph.EdgeId}'.",
                        section: "start_finish"));
                }
                else
                {
                    if (subgraph.StartMeters < 0f || subgraph.EndMeters < 0f)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' uses negative start/end.",
                            section: "start_finish",
                            edgeId: edge.Id));
                    }
                    if (subgraph.EndMeters > edge.LengthMeters)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Start/finish '{subgraph.Id}' end {subgraph.EndMeters:0.###}m exceeds edge length {edge.LengthMeters:0.###}m.",
                            section: "start_finish",
                            edgeId: edge.Id));
                    }
                }

                if (subgraph.WidthMeters < opts.MinWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Start/finish '{subgraph.Id}' width {subgraph.WidthMeters:0.###}m below minimum {opts.MinWidthMeters:0.###}m.",
                        section: "start_finish"));
                }
                else if (subgraph.WidthMeters < opts.WarningWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Start/finish '{subgraph.Id}' width {subgraph.WidthMeters:0.###}m below recommended {opts.WarningWidthMeters:0.###}m.",
                        section: "start_finish"));
                }

                var laneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var lane in subgraph.Lanes)
                {
                    if (!laneIds.Add(lane.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' has duplicate lane id '{lane.Id}'.",
                            section: "start_finish"));
                    }

                    if (lane.OwnerKind != TrackLaneOwnerKind.StartFinish && lane.OwnerKind != TrackLaneOwnerKind.Custom)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Start/finish '{subgraph.Id}' lane '{lane.Id}' uses owner_kind {lane.OwnerKind}.",
                            section: "start_finish"));
                    }
                }

                var laneLinkIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var link in subgraph.LaneLinks)
                {
                    if (!laneLinkIds.Add(link.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' has duplicate lane link id '{link.Id}'.",
                            section: "start_finish"));
                    }

                    if (!laneIds.Contains(link.FromLaneId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' lane link '{link.Id}' references missing from-lane '{link.FromLaneId}'.",
                            section: "start_finish"));
                    }
                    if (!laneIds.Contains(link.ToLaneId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' lane link '{link.Id}' references missing to-lane '{link.ToLaneId}'.",
                            section: "start_finish"));
                    }
                }

                var laneGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in subgraph.LaneGroups)
                {
                    if (!laneGroupIds.Add(group.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' has duplicate lane group id '{group.Id}'.",
                            section: "start_finish"));
                    }

                    if (group.LaneCount <= 0 && group.LaneIds.Count == 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' lane group '{group.Id}' missing lane_count and lane_ids.",
                            section: "start_finish"));
                    }

                    if (group.LaneCount > 0 && group.LaneIds.Count > 0 && group.LaneCount != group.LaneIds.Count)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Start/finish '{subgraph.Id}' lane group '{group.Id}' lane_count {group.LaneCount} differs from lane_ids {group.LaneIds.Count}.",
                            section: "start_finish"));
                    }

                    foreach (var laneId in group.LaneIds)
                    {
                        if (!laneIds.Contains(laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Start/finish '{subgraph.Id}' lane group '{group.Id}' references missing lane '{laneId}'.",
                                section: "start_finish"));
                        }
                    }
                }

                var transitionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var transition in subgraph.LaneTransitions)
                {
                    if (!transitionIds.Add(transition.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' has duplicate lane transition id '{transition.Id}'.",
                            section: "start_finish"));
                    }

                    if (!laneGroupIds.Contains(transition.FromGroupId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' lane transition '{transition.Id}' references missing from-group '{transition.FromGroupId}'.",
                            section: "start_finish"));
                    }
                    if (!laneGroupIds.Contains(transition.ToGroupId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' lane transition '{transition.Id}' references missing to-group '{transition.ToGroupId}'.",
                            section: "start_finish"));
                    }

                    foreach (var laneId in transition.FromLaneIds)
                    {
                        if (!laneIds.Contains(laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Start/finish '{subgraph.Id}' lane transition '{transition.Id}' references missing from-lane '{laneId}'.",
                                section: "start_finish"));
                        }
                    }
                    foreach (var laneId in transition.ToLaneIds)
                    {
                        if (!laneIds.Contains(laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Start/finish '{subgraph.Id}' lane transition '{transition.Id}' references missing to-lane '{laneId}'.",
                                section: "start_finish"));
                        }
                    }
                }

                var areaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var hasStartLine = false;
                var hasFinishLine = false;
                foreach (var area in subgraph.Areas)
                {
                    if (!areaIds.Add(area.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Start/finish '{subgraph.Id}' has duplicate area id '{area.Id}'.",
                            section: "start_finish"));
                    }

                    switch (area.Shape)
                    {
                        case TrackIntersectionAreaShape.Circle:
                            if (area.RadiusMeters <= 0f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Start/finish '{subgraph.Id}' area '{area.Id}' circle radius must be > 0.",
                                    section: "start_finish"));
                            }
                            break;
                        case TrackIntersectionAreaShape.Box:
                            if (area.WidthMeters <= 0f || area.LengthMeters <= 0f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Start/finish '{subgraph.Id}' area '{area.Id}' box width/length must be > 0.",
                                    section: "start_finish"));
                            }
                            break;
                        case TrackIntersectionAreaShape.Polygon:
                            if (area.Points.Count < 3)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Start/finish '{subgraph.Id}' area '{area.Id}' polygon must define at least 3 points.",
                                    section: "start_finish"));
                            }
                            break;
                    }

                    if (area.OwnerKind == TrackIntersectionAreaOwnerKind.Leg || area.OwnerKind == TrackIntersectionAreaOwnerKind.Connector)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Start/finish '{subgraph.Id}' area '{area.Id}' uses owner_kind {area.OwnerKind}.",
                            section: "start_finish"));
                    }
                    if (area.OwnerKind == TrackIntersectionAreaOwnerKind.LaneGroup)
                    {
                        var ownerId = area.OwnerId;
                        if (!string.IsNullOrWhiteSpace(ownerId) && !laneGroupIds.Contains(ownerId!))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Start/finish '{subgraph.Id}' area '{area.Id}' references missing lane group '{ownerId}'.",
                                section: "start_finish"));
                        }
                    }
                    foreach (var laneId in area.LaneIds)
                    {
                        if (!laneIds.Contains(laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Start/finish '{subgraph.Id}' area '{area.Id}' references missing lane '{laneId}'.",
                                section: "start_finish"));
                        }
                    }

                    switch (area.Kind)
                    {
                        case TrackIntersectionAreaKind.StartLine:
                            hasStartLine = true;
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Start/finish '{subgraph.Id}' area '{area.Id}' start line should not be circular.",
                                    section: "start_finish"));
                            }
                            break;
                        case TrackIntersectionAreaKind.FinishLine:
                            hasFinishLine = true;
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Start/finish '{subgraph.Id}' area '{area.Id}' finish line should not be circular.",
                                    section: "start_finish"));
                            }
                            break;
                        case TrackIntersectionAreaKind.GridBox:
                        case TrackIntersectionAreaKind.TimingGate:
                            if (area.Shape == TrackIntersectionAreaShape.Circle)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Start/finish '{subgraph.Id}' area '{area.Id}' should not be circular.",
                                    section: "start_finish"));
                            }
                            break;
                    }
                }

                if (subgraph.Kind == TrackStartFinishKind.Start && !hasStartLine)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Start/finish '{subgraph.Id}' is start-only but has no start_line area.",
                        section: "start_finish"));
                }

                if (subgraph.Kind == TrackStartFinishKind.Finish && !hasFinishLine)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Start/finish '{subgraph.Id}' is finish-only but has no finish_line area.",
                        section: "start_finish"));
                }
            }
        }

        private static void ValidateLaneSubgraphs(
            TrackIntersectionProfile intersection,
            string nodeId,
            Dictionary<string, TrackIntersectionLeg> legLookup,
            Dictionary<string, TrackIntersectionConnector> connectorLookup,
            List<TrackLayoutIssue> issues)
        {
            var laneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var laneLookup = new Dictionary<string, TrackLane>(StringComparer.OrdinalIgnoreCase);
            foreach (var lane in intersection.Lanes)
            {
                laneIds.Add(lane.Id);
                laneLookup[lane.Id] = lane;
                if (lane.OwnerKind != TrackLaneOwnerKind.Leg && lane.OwnerKind != TrackLaneOwnerKind.Connector)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane '{lane.Id}' uses owner_kind {lane.OwnerKind}.",
                        section: "intersection"));
                }
            }

            if (intersection.Lanes.Count > 0 && intersection.LaneGroups.Count == 0)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Intersection '{nodeId}' defines lanes but no lane_groups for lane-aware routing.",
                    section: "intersection"));
            }

            var groupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groupLookup = new Dictionary<string, TrackLaneGroup>(StringComparer.OrdinalIgnoreCase);
            var laneGroupMembership = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in intersection.LaneGroups)
            {
                if (!groupIds.Add(group.Id))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{nodeId}' has duplicate lane group id '{group.Id}'.",
                        section: "intersection"));
                }
                else
                {
                    groupLookup[group.Id] = group;
                }

                if (group.Kind == TrackLaneGroupKind.Leg)
                {
                    var ownerId = group.OwnerId;
                    if (ownerId == null || ownerId.Trim().Length == 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{nodeId}' lane group '{group.Id}' references missing leg '{group.OwnerId}'.",
                            section: "intersection"));
                    }
                    else if (!legLookup.ContainsKey(ownerId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{nodeId}' lane group '{group.Id}' references missing leg '{group.OwnerId}'.",
                            section: "intersection"));
                    }
                    else
                    {
                        var leg = legLookup[ownerId];
                        if (leg.LaneCount > 0)
                        {
                            if (group.LaneIds.Count > 0 && group.LaneIds.Count != leg.LaneCount)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{nodeId}' lane group '{group.Id}' lane_ids count {group.LaneIds.Count} differs from leg '{leg.Id}' lane count {leg.LaneCount}.",
                                    section: "intersection"));
                            }
                            if (group.LaneCount > 0 && group.LaneCount != leg.LaneCount)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{nodeId}' lane group '{group.Id}' lane_count {group.LaneCount} differs from leg '{leg.Id}' lane count {leg.LaneCount}.",
                                    section: "intersection"));
                            }
                        }
                    }
                }
                else if (group.Kind == TrackLaneGroupKind.Connector)
                {
                    var ownerId = group.OwnerId;
                    if (ownerId == null || ownerId.Trim().Length == 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{nodeId}' lane group '{group.Id}' references missing connector '{group.OwnerId}'.",
                            section: "intersection"));
                    }
                    else if (!connectorLookup.ContainsKey(ownerId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{nodeId}' lane group '{group.Id}' references missing connector '{group.OwnerId}'.",
                            section: "intersection"));
                    }
                    else
                    {
                        var connector = connectorLookup[ownerId];
                        if (connector.LaneCount > 0)
                        {
                            if (group.LaneIds.Count > 0 && group.LaneIds.Count != connector.LaneCount)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{nodeId}' lane group '{group.Id}' lane_ids count {group.LaneIds.Count} differs from connector '{connector.Id}' lane count {connector.LaneCount}.",
                                    section: "intersection"));
                            }
                            if (group.LaneCount > 0 && group.LaneCount != connector.LaneCount)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{nodeId}' lane group '{group.Id}' lane_count {group.LaneCount} differs from connector '{connector.Id}' lane count {connector.LaneCount}.",
                                    section: "intersection"));
                            }
                        }
                    }
                }

                if (group.LaneIds.Count > 0 && group.LaneCount > 0 && group.LaneCount != group.LaneIds.Count)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane group '{group.Id}' lane_count {group.LaneCount} differs from lane_ids count {group.LaneIds.Count}.",
                        section: "intersection"));
                }

                foreach (var laneId in group.LaneIds)
                {
                    if (!laneIds.Contains(laneId))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{nodeId}' lane group '{group.Id}' references missing lane '{laneId}'.",
                            section: "intersection"));
                        continue;
                    }

                    laneGroupMembership.TryGetValue(laneId, out var count);
                    laneGroupMembership[laneId] = count + 1;

                    if (laneLookup.TryGetValue(laneId, out var lane))
                    {
                        if (group.Kind == TrackLaneGroupKind.Leg &&
                            (!string.Equals(lane.OwnerId, group.OwnerId, StringComparison.OrdinalIgnoreCase) ||
                             lane.OwnerKind != TrackLaneOwnerKind.Leg))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                $"Intersection '{nodeId}' lane '{laneId}' does not match leg group owner '{group.OwnerId}'.",
                                section: "intersection"));
                        }
                        else if (group.Kind == TrackLaneGroupKind.Connector &&
                                 (!string.Equals(lane.OwnerId, group.OwnerId, StringComparison.OrdinalIgnoreCase) ||
                                  lane.OwnerKind != TrackLaneOwnerKind.Connector))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                $"Intersection '{nodeId}' lane '{laneId}' does not match connector group owner '{group.OwnerId}'.",
                                section: "intersection"));
                        }
                    }
                }
            }

            if (intersection.LaneGroups.Count > 0)
            {
                foreach (var lane in laneLookup.Values)
                {
                    if (!laneGroupMembership.ContainsKey(lane.Id))
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{nodeId}' lane '{lane.Id}' is not assigned to any lane group.",
                            section: "intersection"));
                    }
                    else if (laneGroupMembership[lane.Id] > 1)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{nodeId}' lane '{lane.Id}' belongs to multiple lane groups.",
                            section: "intersection"));
                    }
                }
            }

            if (intersection.LaneGroups.Count > 0 && intersection.LaneTransitions.Count == 0)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Intersection '{nodeId}' defines lane groups but no lane transitions.",
                    section: "intersection"));
            }

            var transitionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var incoming = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var outgoing = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var groupId in groupIds)
            {
                incoming[groupId] = 0;
                outgoing[groupId] = 0;
            }
            foreach (var transition in intersection.LaneTransitions)
            {
                if (!transitionIds.Add(transition.Id))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{nodeId}' has duplicate lane transition id '{transition.Id}'.",
                        section: "intersection"));
                }

                if (!groupIds.Contains(transition.FromGroupId))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' references missing from-group '{transition.FromGroupId}'.",
                        section: "intersection"));
                }
                if (!groupIds.Contains(transition.ToGroupId))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' references missing to-group '{transition.ToGroupId}'.",
                        section: "intersection"));
                }
                if (groupIds.Contains(transition.FromGroupId))
                    outgoing[transition.FromGroupId] = outgoing[transition.FromGroupId] + 1;
                if (groupIds.Contains(transition.ToGroupId))
                    incoming[transition.ToGroupId] = incoming[transition.ToGroupId] + 1;

                if (transition.FromGroupId.Equals(transition.ToGroupId, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' loops from and to the same group.",
                        section: "intersection"));
                }

                if (transition.TurnDirection == TrackTurnDirection.Unknown)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' has no turn direction.",
                        section: "intersection"));
                }

                if (transition.ChangeLengthMeters > 0f && !transition.AllowsLaneChange)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' sets change_length but lane_change is false.",
                        section: "intersection"));
                }

                if (transition.AllowsLaneChange && transition.ChangeLengthMeters <= 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' allows lane changes but has no change_length.",
                        section: "intersection"));
                }

                if (transition.FromLaneIds.Count > 0)
                {
                    foreach (var laneId in transition.FromLaneIds)
                    {
                        if (!laneIds.Contains(laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{nodeId}' lane transition '{transition.Id}' references missing from-lane '{laneId}'.",
                                section: "intersection"));
                            continue;
                        }

                        if (groupLookup.TryGetValue(transition.FromGroupId, out var group) &&
                            group.LaneIds.Count > 0 &&
                            !ContainsIgnoreCase(group.LaneIds, laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{nodeId}' lane transition '{transition.Id}' from-lane '{laneId}' is not in group '{group.Id}'.",
                                section: "intersection"));
                        }
                    }
                }

                if (transition.ToLaneIds.Count > 0)
                {
                    foreach (var laneId in transition.ToLaneIds)
                    {
                        if (!laneIds.Contains(laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{nodeId}' lane transition '{transition.Id}' references missing to-lane '{laneId}'.",
                                section: "intersection"));
                            continue;
                        }

                        if (groupLookup.TryGetValue(transition.ToGroupId, out var group) &&
                            group.LaneIds.Count > 0 &&
                            !ContainsIgnoreCase(group.LaneIds, laneId))
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                $"Intersection '{nodeId}' lane transition '{transition.Id}' to-lane '{laneId}' is not in group '{group.Id}'.",
                                section: "intersection"));
                        }
                    }
                }

                if (transition.FromLaneIds.Count > 0 && transition.ToLaneIds.Count > 0 &&
                    transition.FromLaneIds.Count != transition.ToLaneIds.Count)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Intersection '{nodeId}' lane transition '{transition.Id}' has mismatched from/to lane counts.",
                        section: "intersection"));
                }
            }

            foreach (var group in intersection.LaneGroups)
            {
                incoming.TryGetValue(group.Id, out var inCount);
                outgoing.TryGetValue(group.Id, out var outCount);

                if (group.Kind == TrackLaneGroupKind.Leg)
                {
                    if (group.OwnerId != null && legLookup.TryGetValue(group.OwnerId, out var leg))
                    {
                        switch (leg.LegType)
                        {
                            case TrackIntersectionLegType.Entry:
                                if (outCount == 0)
                                {
                                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                        $"Intersection '{nodeId}' lane group '{group.Id}' is entry-only but has no outgoing transitions.",
                                        section: "intersection"));
                                }
                                break;
                            case TrackIntersectionLegType.Exit:
                                if (inCount == 0)
                                {
                                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                        $"Intersection '{nodeId}' lane group '{group.Id}' is exit-only but has no incoming transitions.",
                                        section: "intersection"));
                                }
                                break;
                            case TrackIntersectionLegType.Both:
                                if (outCount == 0)
                                {
                                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                        $"Intersection '{nodeId}' lane group '{group.Id}' has no outgoing transitions.",
                                        section: "intersection"));
                                }
                                if (inCount == 0)
                                {
                                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                        $"Intersection '{nodeId}' lane group '{group.Id}' has no incoming transitions.",
                                        section: "intersection"));
                                }
                                break;
                        }
                    }
                }
                else if (group.Kind == TrackLaneGroupKind.Connector)
                {
                    if (inCount == 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{nodeId}' lane group '{group.Id}' has no incoming transitions.",
                            section: "intersection"));
                    }
                    if (outCount == 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{nodeId}' lane group '{group.Id}' has no outgoing transitions.",
                            section: "intersection"));
                    }
                }
                else if (group.Kind == TrackLaneGroupKind.Custom)
                {
                    if (inCount == 0 && outCount == 0)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{nodeId}' lane group '{group.Id}' is isolated (no incoming/outgoing transitions).",
                            section: "intersection"));
                    }
                }
            }
        }
        private static void ValidateGeometry(
            TrackGeometrySpec geometry,
            TrackLayoutValidationOptions opts,
            List<TrackLayoutIssue> issues,
            string? edgeId,
            bool checkClosure)
        {
            var spans = geometry.Spans;
            if (spans.Count == 0)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                    "Geometry spans are missing.",
                    section: "geometry",
                    edgeId: edgeId));
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
                        "geometry",
                        edgeId));
                }

                var maxSlope = Math.Max(Math.Abs(span.StartSlope), Math.Abs(span.EndSlope));
                var slopePercent = maxSlope * 100f;
                if (slopePercent > opts.MaxSlopePercent)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Slope {slopePercent:0.##}% exceeds max {opts.MaxSlopePercent:0.##}%.",
                        i,
                        "geometry",
                        edgeId));
                }
                else if (slopePercent > opts.WarningSlopePercent)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Slope {slopePercent:0.##}% exceeds warning {opts.WarningSlopePercent:0.##}%.",
                        i,
                        "geometry",
                        edgeId));
                }

                var bank = Math.Max(Math.Abs(span.BankStartDegrees), Math.Abs(span.BankEndDegrees));
                if (bank > opts.MaxBankDegrees)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Bank {bank:0.##}� exceeds max {opts.MaxBankDegrees:0.##}�.",
                        i,
                        "geometry",
                        edgeId));
                }
                else if (bank > opts.WarningBankDegrees)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Bank {bank:0.##}� exceeds warning {opts.WarningBankDegrees:0.##}�.",
                        i,
                        "geometry",
                        edgeId));
                }

                switch (span.Kind)
                {
                    case TrackGeometrySpanKind.Arc:
                        ValidateRadius(span.RadiusMeters, opts, i, issues, edgeId);
                        minRadius = Math.Min(minRadius, span.RadiusMeters);
                        break;
                    case TrackGeometrySpanKind.Clothoid:
                        if (span.StartRadiusMeters > 0f)
                        {
                            ValidateRadius(span.StartRadiusMeters, opts, i, issues, edgeId);
                            minRadius = Math.Min(minRadius, span.StartRadiusMeters);
                        }
                        if (span.EndRadiusMeters > 0f)
                        {
                            ValidateRadius(span.EndRadiusMeters, opts, i, issues, edgeId);
                            minRadius = Math.Min(minRadius, span.EndRadiusMeters);
                        }
                        ValidateClothoidLength(span, opts, i, issues, edgeId);
                        break;
                }

                if (span.Kind != TrackGeometrySpanKind.Straight &&
                    span.CurveSeverity == null)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Curved span has no curve severity for announcements.",
                        i,
                        "geometry",
                        edgeId));
                }
            }

            if (totalLength < 200f)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Total length {totalLength:0.###}m is very short.",
                    section: "geometry",
                    edgeId: edgeId));
            }

            var curvatureChecks = checkClosure ? spans.Count : spans.Count - 1;
            for (var i = 0; i < curvatureChecks; i++)
            {
                var current = spans[i];
                var next = spans[(i + 1) % spans.Count];
                var delta = Math.Abs(current.EndCurvature - next.StartCurvature);
                if (delta > opts.MaxCurvatureJump)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Curvature jump {delta:0.#####} 1/m exceeds max {opts.MaxCurvatureJump:0.#####}.",
                        i,
                        "geometry",
                        edgeId));
                }
                else if (delta > opts.WarningCurvatureJump)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Curvature jump {delta:0.#####} 1/m exceeds warning {opts.WarningCurvatureJump:0.#####}.",
                        i,
                        "geometry",
                        edgeId));
                }
            }

            if (checkClosure)
            {
                var strictClosure = !geometry.EnforceClosure;
                var totalTurnRadians = ComputeTotalTurnRadians(spans);
                var turnDeltaRadians = NormalizeTurnDelta(totalTurnRadians);
                var turnDeltaDegrees = RadiansToDegrees(turnDeltaRadians);
                if (turnDeltaDegrees > opts.MaxTotalTurnDegrees)
                {
                    issues.Add(new TrackLayoutIssue(strictClosure ? TrackLayoutIssueSeverity.Error : TrackLayoutIssueSeverity.Warning,
                        $"Total turn delta {turnDeltaDegrees:0.###}° exceeds max {opts.MaxTotalTurnDegrees:0.###}°.",
                        section: "geometry",
                        edgeId: edgeId));
                }
                else if (turnDeltaDegrees > opts.WarningTotalTurnDegrees)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Total turn delta {turnDeltaDegrees:0.###}° exceeds warning {opts.WarningTotalTurnDegrees:0.###}°.",
                        section: "geometry",
                        edgeId: edgeId));
                }

                var spacing = geometry.SampleSpacingMeters;
                if (spacing > 0f)
                {
                    try
                    {
                        var rawSpec = new TrackGeometrySpec(spans, spacing, enforceClosure: false);
                        var rawGeometry = TrackGeometry.Build(rawSpec);
                        var startPose = rawGeometry.GetPose(0f);
                        var endPose = rawGeometry.GetPose(rawGeometry.LengthMeters);
                        var closureDistance = Vector3.Distance(startPose.Position, endPose.Position);
                        var headingDelta = Math.Abs(NormalizeAngle(endPose.HeadingRadians - startPose.HeadingRadians));
                        var headingDeltaDegrees = RadiansToDegrees(headingDelta);

                        if (closureDistance > opts.MaxClosureDistanceMeters)
                        {
                            issues.Add(new TrackLayoutIssue(strictClosure ? TrackLayoutIssueSeverity.Error : TrackLayoutIssueSeverity.Warning,
                                $"Closure distance {closureDistance:0.###}m exceeds max {opts.MaxClosureDistanceMeters:0.###}m.",
                                section: "geometry",
                                edgeId: edgeId));
                        }
                        else if (closureDistance > opts.WarningClosureDistanceMeters)
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                $"Closure distance {closureDistance:0.###}m exceeds warning {opts.WarningClosureDistanceMeters:0.###}m.",
                                section: "geometry",
                                edgeId: edgeId));
                        }

                        if (headingDeltaDegrees > opts.MaxClosureHeadingDegrees)
                        {
                            issues.Add(new TrackLayoutIssue(strictClosure ? TrackLayoutIssueSeverity.Error : TrackLayoutIssueSeverity.Warning,
                                $"Closure heading delta {headingDeltaDegrees:0.###}° exceeds max {opts.MaxClosureHeadingDegrees:0.###}°.",
                                section: "geometry",
                                edgeId: edgeId));
                        }
                        else if (headingDeltaDegrees > opts.WarningClosureHeadingDegrees)
                        {
                            issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                $"Closure heading delta {headingDeltaDegrees:0.###}° exceeds warning {opts.WarningClosureHeadingDegrees:0.###}°.",
                                section: "geometry",
                                edgeId: edgeId));
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Closure validation failed to build geometry: {ex.Message}",
                            section: "geometry",
                            edgeId: edgeId));
                    }
                }
            }

            if (geometry.SampleSpacingMeters <= 0f)
                return;

            if (minRadius < float.MaxValue)
            {
                var spacing = geometry.SampleSpacingMeters;
                if (spacing > minRadius / 2f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Sample spacing {spacing:0.###}m is too coarse for min radius {minRadius:0.###}m.",
                        section: "environment",
                        edgeId: edgeId));
                }
                else if (spacing > minRadius / 4f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Sample spacing {spacing:0.###}m may be too coarse for min radius {minRadius:0.###}m.",
                        section: "environment",
                        edgeId: edgeId));
                }
            }
        }                private static void ValidateRadius(
            float radius,
            TrackLayoutValidationOptions opts,
            int spanIndex,
            List<TrackLayoutIssue> issues,
            string? edgeId)
        {
            if (radius < opts.MinRadiusMeters)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                    $"Radius {radius:0.###}m below minimum {opts.MinRadiusMeters:0.###}m.",
                    spanIndex,
                    "geometry",
                    edgeId));
            }
            else if (radius > opts.MaxRadiusMeters)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Radius {radius:0.###}m above recommended max {opts.MaxRadiusMeters:0.###}m.",
                    spanIndex,
                    "geometry",
                    edgeId));
            }
        }

        private static void ValidateClothoidLength(
            TrackGeometrySpan span,
            TrackLayoutValidationOptions opts,
            int spanIndex,
            List<TrackLayoutIssue> issues,
            string? edgeId)
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
                    "geometry",
                    edgeId));
            }
            else if (ratio > opts.WarningClothoidRatioMax)
            {
                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                    $"Clothoid length ratio {ratio:0.###} (length/radius) is very long.",
                    spanIndex,
                    "geometry",
                    edgeId));
            }
        }

        private static void ValidateZones(TrackEdgeProfile profile, float length, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues, string? edgeId)
        {
            ValidateZoneRange(profile.SurfaceZones, length, "surface", issues, edgeId);
            ValidateZoneRange(profile.NoiseZones, length, "noise", issues, edgeId);
            ValidateWidthZones(profile.WidthZones, length, opts, issues, edgeId);
            ValidateSpeedZones(profile.SpeedLimitZones, length, issues, edgeId);

            // Validate additional zone types
            ValidateRangeZones(profile.WeatherZones, length, "weather", issues, edgeId, z => (z.StartMeters, z.EndMeters));
            ValidateRangeZones(profile.AmbienceZones, length, "ambience", issues, edgeId, z => (z.StartMeters, z.EndMeters));
            ValidateRangeZones(profile.Hazards, length, "hazards", issues, edgeId, z => (z.StartMeters, z.EndMeters));
            ValidateRangeZones(profile.HitLanes, length, "hit_lanes", issues, edgeId, z => (z.StartMeters, z.EndMeters));
            ValidateRangeZones(profile.Triggers, length, "triggers", issues, edgeId, z => (z.StartMeters, z.EndMeters));
            ValidateBoundaryZones(profile.BoundaryZones, length, issues, edgeId);

            // Validate point-based elements
            ValidatePositionalElements(profile.Checkpoints, length, "checkpoints", issues, edgeId, c => c.PositionMeters);
            ValidatePositionalElements(profile.Emitters, length, "emitters", issues, edgeId, e => e.PositionMeters);

            if (!opts.AllowZoneOverlap)
            {
                CheckOverlaps(profile.SurfaceZones, "surface", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.NoiseZones, "noise", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.WidthZones, "width", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.SpeedLimitZones, "speed_limits", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.WeatherZones, "weather", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.AmbienceZones, "ambience", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
            }
        }

        private static void ValidateRangeZones<T>(IReadOnlyList<T> zones, float length, string section, List<TrackLayoutIssue> issues, string? edgeId, Func<T, (float start, float end)> getRange)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var range = getRange(zones[i]);
                if (range.start < 0f || range.end < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"{section} zone has negative start/end.",
                        i,
                        section,
                        edgeId));
                }
                if (range.end > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"{section} zone extends beyond edge length.",
                        i,
                        section,
                        edgeId));
                }
            }
        }

        private static void ValidateBoundaryZones(IReadOnlyList<TrackBoundaryZone> zones, float length, List<TrackLayoutIssue> issues, string? edgeId)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Boundary zone has negative start/end.",
                        i,
                        "boundaries",
                        edgeId));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Boundary zone extends beyond edge length.",
                        i,
                        "boundaries",
                        edgeId));
                }
            }
        }

        private static float ComputeTotalTurnRadians(IReadOnlyList<TrackGeometrySpan> spans)
        {
            var total = 0f;
            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                var avgCurvature = (span.StartCurvature + span.EndCurvature) * 0.5f;
                total += avgCurvature * span.LengthMeters;
            }
            return total;
        }

        private static float NormalizeTurnDelta(float totalTurnRadians)
        {
            var twoPi = (float)(Math.PI * 2.0);
            if (twoPi <= 0f)
                return Math.Abs(totalTurnRadians);

            var turns = totalTurnRadians / twoPi;
            var nearest = (float)Math.Round(turns) * twoPi;
            return Math.Abs(totalTurnRadians - nearest);
        }

        private static float NormalizeAngle(float angle)
        {
            var twoPi = (float)(Math.PI * 2.0);
            while (angle > Math.PI)
                angle -= twoPi;
            while (angle <= -Math.PI)
                angle += twoPi;
            return angle;
        }

        private static float RadiansToDegrees(float radians)
        {
            return (float)(radians * (180.0 / Math.PI));
        }

        private static void ValidatePositionalElements<T>(IReadOnlyList<T> elements, float length, string section, List<TrackLayoutIssue> issues, string? edgeId, Func<T, float> getPosition)
        {
            for (var i = 0; i < elements.Count; i++)
            {
                var position = getPosition(elements[i]);
                if (position < 0f || position > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"{section} element is outside edge bounds.",
                        i,
                        section,
                        edgeId));
                }
            }
        }

        private static void ValidateZoneRange<T>(IReadOnlyList<TrackZone<T>> zones, float length, string section, List<TrackLayoutIssue> issues, string? edgeId)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Zone has negative start/end.",
                        i,
                        section,
                        edgeId));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Zone extends beyond track length.",
                        i,
                        section,
                        edgeId));
                }
            }
        }

        private static void ValidateWidthZones(IReadOnlyList<TrackWidthZone> zones, float length, TrackLayoutValidationOptions opts, List<TrackLayoutIssue> issues, string? edgeId)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Width zone has negative start/end.",
                        i,
                        "width",
                        edgeId));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Width zone extends beyond track length.",
                        i,
                        "width",
                        edgeId));
                }
                if (zone.WidthMeters < opts.MinWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Width {zone.WidthMeters:0.###}m below minimum {opts.MinWidthMeters:0.###}m.",
                        i,
                        "width",
                        edgeId));
                }
                else if (zone.WidthMeters < opts.WarningWidthMeters)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Width {zone.WidthMeters:0.###}m below recommended {opts.WarningWidthMeters:0.###}m.",
                        i,
                        "width",
                        edgeId));
                }
            }
        }

        private static void ValidateSpeedZones(IReadOnlyList<TrackSpeedLimitZone> zones, float length, List<TrackLayoutIssue> issues, string? edgeId)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.StartMeters < 0f || zone.EndMeters < 0f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        "Speed limit zone has negative start/end.",
                        i,
                        "speed_limits",
                        edgeId));
                }
                if (zone.EndMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        "Speed limit zone extends beyond track length.",
                        i,
                        "speed_limits",
                        edgeId));
                }
                if (zone.MaxSpeedKph < 20f)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Speed limit {zone.MaxSpeedKph:0.###} kph is very low.",
                        i,
                        "speed_limits",
                        edgeId));
                }
            }
        }

        private static void ValidateMarkers(TrackEdgeProfile profile, float length, List<TrackLayoutIssue> issues, string? edgeId)
        {
            var markers = profile.Markers;
            for (var i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                if (marker.PositionMeters < 0f || marker.PositionMeters > length)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                        $"Marker '{marker.Name}' is outside track bounds.",
                        i,
                        "markers",
                        edgeId));
                }
            }
        }

        private static float ComputePolylineLength(IReadOnlyList<TrackPoint3> points)
        {
            if (points == null || points.Count < 2)
                return 0f;
            var length = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                length += Distance(points[i - 1], points[i]);
            }
            return length;
        }

        private static bool ContainsIgnoreCase(IReadOnlyList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static float Distance(TrackPoint3 a, TrackPoint3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static float HeadingDegreesFromPoints(TrackPoint3 a, TrackPoint3 b)
        {
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            if (Math.Abs(dx) < 0.0001f && Math.Abs(dz) < 0.0001f)
                return 0f;
            var radians = Math.Atan2(dx, dz);
            var degrees = radians * (180.0 / Math.PI);
            return NormalizeDegrees((float)degrees);
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }

        private static float AngleDeltaDegrees(float a, float b)
        {
            var delta = NormalizeDegrees(a - b);
            if (delta > 180f)
                delta -= 360f;
            return Math.Abs(delta);
        }
private static void CheckOverlaps<T>(IReadOnlyList<T> zones, string section, List<TrackLayoutIssue> issues, string? edgeId, Func<T, (float start, float end)> getRange)
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
                        section,
                        edgeId));
                }
            }
        }
    }
}
