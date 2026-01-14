using System;
using System.Collections.Generic;
using System.Numerics;

namespace TopSpeed.Tracks.Geometry
{
    public readonly struct TrackPose
    {
        public Vector3 Position { get; }
        public Vector3 Tangent { get; }
        public Vector3 Right { get; }
        public Vector3 Up { get; }
        public float HeadingRadians { get; }
        public float BankRadians { get; }

        public TrackPose(Vector3 position, Vector3 tangent, Vector3 right, Vector3 up, float headingRadians, float bankRadians)
        {
            Position = position;
            Tangent = tangent;
            Right = right;
            Up = up;
            HeadingRadians = headingRadians;
            BankRadians = bankRadians;
        }
    }

    public readonly struct TrackEdges
    {
        public Vector3 Center { get; }
        public Vector3 Left { get; }
        public Vector3 Right { get; }

        public TrackEdges(Vector3 center, Vector3 left, Vector3 right)
        {
            Center = center;
            Left = left;
            Right = right;
        }
    }

    public sealed class TrackGeometry
    {
        private const float MinSpacingMeters = 0.1f;
        private const float TwoPi = (float)(Math.PI * 2.0);

        private readonly TrackGeometrySpan[] _spans;
        private readonly float[] _spanStartMeters;
        private readonly Vector3[] _positions;
        private readonly float[] _headings;
        private readonly float[] _banks;
        private readonly float[] _slopes;

        public float LengthMeters { get; }
        public float SampleSpacingMeters { get; }
        public int SampleCount => _positions.Length;

        private TrackGeometry(
            TrackGeometrySpan[] spans,
            float[] spanStartMeters,
            Vector3[] positions,
            float[] headings,
            float[] banks,
            float[] slopes,
            float lengthMeters,
            float sampleSpacingMeters)
        {
            _spans = spans;
            _spanStartMeters = spanStartMeters;
            _positions = positions;
            _headings = headings;
            _banks = banks;
            _slopes = slopes;
            LengthMeters = lengthMeters;
            SampleSpacingMeters = sampleSpacingMeters;
        }

        public static TrackGeometry Build(TrackGeometrySpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            var spacing = Math.Max(MinSpacingMeters, spec.SampleSpacingMeters);
            var spans = new TrackGeometrySpan[spec.Spans.Count];
            for (var i = 0; i < spans.Length; i++)
            {
                spans[i] = spec.Spans[i];
            }

            var spanStart = new float[spans.Length];
            var totalLength = 0f;
            for (var i = 0; i < spans.Length; i++)
            {
                spanStart[i] = totalLength;
                totalLength += spans[i].LengthMeters;
            }

            var positions = new List<Vector3>();
            var headings = new List<float>();
            var banks = new List<float>();
            var slopes = new List<float>();

            var position = Vector3.Zero;
            var heading = 0f;

            // Initialize with first span's starting bank and slope to avoid discontinuity
            var initialBank = spans.Length > 0 ? DegreesToRadians(spans[0].BankStartDegrees) : 0f;
            var initialSlope = spans.Length > 0 ? spans[0].StartSlope : 0f;

            positions.Add(position);
            headings.Add(heading);
            banks.Add(initialBank);
            slopes.Add(initialSlope);

            for (var spanIndex = 0; spanIndex < spans.Length; spanIndex++)
            {
                var span = spans[spanIndex];
                var spanLength = span.LengthMeters;
                if (spanLength <= 0f)
                    continue;

                var local = 0f;
                
                while (local < spanLength)
                {
                    var step = Math.Min(spacing, spanLength - local);
                    var localMid = local + step * 0.5f;
                    var curvature = span.CurvatureAt(localMid);
                    var bankDegrees = Lerp(span.BankStartDegrees, span.BankEndDegrees, localMid / spanLength);
                    var bankRadians = DegreesToRadians(bankDegrees);
                    var slope = span.SlopeAt(localMid);
                    var headingMid = heading + curvature * step * 0.5f;

                    var dx = (float)(Math.Sin(headingMid) * step);
                    var dz = (float)(Math.Cos(headingMid) * step);
                    var dy = slope * step;

                    position += new Vector3(dx, dy, dz);
                    heading += curvature * step;
                    local += step;

                    positions.Add(position);
                    headings.Add(heading);
                    banks.Add(bankRadians);
                    slopes.Add(slope);
                }
            }

            if (spec.EnforceClosure && positions.Count > 1)
            {
                ApplyClosureCorrection(positions, headings, banks, slopes, spacing);
            }

            return new TrackGeometry(
                spans,
                spanStart,
                positions.ToArray(),
                headings.ToArray(),
                banks.ToArray(),
                slopes.ToArray(),
                totalLength,
                spacing);
        }

        public TrackPose GetPose(float sMeters)
        {
            if (_positions.Length == 0 || LengthMeters <= 0f)
                return default;

            var s = WrapDistance(sMeters);
            return BuildPoseAt(s);
        }

        public TrackPose GetPoseClamped(float sMeters)
        {
            if (_positions.Length == 0 || LengthMeters <= 0f)
                return default;

            var s = Math.Max(0f, Math.Min(LengthMeters, sMeters));
            return BuildPoseAt(s);
        }

