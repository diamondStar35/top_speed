using System;

namespace TopSpeed.Data
{
    public readonly struct RoadSeg
    {
        public RoadSeg(float left, float right, TrackSurface surface, TrackType type, float length, int index, float relPos)
        {
            Left = left;
            Right = right;
            Surface = surface;
            Type = type;
            Length = length;
            Index = index;
            RelPos = relPos;
        }

        public float Left { get; }
        public float Right { get; }
        public TrackSurface Surface { get; }
        public TrackType Type { get; }
        public float Length { get; }
        public int Index { get; }
        public float RelPos { get; }
    }

    public sealed class RoadModel
    {
        public const float DefaultLaneHalfWidth = 5.0f;
        public const float LegacyLaneWidthMeters = 50.0f;
        private const float MinPartLengthMeters = 50.0f;

        private readonly TrackDefinition[] _defs;
        private readonly float _laneHalfWidth;
        private readonly float _curveScale;

        public RoadModel(TrackDefinition[] definitions, float laneHalfWidth = DefaultLaneHalfWidth)
        {
            _defs = definitions ?? Array.Empty<TrackDefinition>();
            _laneHalfWidth = laneHalfWidth > 0f ? laneHalfWidth : DefaultLaneHalfWidth;
            _curveScale = LegacyLaneWidthMeters > 0f ? _laneHalfWidth / LegacyLaneWidthMeters : 1.0f;
            if (_curveScale <= 0f)
                _curveScale = 0.01f;

            var lapDistance = 0f;
            var lapCenter = 0f;
            for (var i = 0; i < _defs.Length; i++)
            {
                var def = _defs[i];
                lapDistance += def.Length;
                lapCenter = UpdateCenter(lapCenter, def);
            }

            LapDistance = lapDistance;
            LapCenter = lapCenter;
        }

        public float LapDistance { get; }
        public float LapCenter { get; }
        public float LaneHalfWidth => _laneHalfWidth;
        public float CurveScale => _curveScale;

        /// <summary>
        /// Signed lateral drift of the road corridor per meter travelled forwards.
        /// A curve here is modelled as the whole corridor translating sideways, so this is
        /// the lateral speed (per unit forward speed) a car must hold to stay on the road.
        /// Positive drifts right, negative drifts left.
        /// </summary>
        public static float CenterDriftPerMeter(TrackType type, float curveScale)
        {
            switch (type)
            {
                case TrackType.EasyLeft:
                    return -curveScale / 2f;
                case TrackType.Left:
                    return -curveScale * 2f / 3f;
                case TrackType.HardLeft:
                    return -curveScale;
                case TrackType.HairpinLeft:
                    return -curveScale * 3f / 2f;
                case TrackType.EasyRight:
                    return curveScale / 2f;
                case TrackType.Right:
                    return curveScale * 2f / 3f;
                case TrackType.HardRight:
                    return curveScale;
                case TrackType.HairpinRight:
                    return curveScale * 3f / 2f;
                default:
                    return 0f;
            }
        }

        public float CenterDriftPerMeter(TrackType type) => CenterDriftPerMeter(type, _curveScale);

        public float Wrap(float position)
        {
            if (LapDistance <= 0f)
                return position;
            var wrapped = position % LapDistance;
            if (wrapped < 0f)
                wrapped += LapDistance;
            return wrapped;
        }

