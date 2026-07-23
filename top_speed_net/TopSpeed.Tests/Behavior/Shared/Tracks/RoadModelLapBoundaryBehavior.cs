using System;
using TopSpeed.Data;
using Xunit;

namespace TopSpeed.Tests;

// Regression coverage for the lap-boundary road jump that crashed cars at the
// start/finish line and on pit exit. RoadModel.At() used to derive the lap index
// (floor(position / LapDistance)) and the in-lap distance (position % LapDistance)
// from two independent calculations. At a lap boundary float rounding could split
// them — one saying "end of lap N", the other "start of lap N+1" — and because each
// lap is offset sideways by LapCenter, that one-frame disagreement shifted the road a
// whole LapCenter and threw the car out of bounds.
//
// The track under test is a representative oval: nine segments with four left curves,
// so LapCenter is a large non-zero -134 m. Any track with a non-zero LapCenter is
// vulnerable; if the lap index and the in-lap distance ever disagree, the road center
// jumps by that full amount, which these tests catch.
[Trait("Category", "Behavior")]
public sealed class RoadModelLapBoundaryBehaviorTests
{
    private static TrackDefinition Seg(TrackType type, float length) =>
        new TrackDefinition(type, TrackSurface.Asphalt, TrackNoise.NoNoise, length);

    private static TrackDefinition[] CurvedOvalDefinitions() => new[]
    {
        Seg(TrackType.Straight, 502.92f),
        Seg(TrackType.HardLeft, 402f),
        Seg(TrackType.Straight, 201f),
        Seg(TrackType.Left, 402f),
        Seg(TrackType.Straight, 1006f),
        Seg(TrackType.HardLeft, 402f),
        Seg(TrackType.Straight, 203f),
        Seg(TrackType.Left, 402f),
        Seg(TrackType.Straight, 502.92f),
    };

    private static float Center(RoadSeg seg) => (seg.Left + seg.Right) * 0.5f;

    [Fact]
    public void Geometry_HasExpectedLapDistanceAndCenter()
    {
        var model = new RoadModel(CurvedOvalDefinitions());

        // Documents the geometry the rest of the tests rely on.
        model.LapDistance.Should().BeApproximately(4023.84f, 0.01f);
        model.LapCenter.Should().BeApproximately(-134f, 0.01f);
    }

    [Fact]
    public void At_ExactLapBoundaries_StaysOnTheCorrectLap()
    {
        var model = new RoadModel(CurvedOvalDefinitions());

        // Each lap starts on segment 1 (a straight), whose center is exactly the lap's
        // sideways offset lap * LapCenter. If the index/distance split fires, the center
        // lands a whole LapCenter (134 m) away — far outside this tolerance.
        for (var lap = 1; lap <= 500; lap++)
        {
            var position = lap * model.LapDistance;
            var center = Center(model.At(position));

            center.Should().BeApproximately(lap * model.LapCenter, 1.0f,
                $"the road center at the start of lap {lap} (position {position}) must not jump a LapCenter");
        }
    }

    [Fact]
    public void At_IsContinuousAcrossLapBoundaries()
    {
        var model = new RoadModel(CurvedOvalDefinitions());

        // Step finely across each seam; the center must never move more than the car
        // could physically travel between samples. A split shows up as a ~134 m jump.
        for (var lap = 1; lap <= 500; lap++)
        {
            var boundary = lap * model.LapDistance;
            var before = Center(model.At(MathF.BitDecrement(boundary)));
            var at = Center(model.At(boundary));
            var after = Center(model.At(boundary + 0.1f));

            Math.Abs(at - before).Should().BeLessThan(2.0f,
                $"road center must not jump across the lap {lap} seam");
            Math.Abs(after - at).Should().BeLessThan(2.0f,
                $"road center must not jump just after the lap {lap} seam");
        }
    }

    [Fact]
    public void At_PitExitReconstruction_MatchesNeighbouringRoad()
    {
        var model = new RoadModel(CurvedOvalDefinitions());
        var lapDist = model.LapDistance;

        // Pit exit rejoins at floor(entry / lapDist) * lapDist + exitDistanceInLap — an
        // exact lap multiple plus an offset, which is what used to land dead on the
        // ambiguous boundary. Rebuild that position for many laps and a range of exit
        // offsets and confirm the road there agrees with the road a hair earlier/later.
        float[] exitOffsets = { 0f, 0.5f, 50f, 201f, 402f };
        for (var lap = 0; lap < 500; lap++)
        {
            var lapStart = (float)Math.Floor((lap * lapDist + 10f) / lapDist) * lapDist;
            foreach (var exitOffset in exitOffsets)
            {
                var exitY = lapStart + exitOffset;
                var here = Center(model.At(exitY));
                var justAfter = Center(model.At(exitY + 0.1f));

                Math.Abs(justAfter - here).Should().BeLessThan(2.0f,
                    $"pit-exit road center at lap {lap}, offset {exitOffset} must not jump a LapCenter");
            }
        }
    }
}
