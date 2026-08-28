using System;
using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public enum BotDrivingDifficulty : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    public enum BotManeuver : byte
    {
        Follow = 0,
        PassLeft = 1,
        PassRight = 2
    }

    /// <summary>
    /// One sample of the road corridor ahead of the bot.
    /// </summary>
    public readonly struct BotRoadPreview
    {
        public BotRoadPreview(float distanceAheadM, float left, float right, TrackSurface surface, TrackType type, float driftPerMeter, float segmentRemainingM)
        {
            DistanceAheadM = distanceAheadM;
            Left = Math.Min(left, right);
            Right = Math.Max(left, right);
            Surface = surface;
            Type = type;
            DriftPerMeter = driftPerMeter;
            SegmentRemainingM = Math.Max(1f, segmentRemainingM);
        }

        /// <summary>
        /// Convenience overload that derives the corridor drift from the corner type using the
        /// same curve scale <see cref="RoadModel"/> applies for a road of this width.
        /// </summary>
        public BotRoadPreview(float distanceAheadM, float left, float right, TrackSurface surface, TrackType type)
            : this(
                distanceAheadM,
                left,
                right,
                surface,
                type,
                RoadModel.CenterDriftPerMeter(type, Math.Abs(right - left) * 0.5f / RoadModel.LegacyLaneWidthMeters),
                DefaultSegmentRemainingM)
        {
        }

        /// <summary>Stand-in corner length for callers that have no segment data to hand.</summary>
        public const float DefaultSegmentRemainingM = 150f;

        public float DistanceAheadM { get; }
        public float Left { get; }
        public float Right { get; }
        public TrackSurface Surface { get; }
        public TrackType Type { get; }

        /// <summary>
        /// Signed lateral drift of the corridor per meter travelled forwards. Holding a line
        /// through a corner requires a standing lateral velocity of <c>DriftPerMeter * speedMps</c>.
        /// </summary>
        public float DriftPerMeter { get; }

        /// <summary>
        /// Distance from this sample to the end of its segment. A car that cannot fully match a
        /// corner's drift survives it by spending road width, and how much it can spend depends on
        /// how long it has to keep spending.
        /// </summary>
        public float SegmentRemainingM { get; }

        public float Center => (Left + Right) * 0.5f;
        public float HalfWidth => Math.Max(0.1f, (Right - Left) * 0.5f);
    }

    public readonly struct BotVehicleObservation
    {
        public BotVehicleObservation(uint id, bool isHuman, float positionX, float positionY, float speedKph, float widthM, float lengthM)
            : this(id, isHuman, positionX, positionY, speedKph, widthM, lengthM, 0f)
        {
        }

        public BotVehicleObservation(uint id, bool isHuman, float positionX, float positionY, float speedKph, float widthM, float lengthM, float lateralVelocityMps)
        {
            Id = id;
            IsHuman = isHuman;
            PositionX = positionX;
            PositionY = positionY;
            SpeedKph = Math.Max(0f, speedKph);
            WidthM = Math.Max(0.1f, widthM);
            LengthM = Math.Max(0.1f, lengthM);
            LateralVelocityMps = lateralVelocityMps;
        }

        public uint Id { get; }
        public bool IsHuman { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float SpeedKph { get; }
        public float WidthM { get; }
        public float LengthM { get; }
        public float LateralVelocityMps { get; }
    }

    /// <summary>Live motion state of the bot's own car.</summary>
    public readonly struct BotEgoState
    {
        public BotEgoState(float positionX, float positionY, float speedKph, float lateralVelocityMps, float yawRateRad, int gear)
            : this(positionX, positionY, speedKph, lateralVelocityMps, yawRateRad, gear, 0f)
        {
        }

        public BotEgoState(float positionX, float positionY, float speedKph, float lateralVelocityMps, float yawRateRad, int gear, float effectiveDriveRatio)
        {
            PositionX = positionX;
            PositionY = positionY;
            SpeedKph = Math.Max(0f, speedKph);
            LateralVelocityMps = lateralVelocityMps;
            YawRateRad = yawRateRad;
            Gear = gear < 1 ? 1 : gear;
            EffectiveDriveRatio = effectiveDriveRatio;
        }

        public float PositionX { get; }
        public float PositionY { get; }
        public float SpeedKph { get; }
        public float LateralVelocityMps { get; }
        public float YawRateRad { get; }
        public int Gear { get; }

        /// <summary>
        /// Live driveline ratio for CVT and other automatic families, or 0 for a plain gearbox.
        /// Without it the driver would size throttle off first gear on a CVT car and settle at a
        /// fraction of the acceleration it actually asked for.
        /// </summary>
        public float EffectiveDriveRatio { get; }
    }

    public readonly struct BotDrivingInput
    {
        public BotDrivingInput(
            BotDrivingDifficulty difficulty,
            uint seed,
            uint vehicleId,
            float elapsedSeconds,
            in BotEgoState ego,
            in BotCapabilities capabilities,
            BotRoadPreview[] road,
            BotVehicleObservation[] traffic)
        {
            Difficulty = difficulty;
            Seed = seed;
            VehicleId = vehicleId;
            ElapsedSeconds = Math.Max(0f, elapsedSeconds);
            Ego = ego;
            Capabilities = capabilities;
            Road = road ?? Array.Empty<BotRoadPreview>();
            Traffic = traffic ?? Array.Empty<BotVehicleObservation>();
        }

        public BotDrivingDifficulty Difficulty { get; }
        public uint Seed { get; }
        public uint VehicleId { get; }
        public float ElapsedSeconds { get; }
        public BotEgoState Ego { get; }
        public BotCapabilities Capabilities { get; }
        public BotRoadPreview[] Road { get; }
        public BotVehicleObservation[] Traffic { get; }

        public float PositionX => Ego.PositionX;
        public float PositionY => Ego.PositionY;
        public float SpeedKph => Ego.SpeedKph;
        public float SpeedMps => Ego.SpeedKph / 3.6f;
        public float TopSpeedKph => Capabilities.TopSpeedKph;
        public float WidthM => Capabilities.WidthM;
        public float LengthM => Capabilities.LengthM;
    }

    /// <summary>
    /// Everything the driver remembers between ticks. Plain struct so hosts can copy it
    /// freely; the scratch arrays are allocated once on first use.
    /// </summary>
    public struct BotDriverState
    {
        public bool Initialized;
        public uint RandomState;

        /// <summary>Seconds until the next heavy plan (speed profile + lane choice).</summary>
        public float PlanCooldownSeconds;

        /// <summary>Committed lateral target, expressed as an offset from the road center.</summary>
        public float TargetOffsetM;

        public float TargetSpeedKph;

        /// <summary>Stable per-bot line preference so a field of bots does not stack up.</summary>
        public float LaneBiasM;

        /// <summary>Seconds of post-contact calm-down left; overtaking is suppressed while positive.</summary>
        public float RecoverySecondsRemaining;

        public float LastPositionX;
        public float MeasuredLateralMps;
        public bool HasLastPosition;

        public float Throttle;
        public float Brake;
        public float Steering;

        public BotManeuver Maneuver;

        internal float[]? SpeedScratch;
        internal int[]? OrderScratch;
    }

    public readonly struct BotControlOutput
    {
        public BotControlOutput(float throttle, float brake, float steering, float targetSpeedKph, BotManeuver maneuver)
        {
            Throttle = Math.Max(0f, Math.Min(100f, throttle));
            Brake = Math.Max(-100f, Math.Min(0f, brake));
            Steering = Math.Max(-100f, Math.Min(100f, steering));
            TargetSpeedKph = Math.Max(0f, targetSpeedKph);
            Maneuver = maneuver;
        }

        public float Throttle { get; }
        public float Brake { get; }
        public float Steering { get; }
        public float TargetSpeedKph { get; }
        public BotManeuver Maneuver { get; }
        public bool Braking => Brake < -0.5f;
    }
}