        public TrackEdges GetEdges(float sMeters, float widthMeters)
        {
            var pose = GetPose(sMeters);
            var halfWidth = Math.Max(0f, widthMeters * 0.5f);
            var left = pose.Position - pose.Right * halfWidth;
            var right = pose.Position + pose.Right * halfWidth;
            return new TrackEdges(pose.Position, left, right);
        }

        public TrackGeometrySpan GetSpan(float sMeters)
        {
            if (_spans.Length == 0 || LengthMeters <= 0f)
                return default;

            var s = WrapDistance(sMeters);
            var index = FindSpanIndex(s);
            return _spans[index];
        }

        public TrackGeometrySpan GetSpanClamped(float sMeters)
        {
            if (_spans.Length == 0 || LengthMeters <= 0f)
                return default;

            var s = Math.Max(0f, Math.Min(LengthMeters, sMeters));
            var index = FindSpanIndex(s);
            return _spans[index];
        }

        public float CurvatureAt(float sMeters)
        {
            if (_spans.Length == 0 || LengthMeters <= 0f)
                return 0f;

            var s = WrapDistance(sMeters);
            var spanIndex = FindSpanIndex(s);
            var spanStart = _spanStartMeters[spanIndex];
            var local = s - spanStart;
            return _spans[spanIndex].CurvatureAt(local);
        }

        public int GetSpanIndexClamped(float sMeters)
        {
            if (_spans.Length == 0 || LengthMeters <= 0f)
                return -1;

            var s = Math.Max(0f, Math.Min(LengthMeters, sMeters));
            return FindSpanIndex(s);
        }

        private float WrapDistance(float sMeters)
        {
            if (LengthMeters <= 0f)
                return 0f;

            var wrapped = sMeters % LengthMeters;
            if (wrapped < 0f)
                wrapped += LengthMeters;
            // Use epsilon comparison to handle floating point precision issues
            if (wrapped >= LengthMeters - 0.0001f)
                return 0f;
            return wrapped;
        }

        private int FindSpanIndex(float sMeters)
        {
            var index = Array.BinarySearch(_spanStartMeters, sMeters);
            if (index >= 0)
                return index;
            index = ~index - 1;
            if (index < 0)
                index = 0;
            return index;
        }

        private TrackPose BuildPoseAt(float s)
        {
            var lastIndex = _positions.Length - 1;
            var index = (int)(s / SampleSpacingMeters);
            if (index >= lastIndex)
            {
                return BuildPose(_positions[lastIndex], _headings[lastIndex], _banks[lastIndex], _slopes[lastIndex]);
            }

            var t = (s - index * SampleSpacingMeters) / SampleSpacingMeters;
            var position = Vector3.Lerp(_positions[index], _positions[index + 1], t);
            var heading = LerpAngle(_headings[index], _headings[index + 1], t);
            var bank = _banks[index] + (_banks[index + 1] - _banks[index]) * t;
            var slope = _slopes[index] + (_slopes[index + 1] - _slopes[index]) * t;
            return BuildPose(position, heading, bank, slope);
        }

        private static TrackPose BuildPose(Vector3 position, float heading, float bank, float slope)
        {
            var tangent = new Vector3(
                (float)Math.Sin(heading),
                slope,
                (float)Math.Cos(heading));
            tangent = Vector3.Normalize(tangent);

            var up = Vector3.UnitY;
            var right = Vector3.Cross(up, tangent);
            if (right.LengthSquared() < 0.000001f)
                right = Vector3.UnitX;
            right = Vector3.Normalize(right);
            up = Vector3.Normalize(Vector3.Cross(tangent, right));

            if (Math.Abs(bank) > 0.0001f)
            {
                right = RotateAroundAxis(right, tangent, bank);
                up = RotateAroundAxis(up, tangent, bank);
            }

            return new TrackPose(position, tangent, right, up, heading, bank);
        }

        private static void ApplyClosureCorrection(
            List<Vector3> positions,
            List<float> headings,
            List<float> banks,
            List<float> slopes,
            float sampleSpacingMeters)
        {
            var lastIndex = positions.Count - 1;
            var deltaPosition = positions[lastIndex] - positions[0];
            var deltaHeading = NormalizeAngle(headings[lastIndex] - headings[0]);
            var deltaBank = banks[lastIndex] - banks[0];
            var totalLength = Math.Max(sampleSpacingMeters * lastIndex, 0.0001f);
            var slopeCorrection = deltaPosition.Y / totalLength;

            if (lastIndex <= 0)
                return;

            for (var i = 0; i <= lastIndex; i++)
            {
                var t = i / (float)lastIndex;
                positions[i] -= deltaPosition * t;
                headings[i] -= deltaHeading * t;
                banks[i] -= deltaBank * t;
                slopes[i] -= slopeCorrection;
            }
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > Math.PI)
                angle -= TwoPi;
            while (angle <= -Math.PI)
                angle += TwoPi;
            return angle;
        }

        private static float LerpAngle(float a, float b, float t)
        {
            var delta = NormalizeAngle(b - a);
            return a + delta * t;
        }

        private static float DegreesToRadians(float degrees)
        {
            return (float)(degrees * (Math.PI / 180.0));
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static Vector3 RotateAroundAxis(Vector3 vector, Vector3 axis, float angle)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            return vector * cos + Vector3.Cross(axis, vector) * sin + axis * Vector3.Dot(axis, vector) * (1f - cos);
        }
    }
}
