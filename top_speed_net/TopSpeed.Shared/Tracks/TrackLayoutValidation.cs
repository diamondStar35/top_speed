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
        public float WarningBankDegrees { get; set; } = 8f;
        public float WarningCrossSlopeDegrees { get; set; } = 5f;
        public float MaxCrossSlopeDegrees { get; set; } = 12f;
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

            ValidateRouteGeometry(layout, opts, issues);
            ValidateGraphEdges(layout, opts, issues);
            ValidateIntersections(layout, opts, issues);

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
                            $"Intersection '{node.Id}' connector '{connector.Id}' bank {connector.BankDegrees:0.##}° exceeds max {opts.MaxBankDegrees:0.##}°.",
                            section: "intersection"));
                    }
                    else if (connectorBankAbs > opts.WarningBankDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' bank {connector.BankDegrees:0.##}° exceeds warning {opts.WarningBankDegrees:0.##}°.",
                            section: "intersection"));
                    }

                    var connectorCrossAbs = Math.Abs(connector.CrossSlopeDegrees);
                    if (connectorCrossAbs > opts.MaxCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' connector '{connector.Id}' cross slope {connector.CrossSlopeDegrees:0.##}° exceeds max {opts.MaxCrossSlopeDegrees:0.##}°.",
                            section: "intersection"));
                    }
                    else if (connectorCrossAbs > opts.WarningCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' connector '{connector.Id}' cross slope {connector.CrossSlopeDegrees:0.##}° exceeds warning {opts.WarningCrossSlopeDegrees:0.##}°.",
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
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile bank {profilePoint.BankDegrees:0.##}° exceeds max {opts.MaxBankDegrees:0.##}°.",
                                    section: "intersection"));
                            }
                            else if (bankAbs > opts.WarningBankDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile bank {profilePoint.BankDegrees:0.##}° exceeds warning {opts.WarningBankDegrees:0.##}°.",
                                    section: "intersection"));
                            }

                            var crossAbs = Math.Abs(profilePoint.CrossSlopeDegrees);
                            if (crossAbs > opts.MaxCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}° exceeds max {opts.MaxCrossSlopeDegrees:0.##}°.",
                                    section: "intersection"));
                            }
                            else if (crossAbs > opts.WarningCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' connector '{connector.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}° exceeds warning {opts.WarningCrossSlopeDegrees:0.##}°.",
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
                            $"Intersection '{node.Id}' lane '{lane.Id}' bank {lane.BankDegrees:0.##}° exceeds max {opts.MaxBankDegrees:0.##}°.",
                            section: "intersection"));
                    }
                    else if (laneBankAbs > opts.WarningBankDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' bank {lane.BankDegrees:0.##}° exceeds warning {opts.WarningBankDegrees:0.##}°.",
                            section: "intersection"));
                    }

                    var laneCrossAbs = Math.Abs(lane.CrossSlopeDegrees);
                    if (laneCrossAbs > opts.MaxCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                            $"Intersection '{node.Id}' lane '{lane.Id}' cross slope {lane.CrossSlopeDegrees:0.##}° exceeds max {opts.MaxCrossSlopeDegrees:0.##}°.",
                            section: "intersection"));
                    }
                    else if (laneCrossAbs > opts.WarningCrossSlopeDegrees)
                    {
                        issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                            $"Intersection '{node.Id}' lane '{lane.Id}' cross slope {lane.CrossSlopeDegrees:0.##}° exceeds warning {opts.WarningCrossSlopeDegrees:0.##}°.",
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
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile bank {profilePoint.BankDegrees:0.##}° exceeds max {opts.MaxBankDegrees:0.##}°.",
                                    section: "intersection"));
                            }
                            else if (bankAbs > opts.WarningBankDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile bank {profilePoint.BankDegrees:0.##}° exceeds warning {opts.WarningBankDegrees:0.##}°.",
                                    section: "intersection"));
                            }

                            var crossAbs = Math.Abs(profilePoint.CrossSlopeDegrees);
                            if (crossAbs > opts.MaxCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Error,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}° exceeds max {opts.MaxCrossSlopeDegrees:0.##}°.",
                                    section: "intersection"));
                            }
                            else if (crossAbs > opts.WarningCrossSlopeDegrees)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' profile cross slope {profilePoint.CrossSlopeDegrees:0.##}° exceeds warning {opts.WarningCrossSlopeDegrees:0.##}°.",
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
                                    $"Intersection '{node.Id}' lane '{lane.Id}' entry heading differs from centerline by {delta:0.##}°.",
                                    section: "intersection"));
                            }
                        }
                        if (Math.Abs(lane.ExitHeadingDegrees) > 0.0001f)
                        {
                            var delta = AngleDeltaDegrees(lane.ExitHeadingDegrees, exitHeading);
                            if (delta > 30f)
                            {
                                issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                                    $"Intersection '{node.Id}' lane '{lane.Id}' exit heading differs from centerline by {delta:0.##}°.",
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
                var areaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        $"Bank {bank:0.##}° exceeds max {opts.MaxBankDegrees:0.##}°.",
                        i,
                        "geometry",
                        edgeId));
                }
                else if (bank > opts.WarningBankDegrees)
                {
                    issues.Add(new TrackLayoutIssue(TrackLayoutIssueSeverity.Warning,
                        $"Bank {bank:0.##}° exceeds warning {opts.WarningBankDegrees:0.##}°.",
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

            if (!opts.AllowZoneOverlap)
            {
                CheckOverlaps(profile.SurfaceZones, "surface", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.NoiseZones, "noise", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.WidthZones, "width", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
                CheckOverlaps(profile.SpeedLimitZones, "speed_limits", issues, edgeId, zone => (zone.StartMeters, zone.EndMeters));
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