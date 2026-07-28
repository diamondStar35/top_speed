using System;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        // Turn numbering is derived purely from the segment list, so both the client that owns the
        // car and any client watching a remote car compute the same number from a position alone.
        // Each curved segment is its own turn, numbered 1..N in lap order; a straight has no turn of
        // its own but points at the next curve ahead (for "approaching turn N"), wrapping to turn 1
        // once past the final curve. This deliberately counts contiguous curves as separate turns so
        // that ovals whose turn 1 and turn 2 sit end to end (no straight between) announce as two
        // turns rather than one. The flip side is that a single corner an author shaped from several
        // segments is counted as several turns; there is no geometric signal that separates the two
        // cases, so the count reflects the track's curves rather than a circuit's official numbering.
        private int[]? _turnNumberBySegment;
        private bool[]? _segmentIsCurve;
        private bool[]? _segmentWrapsPastLapEnd;
        private int _turnCount;
        private bool _turnTableBuilt;

        // Resolves the turn a position sits in or is heading toward. Position is a total distance
        // (may span multiple laps); it is folded into a single lap before lookup. Returns false when
        // the track has no curves at all, in which case there is nothing meaningful to announce.
        // approachingLapEnd is set when the position is on the closing straight past the final curve,
        // where the only turn ahead (turn 1) belongs to the next lap; the caller phrases that as
        // completing the lap rather than approaching turn 1.
        public bool TryGetTurn(float position, out int turnNumber, out bool inTurn, out bool approachingLapEnd)
        {
            turnNumber = 0;
            inTurn = false;
            approachingLapEnd = false;
            if (_lapDistance == 0)
                Initialize();

            EnsureTurnTable();
            if (_turnCount == 0 || _turnNumberBySegment == null || _segmentIsCurve == null || _segmentWrapsPastLapEnd == null)
                return false;

            var index = RoadIndexAt(position);
            if (index < 0 || index >= _segmentCount)
                return false;

            turnNumber = _turnNumberBySegment[index];
            inTurn = _segmentIsCurve[index];
            approachingLapEnd = !inTurn && _segmentWrapsPastLapEnd[index];
            return true;
        }

        private void EnsureTurnTable()
        {
            if (_turnTableBuilt)
                return;

            var n = Math.Max(0, _segmentCount);
            var numbers = new int[n];
            var isCurve = new bool[n];
            var count = 0;
            for (var i = 0; i < n; i++)
            {
                var curve = _definition[i].Type != TrackType.Straight;
                isCurve[i] = curve;
                if (curve)
                {
                    count++;
                    numbers[i] = count;
                }
            }

            var wrapsPastLapEnd = new bool[n];
            if (count > 0)
            {
                // Walk backwards so each straight inherits the turn number of the nearest curve
                // ahead of it. Trailing straights (after the last curve) start seeded with turn 1,
                // which is the curve they reach after crossing the start/finish line; those are also
                // the straights whose next turn only exists on the next lap, flagged via seenCurve.
                var nextTurn = 1;
                var seenCurve = false;
                for (var i = n - 1; i >= 0; i--)
                {
                    if (isCurve[i])
                    {
                        nextTurn = numbers[i];
                        seenCurve = true;
                    }
                    else
                    {
                        numbers[i] = nextTurn;
                        wrapsPastLapEnd[i] = !seenCurve;
                    }
                }
            }

            _turnNumberBySegment = numbers;
            _segmentIsCurve = isCurve;
            _segmentWrapsPastLapEnd = wrapsPastLapEnd;
            _turnCount = count;
            _turnTableBuilt = true;
        }
    }
}
