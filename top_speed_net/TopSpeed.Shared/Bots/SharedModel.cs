using TopSpeed.Data;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Shared bot tuning tables. Corner and braking limits are no longer table driven - the
    /// driver derives those from the car's own physics (see <see cref="BotDrivingPlanner"/>) -
    /// so what remains here is the surface handicap, which has no physical counterpart the
    /// planner can query.
    /// </summary>
    public static class BotSharedModel
    {
        public static float GetSurfaceSpeedFactor(TrackSurface surface)
        {
            return surface switch
            {
                TrackSurface.Gravel => 0.90f,
                TrackSurface.Water => 0.82f,
                TrackSurface.Sand => 0.74f,
                TrackSurface.Snow => 0.78f,
                _ => 1.0f
            };
        }
    }
}