        public RoadSeg At(float position)
        {
            if (_defs.Length == 0 || LapDistance <= 0f)
                return new RoadSeg(0f, 0f, TrackSurface.Asphalt, TrackType.Straight, MinPartLengthMeters, -1, 0f);

            // Derive the in-lap distance from the same lap index instead of an
            // independent modulo, so the two can never disagree at a lap
            // boundary (a float-rounding split there used to shift the road one
            // LapCenter sideways for a frame and crash the car — most reliably
            // on pit exit, which lands exactly on the boundary).
            var lap = (int)Math.Floor(position / LapDistance);
            var pos = position - (lap * LapDistance);
            if (pos >= LapDistance)
            {
                pos -= LapDistance;
                lap++;
            }
            else if (pos < 0f)
            {
                pos += LapDistance;
                lap--;
            }
            var dist = 0.0f;
            var center = lap * LapCenter;

            for (var i = 0; i < _defs.Length; i++)
            {
                var def = _defs[i];
                if (dist <= pos && dist + def.Length > pos)
                {
                    var relPos = pos - dist;
                    return ApplyRoadOffset(center, relPos, def, i);
                }

                center = UpdateCenter(center, def);
                dist += def.Length;
            }

            return new RoadSeg(0f, 0f, TrackSurface.Asphalt, TrackType.Straight, MinPartLengthMeters, -1, 0f);
        }

        private float UpdateCenter(float center, TrackDefinition def)
        {
            switch (def.Type)
            {
                case TrackType.EasyLeft:
                    return center - (def.Length * _curveScale) / 2f;
                case TrackType.Left:
                    return center - (def.Length * _curveScale) * 2f / 3f;
                case TrackType.HardLeft:
                    return center - def.Length * _curveScale;
                case TrackType.HairpinLeft:
                    return center - (def.Length * _curveScale) * 3f / 2f;
                case TrackType.EasyRight:
                    return center + (def.Length * _curveScale) / 2f;
                case TrackType.Right:
                    return center + (def.Length * _curveScale) * 2f / 3f;
                case TrackType.HardRight:
                    return center + def.Length * _curveScale;
                case TrackType.HairpinRight:
                    return center + (def.Length * _curveScale) * 3f / 2f;
                default:
                    return center;
            }
        }

        private RoadSeg ApplyRoadOffset(float center, float relPos, TrackDefinition def, int index)
        {
            var offset = relPos * _curveScale;
            var laneHalfWidth = GetLaneHalfWidth(def);
            float left;
            float right;

            switch (def.Type)
            {
                case TrackType.Straight:
                    left = center - laneHalfWidth;
                    right = center + laneHalfWidth;
                    break;
                case TrackType.EasyLeft:
                    left = center - laneHalfWidth - offset / 2f;
                    right = center + laneHalfWidth - offset / 2f;
                    break;
                case TrackType.Left:
                    left = center - laneHalfWidth - offset * 2f / 3f;
                    right = center + laneHalfWidth - offset * 2f / 3f;
                    break;
                case TrackType.HardLeft:
                    left = center - laneHalfWidth - offset;
                    right = center + laneHalfWidth - offset;
                    break;
                case TrackType.HairpinLeft:
                    left = center - laneHalfWidth - offset * 3f / 2f;
                    right = center + laneHalfWidth - offset * 3f / 2f;
                    break;
                case TrackType.EasyRight:
                    left = center - laneHalfWidth + offset / 2f;
                    right = center + laneHalfWidth + offset / 2f;
                    break;
                case TrackType.Right:
                    left = center - laneHalfWidth + offset * 2f / 3f;
                    right = center + laneHalfWidth + offset * 2f / 3f;
                    break;
                case TrackType.HardRight:
                    left = center - laneHalfWidth + offset;
                    right = center + laneHalfWidth + offset;
                    break;
                case TrackType.HairpinRight:
                    left = center - laneHalfWidth + offset * 3f / 2f;
                    right = center + laneHalfWidth + offset * 3f / 2f;
                    break;
                default:
                    left = center - laneHalfWidth;
                    right = center + laneHalfWidth;
                    break;
            }

            return new RoadSeg(left, right, def.Surface, def.Type, def.Length, index, relPos);
        }

        private float GetLaneHalfWidth(TrackDefinition def)
        {
            if (def.Width > 0f)
            {
                var half = def.Width * 0.5f;
                if (half > 0f)
                    return half;
            }

            return _laneHalfWidth > 0f ? _laneHalfWidth : DefaultLaneHalfWidth;
        }
    }
}
