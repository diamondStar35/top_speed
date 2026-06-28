using System;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public void SetLaneWidth(float laneWidth)
        {
            _laneWidth = laneWidth;
            UpdateCurveScale();
            _roadModel = null;
        }

        public float LaneHalfWidthAtPosition(float position)
        {
            var laneHalfWidth = _laneWidth;
            var segmentIndex = RoadIndexAt(position);
            if (segmentIndex >= 0 && segmentIndex < _segmentCount)
            {
                var segmentWidth = _definition[segmentIndex].Width;
                if (segmentWidth > 0f)
                {
                    var segmentHalfWidth = segmentWidth * 0.5f;
                    if (segmentHalfWidth > 0f)
                        laneHalfWidth = segmentHalfWidth;
                }
            }

            return Math.Max(0.1f, laneHalfWidth);
        }

        public int Lap(float position)
        {
            if (_lapDistance <= 0)
                return 1;
            var lap = (int)Math.Floor(position / _lapDistance) + 1;
            return lap < 1 ? 1 : lap;
        }

        public Road RoadAtPosition(float position)
        {
            if (_lapDistance == 0)
                Initialize();
            var model = GetRoadModel();
            var seg = model.At(position);
            _prevRelPos = _relPos;
            _relPos = seg.RelPos;
            _currentRoad = seg.Index >= 0 ? seg.Index : 0;
            return new Road
            {
                Left = seg.Left,
                Right = seg.Right,
                Surface = seg.Surface,
                Type = seg.Type,
                Length = seg.Length
            };
        }

        public Road RoadComputer(float position)
        {
            if (_lapDistance == 0)
                Initialize();
            var seg = GetRoadModel().At(position);
            return new Road
            {
                Left = seg.Left,
                Right = seg.Right,
                Surface = seg.Surface,
                Type = seg.Type,
                Length = seg.Length
            };
        }

        public bool TryGetPitPointDistance(Data.SegmentPitPoint pitPoint, bool atSegmentEnd, out float distanceInLap)
        {
            if (_lapDistance == 0)
                Initialize();
            for (var i = 0; i < _segmentCount; i++)
            {
                var matches = pitPoint == Data.SegmentPitPoint.PitEntry
                    ? _definition[i].IsPitEntry
                    : _definition[i].IsPitExit;
                if (matches)
                {
                    // Pit entry begins at the start of its segment; pit exit rejoins the
                    // track at the end of its segment (start distance + segment length).
                    distanceInLap = _segmentStartDistances[i];
                    if (atSegmentEnd)
                        distanceInLap += _definition[i].Length;
                    return true;
                }
            }
            distanceInLap = 0f;
            return false;
        }

        public bool NextRoad(float position, float speed, int curveAnnouncementMode, float speedDependentLeadTimeSeconds, out Road road)
        {
            road = new Road();
            if (_segmentCount == 0)
                return false;

            if (curveAnnouncementMode == 0)
            {
                var currentLength = _definition[_currentRoad].Length;
                if ((_relPos + _callLength > currentLength) && (_prevRelPos + _callLength <= currentLength))
                {
                    var next = _definition[(_currentRoad + 1) % _segmentCount];
                    road.Type = next.Type;
                    road.Surface = next.Surface;
                    road.Length = next.Length;
                    return true;
                }

                return false;
            }

            var safeLeadTimeSeconds = Math.Max(0.5f, Math.Min(4.0f, speedDependentLeadTimeSeconds));
            var speedMetersPerSecond = Math.Max(0f, speed) / 3.6f;
            var lookAhead = _callLength + speedMetersPerSecond * safeLeadTimeSeconds;
            var roadAhead = RoadIndexAt(position + lookAhead);
            if (roadAhead < 0)
                return false;

            var delta = (roadAhead - _lastCalled + _segmentCount) % _segmentCount;
            if (delta > 0 && delta <= _segmentCount / 2)
            {
                var next = _definition[roadAhead];
                road.Type = next.Type;
                road.Surface = next.Surface;
                road.Length = next.Length;
                _lastCalled = roadAhead;
                return true;
            }

            return false;
        }

        private int RoadIndexAt(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            var pos = GetPositionInLap(position);
            var dist = 0.0f;
            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                    return i;
                dist += _definition[i].Length;
            }

            return -1;
        }

        private float GetLapStartDistance(float position)
        {
            if (_lapDistance <= 0f)
                return 0f;
            var safePosition = position < 0f ? 0f : position;
            var lapIndex = (float)Math.Floor(safePosition / _lapDistance);
            return lapIndex * _lapDistance;
        }

        private float GetPositionInLap(float position)
        {
            if (_lapDistance <= 0f)
                return position;

            var lapStart = GetLapStartDistance(position);
            var inLap = position - lapStart;
            if (inLap < 0f)
                return 0f;
            if (inLap >= _lapDistance)
                return 0f;
            return inLap;
        }

        private float AlignToReferenceLap(float zInLap, float referencePosition)
        {
            if (_lapDistance <= 0f)
                return zInLap;
            return GetLapStartDistance(referencePosition) + zInLap;
        }

        private static float Lerp(float a, float b, float t)
        {
            if (t < 0f)
                t = 0f;
            if (t > 1f)
                t = 1f;
            return a + ((b - a) * t);
        }

        private RoadModel GetRoadModel()
        {
            if (_roadModel == null)
                _roadModel = new RoadModel(_definition, _laneWidth);
            return _roadModel;
        }

        private float UpdateCenter(float center, TrackDefinition definition)
        {
            switch (definition.Type)
            {
                case TrackType.EasyLeft:
                    return center - (definition.Length * _curveScale) / 2;
                case TrackType.Left:
                    return center - (definition.Length * _curveScale) * 2 / 3;
                case TrackType.HardLeft:
                    return center - definition.Length * _curveScale;
                case TrackType.HairpinLeft:
                    return center - (definition.Length * _curveScale) * 3 / 2;
                case TrackType.EasyRight:
                    return center + (definition.Length * _curveScale) / 2;
                case TrackType.Right:
                    return center + (definition.Length * _curveScale) * 2 / 3;
                case TrackType.HardRight:
                    return center + definition.Length * _curveScale;
                case TrackType.HairpinRight:
                    return center + (definition.Length * _curveScale) * 3 / 2;
                default:
                    return center;
            }
        }

        private void ApplyRoadOffset(ref Road road, float center, float relPos, TrackType type)
        {
            var offset = relPos * _curveScale;
            switch (type)
            {
                case TrackType.Straight:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
                case TrackType.EasyLeft:
                    road.Left = center - _laneWidth - offset / 2;
                    road.Right = center + _laneWidth - offset / 2;
                    break;
                case TrackType.Left:
                    road.Left = center - _laneWidth - offset * 2 / 3;
                    road.Right = center + _laneWidth - offset * 2 / 3;
                    break;
                case TrackType.HardLeft:
                    road.Left = center - _laneWidth - offset;
                    road.Right = center + _laneWidth - offset;
                    break;
                case TrackType.HairpinLeft:
                    road.Left = center - _laneWidth - offset * 3 / 2;
                    road.Right = center + _laneWidth - offset * 3 / 2;
                    break;
                case TrackType.EasyRight:
                    road.Left = center - _laneWidth + offset / 2;
                    road.Right = center + _laneWidth + offset / 2;
                    break;
                case TrackType.Right:
                    road.Left = center - _laneWidth + offset * 2 / 3;
                    road.Right = center + _laneWidth + offset * 2 / 3;
                    break;
                case TrackType.HardRight:
                    road.Left = center - _laneWidth + offset;
                    road.Right = center + _laneWidth + offset;
                    break;
                case TrackType.HairpinRight:
                    road.Left = center - _laneWidth + offset * 3 / 2;
                    road.Right = center + _laneWidth + offset * 3 / 2;
                    break;
                default:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
            }
        }

        private void UpdateCurveScale()
        {
            _curveScale = LegacyLaneWidthMeters > 0f ? _laneWidth / LegacyLaneWidthMeters : 1.0f;
            if (_curveScale <= 0f)
                _curveScale = 0.01f;
        }
    }
}
