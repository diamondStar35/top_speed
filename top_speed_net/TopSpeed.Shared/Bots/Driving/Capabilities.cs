using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Bots
{
    /// <summary>
    /// The physical envelope the planner is allowed to reason about. Built once per bot from
    /// its <see cref="BotPhysicsConfig"/> so the driver plans against the same numbers the
    /// simulation will actually apply, instead of guessed constants.
    /// </summary>
    public readonly struct BotCapabilities
    {
        public BotCapabilities(
            Config powertrain,
            float topSpeedKph,
            float massKg,
            float widthM,
            float lengthM,
            float highSpeedStability,
            float turnResponse,
            float surfaceTractionFactor)
        {
            Powertrain = powertrain;
            TopSpeedKph = Math.Max(1f, topSpeedKph);
            MassKg = Math.Max(1f, massKg);
            WidthM = Math.Max(0.1f, widthM);
            LengthM = Math.Max(0.1f, lengthM);
            HighSpeedStability = Math.Max(0f, Math.Min(1f, highSpeedStability));
            TurnResponse = Math.Max(0.2f, turnResponse);
            SurfaceTractionFactor = surfaceTractionFactor > 0f ? surfaceTractionFactor : 1f;
        }

        public Config? Powertrain { get; }
        public float TopSpeedKph { get; }
        public float MassKg { get; }
        public float WidthM { get; }
        public float LengthM { get; }
        public float HighSpeedStability { get; }
        public float TurnResponse { get; }
        public float SurfaceTractionFactor { get; }

        public bool HasPowertrain => Powertrain != null;

        public static BotCapabilities From(BotPhysicsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new BotCapabilities(
                config.Powertrain,
                config.TopSpeedKph,
                config.MassKg,
                config.WidthM,
                config.LengthM,
                config.HighSpeedStability,
                config.TurnResponse,
                config.SurfaceTractionFactor);
        }
    }
}
