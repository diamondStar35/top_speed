using TopSpeed.Protocol;

namespace TopSpeed.Server.Bots
{
    internal enum BotDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    internal enum BotRacePhase
    {
        Normal = 0,
        Crashing = 1,
        Restarting = 2
    }

    internal readonly struct BotAudioProfile
    {
        public BotAudioProfile(int idleFrequency, int topFrequency, int shiftFrequency)
        {
            IdleFrequency = idleFrequency;
            TopFrequency = topFrequency;
            ShiftFrequency = shiftFrequency;
        }

        public int IdleFrequency { get; }
        public int TopFrequency { get; }
        public int ShiftFrequency { get; }
    }

    internal sealed class RoomBot
    {
        public uint Id { get; set; }
        public byte PlayerNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public BotDifficulty Difficulty { get; set; }
        public int AddedOrder { get; set; }
        public CarType Car { get; set; } = CarType.Vehicle1;
        public bool AutomaticTransmission { get; set; } = true;
        public PlayerState State { get; set; } = PlayerState.NotReady;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float SpeedKph { get; set; }
        public float StartDelaySeconds { get; set; }
        public float EngineStartSecondsRemaining { get; set; }
        public float WidthM { get; set; } = 1.8f;
        public float LengthM { get; set; } = 4.5f;
        public TopSpeed.Bots.BotPhysicsState PhysicsState { get; set; }

        private TopSpeed.Bots.BotPhysicsConfig _physicsConfig = TopSpeed.Bots.BotPhysicsCatalog.Get(CarType.Vehicle1);

        public TopSpeed.Bots.BotPhysicsConfig PhysicsConfig
        {
            get => _physicsConfig;
            set
            {
                _physicsConfig = value;
                Capabilities = TopSpeed.Bots.BotCapabilities.From(value);
            }
        }

        /// <summary>Planner view of the car's physical envelope; kept in sync with PhysicsConfig.</summary>
        public TopSpeed.Bots.BotCapabilities Capabilities { get; private set; } =
            TopSpeed.Bots.BotCapabilities.From(TopSpeed.Bots.BotPhysicsCatalog.Get(CarType.Vehicle1));
        public BotAudioProfile AudioProfile { get; set; } = new BotAudioProfile(22050, 55000, 26000);
        public int EngineFrequency { get; set; } = 22050;
        public bool Horning { get; set; }
        public float HornSecondsRemaining { get; set; }
        public float HornCooldownSeconds { get; set; }
        public bool Braking { get; set; }
        public bool BackfireArmed { get; set; } = true;
        public float BackfirePulseSeconds { get; set; }
        public BotRacePhase RacePhase { get; set; } = BotRacePhase.Normal;
        public float CrashRecoverySeconds { get; set; }
        public TopSpeed.Bots.BotDriverState DriverState { get; set; }
        public TopSpeed.Bots.BotRoadPreview[] RoadPreview { get; } = TopSpeed.Bots.BotRoadSampling.CreatePreview();
        public float[] RoadPreviewDistances { get; } = TopSpeed.Bots.BotRoadSampling.CreateDistances();
        public float RoadPreviewRefreshSeconds { get; set; }
    }
}
