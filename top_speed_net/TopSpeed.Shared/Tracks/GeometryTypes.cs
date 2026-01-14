using System;
using System.Collections.Generic;
using TopSpeed.Data;

namespace TopSpeed.Tracks.Geometry
{
    public readonly struct TrackGeometrySpan
    {
        public TrackGeometrySpanKind Kind { get; }
        public float LengthMeters { get; }
        public TrackCurveDirection Direction { get; }
        public float RadiusMeters { get; }
        public float StartRadiusMeters { get; }
        public float EndRadiusMeters { get; }
        public float ElevationDeltaMeters { get; }
        public float StartSlope { get; }
        public float EndSlope { get; }
        public float BankStartDegrees { get; }
        public float BankEndDegrees { get; }
        public float BankDegrees => (BankStartDegrees + BankEndDegrees) * 0.5f;
        public TrackCurveSeverity? CurveSeverity { get; }

        private TrackGeometrySpan(
            TrackGeometrySpanKind kind,
            float lengthMeters,
            TrackCurveDirection direction,
            float radiusMeters,
            float startRadiusMeters,
            float endRadiusMeters,
            float elevationDeltaMeters,
            float startSlope,
            float endSlope,
            float bankStartDegrees,
            float bankEndDegrees,
            TrackCurveSeverity? curveSeverity)
        {
            if (!IsFinite(lengthMeters) || lengthMeters <= 0f)
                throw new ArgumentOutOfRangeException(nameof(lengthMeters), "Span length must be positive.");

            if (!IsFinite(elevationDeltaMeters))
                throw new ArgumentOutOfRangeException(nameof(elevationDeltaMeters));

            if (!IsFinite(startSlope))
                throw new ArgumentOutOfRangeException(nameof(startSlope));
            if (!IsFinite(endSlope))
                throw new ArgumentOutOfRangeException(nameof(endSlope));

            if (!IsFinite(bankStartDegrees))
                throw new ArgumentOutOfRangeException(nameof(bankStartDegrees));
            if (!IsFinite(bankEndDegrees))
                throw new ArgumentOutOfRangeException(nameof(bankEndDegrees));

            switch (kind)
            {
                case TrackGeometrySpanKind.Straight:
                    if (direction != TrackCurveDirection.Straight)
                        throw new ArgumentException("Straight spans must use Straight direction.", nameof(direction));
                    radiusMeters = 0f;
                    startRadiusMeters = 0f;
                    endRadiusMeters = 0f;
                    curveSeverity = null;
                    break;
                case TrackGeometrySpanKind.Arc:
                    if (direction == TrackCurveDirection.Straight)
                        throw new ArgumentException("Arc spans require Left or Right direction.", nameof(direction));
                    if (!IsFinite(radiusMeters) || radiusMeters <= 0f)
                        throw new ArgumentOutOfRangeException(nameof(radiusMeters), "Arc radius must be positive.");
                    startRadiusMeters = 0f;
                    endRadiusMeters = 0f;
                    break;
                case TrackGeometrySpanKind.Clothoid:
                    if (!IsFinite(startRadiusMeters) || startRadiusMeters < 0f)
                        throw new ArgumentOutOfRangeException(nameof(startRadiusMeters));
                    if (!IsFinite(endRadiusMeters) || endRadiusMeters < 0f)
                        throw new ArgumentOutOfRangeException(nameof(endRadiusMeters));
                    if (startRadiusMeters == 0f && endRadiusMeters == 0f)
                    {
                        if (direction != TrackCurveDirection.Straight)
                            throw new ArgumentException("Clothoid with zero radii must be Straight.", nameof(direction));
                        curveSeverity = null;
                    }
                    else if (direction == TrackCurveDirection.Straight)
                    {
                        throw new ArgumentException("Clothoid spans with curvature require Left or Right direction.", nameof(direction));
                    }
                    radiusMeters = 0f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown span type.");
            }

            Kind = kind;
            LengthMeters = lengthMeters;
            Direction = direction;
            RadiusMeters = radiusMeters;
            StartRadiusMeters = startRadiusMeters;
            EndRadiusMeters = endRadiusMeters;
            ElevationDeltaMeters = elevationDeltaMeters;
            StartSlope = startSlope;
            EndSlope = endSlope;
            BankStartDegrees = bankStartDegrees;
            BankEndDegrees = bankEndDegrees;
            CurveSeverity = curveSeverity;
        }

        public static TrackGeometrySpan Straight(
            float lengthMeters,
            float elevationDeltaMeters = 0f,
            float bankDegrees = 0f)
        {
            var slope = lengthMeters > 0f ? elevationDeltaMeters / lengthMeters : 0f;
            return new TrackGeometrySpan(
                TrackGeometrySpanKind.Straight,
                lengthMeters,
                TrackCurveDirection.Straight,
                0f,
                0f,
                0f,
                elevationDeltaMeters,
                slope,
                slope,
                bankDegrees,
                bankDegrees,
                null);
        }

        public static TrackGeometrySpan Arc(
            float lengthMeters,
            float radiusMeters,
            TrackCurveDirection direction,
            TrackCurveSeverity? curveSeverity = null,
            float elevationDeltaMeters = 0f,
            float bankDegrees = 0f)
        {
            var slope = lengthMeters > 0f ? elevationDeltaMeters / lengthMeters : 0f;
            return new TrackGeometrySpan(
                TrackGeometrySpanKind.Arc,
                lengthMeters,
                direction,
                radiusMeters,
                0f,
                0f,
                elevationDeltaMeters,
                slope,
                slope,
                bankDegrees,
                bankDegrees,
                curveSeverity);
        }

        public static TrackGeometrySpan Clothoid(
            float lengthMeters,
            float startRadiusMeters,
            float endRadiusMeters,
            TrackCurveDirection direction,
            TrackCurveSeverity? curveSeverity = null,
            float elevationDeltaMeters = 0f,
            float bankDegrees = 0f)
        {
            var slope = lengthMeters > 0f ? elevationDeltaMeters / lengthMeters : 0f;
            return new TrackGeometrySpan(
                TrackGeometrySpanKind.Clothoid,
                lengthMeters,
                direction,
                0f,
                startRadiusMeters,
                endRadiusMeters,
                elevationDeltaMeters,
                slope,
                slope,
                bankDegrees,
                bankDegrees,
                curveSeverity);
        }

        public static TrackGeometrySpan StraightWithProfile(
            float lengthMeters,
            float elevationDeltaMeters,
            float startSlope,
            float endSlope,
            float bankStartDegrees,
            float bankEndDegrees)
        {
            return new TrackGeometrySpan(
                TrackGeometrySpanKind.Straight,
                lengthMeters,
                TrackCurveDirection.Straight,
                0f,
                0f,
                0f,
                elevationDeltaMeters,
                startSlope,
                endSlope,
                bankStartDegrees,
                bankEndDegrees,
                null);
        }

        public static TrackGeometrySpan ArcWithProfile(
            float lengthMeters,
            float radiusMeters,
            TrackCurveDirection direction,
            TrackCurveSeverity? curveSeverity,
            float elevationDeltaMeters,
            float startSlope,
            float endSlope,
            float bankStartDegrees,
            float bankEndDegrees)
        {
            return new TrackGeometrySpan(
                TrackGeometrySpanKind.Arc,
                lengthMeters,
                direction,
                radiusMeters,
                0f,
                0f,
                elevationDeltaMeters,
                startSlope,
                endSlope,
                bankStartDegrees,
                bankEndDegrees,
                curveSeverity);
        }

        public static TrackGeometrySpan ClothoidWithProfile(
            float lengthMeters,
            float startRadiusMeters,
            float endRadiusMeters,
            TrackCurveDirection direction,
            TrackCurveSeverity? curveSeverity,
            float elevationDeltaMeters,
            float startSlope,
            float endSlope,
            float bankStartDegrees,
            float bankEndDegrees)
        {
            return new TrackGeometrySpan(
                TrackGeometrySpanKind.Clothoid,
                lengthMeters,
                direction,
                0f,
                startRadiusMeters,
                endRadiusMeters,
                elevationDeltaMeters,
                startSlope,
                endSlope,
                bankStartDegrees,
                bankEndDegrees,
                curveSeverity);
        }

        public float CurvatureAt(float offsetMeters)
        {
            if (LengthMeters <= 0f)
                return 0f;

            var clamped = Clamp(offsetMeters, 0f, LengthMeters);
            switch (Kind)
            {
                case TrackGeometrySpanKind.Straight:
                    return 0f;
                case TrackGeometrySpanKind.Arc:
                    return SignedCurvature(RadiusMeters);
                case TrackGeometrySpanKind.Clothoid:
                    var k0 = SignedCurvature(StartRadiusMeters);
                    var k1 = SignedCurvature(EndRadiusMeters);
                    var t = clamped / LengthMeters;
                    return k0 + (k1 - k0) * t;
                default:
                    return 0f;
            }
        }

        public float StartCurvature => CurvatureAt(0f);
        public float EndCurvature => CurvatureAt(LengthMeters);

        public float SlopeAt(float offsetMeters)
        {
            if (LengthMeters <= 0f)
                return 0f;
            var clamped = Clamp(offsetMeters, 0f, LengthMeters);
            var t = clamped / LengthMeters;
            return StartSlope + (EndSlope - StartSlope) * t;
        }

        private float SignedCurvature(float radiusMeters)
        {
            if (radiusMeters <= 0f)
                return 0f;
            var curvature = 1.0f / radiusMeters;
            return Direction == TrackCurveDirection.Left ? -curvature : curvature;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public readonly struct TrackZone<T>
    {
        public float StartMeters { get; }
        public float EndMeters { get; }
        public T Value { get; }

        public TrackZone(float startMeters, float endMeters, T value)
        {
            if (!IsFinite(startMeters))
                throw new ArgumentOutOfRangeException(nameof(startMeters));
            if (!IsFinite(endMeters))
                throw new ArgumentOutOfRangeException(nameof(endMeters));
            if (endMeters < startMeters)
                throw new ArgumentException("Zone end must be greater than or equal to start.", nameof(endMeters));

            StartMeters = startMeters;
            EndMeters = endMeters;
            Value = value;
        }

        public bool Contains(float s)
        {
            // Use inclusive end bound to avoid single-frame blips at zone boundaries
            return s >= StartMeters && s <= EndMeters;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class TrackGeometrySpec
    {
        public IReadOnlyList<TrackGeometrySpan> Spans { get; }
        public float SampleSpacingMeters { get; }
        public bool EnforceClosure { get; }
        public float TotalLengthMeters { get; }

        public TrackGeometrySpec(
            IReadOnlyList<TrackGeometrySpan> spans,
            float sampleSpacingMeters = 1.0f,
            bool enforceClosure = true)
        {
            Spans = spans ?? throw new ArgumentNullException(nameof(spans));    
            if (Spans.Count == 0)
                throw new ArgumentException("Geometry spec requires at least one span.", nameof(spans));
            if (!IsFinite(sampleSpacingMeters) || sampleSpacingMeters <= 0f)    
                throw new ArgumentOutOfRangeException(nameof(sampleSpacingMeters));

            SampleSpacingMeters = sampleSpacingMeters;
            EnforceClosure = enforceClosure;
            var total = 0f;
            for (var i = 0; i < Spans.Count; i++)
                total += Spans[i].LengthMeters;
            TotalLengthMeters = total;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

}
