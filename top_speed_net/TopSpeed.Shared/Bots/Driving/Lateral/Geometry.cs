using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Road-relative geometry helpers.
    /// <para>
    /// The corridor translates sideways along a lap, so absolute X values taken at different
    /// points on the track are not comparable. The whole lateral planner therefore works in
    /// <em>offset from the road center</em>: a car holding its lane has a constant offset no
    /// matter how hard the road is curving.
    /// </para>
    /// </summary>
    internal static class BotRoadGeometry
    {
        /// <summary>Road center at a signed distance ahead of the car (negative looks behind).</summary>
        public static float CenterAt(in BotDrivingInput input, float distanceAheadM)
        {
            var road = input.Road;
            if (road.Length == 0)
                return 0f;

            var first = road[0];
            if (distanceAheadM <= first.DistanceAheadM || road.Length == 1)
                return first.Center + (first.DriftPerMeter * (distanceAheadM - first.DistanceAheadM));

            var last = road[road.Length - 1];
            if (distanceAheadM >= last.DistanceAheadM)
                return last.Center + (last.DriftPerMeter * (distanceAheadM - last.DistanceAheadM));

            for (var i = 1; i < road.Length; i++)
            {
                var hi = road[i];
                if (distanceAheadM > hi.DistanceAheadM)
                    continue;

                var lo = road[i - 1];
                var span = hi.DistanceAheadM - lo.DistanceAheadM;
                if (span <= 0.0001f)
                    return hi.Center;
                var t = (distanceAheadM - lo.DistanceAheadM) / span;
                return lo.Center + ((hi.Center - lo.Center) * t);
            }

            return last.Center;
        }

        /// <summary>Corridor drift at a signed distance ahead of the car.</summary>
        public static float DriftAt(in BotDrivingInput input, float distanceAheadM)
        {
            var road = input.Road;
            if (road.Length == 0)
                return 0f;
            if (distanceAheadM <= road[0].DistanceAheadM)
                return road[0].DriftPerMeter;

            for (var i = 1; i < road.Length; i++)
            {
                if (distanceAheadM <= road[i].DistanceAheadM)
                    return road[i - 1].DriftPerMeter;
            }

            return road[road.Length - 1].DriftPerMeter;
        }

        /// <summary>Half-width of the band of center offsets the car may legally occupy.</summary>
        public static float UsableHalfWidth(in BotRoadPreview road, float vehicleWidthM, float marginM)
        {
            var usable = road.HalfWidth - (vehicleWidthM * 0.5f) - marginM;
            return usable > 0.05f ? usable : 0.05f;
        }
    }
}
