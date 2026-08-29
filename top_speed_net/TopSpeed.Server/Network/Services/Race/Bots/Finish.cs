using TopSpeed.Bots;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal static class ServerBotFinish
    {
        /// <summary>
        /// Retires a bot that has crossed the line. The car keeps its speed and rolls to a halt
        /// under <see cref="BotRacePhase.Stopping"/>; freezing it here instead would drop every
        /// listener's copy of it from racing speed to silence in a single snapshot.
        /// </summary>
        public static void BeginStop(RoomBot bot, float finishY)
        {
            bot.State = PlayerState.Finished;
            bot.RacePhase = BotRacePhase.Stopping;
            if (bot.PositionY < finishY)
                bot.PositionY = finishY;
            bot.Horning = false;
            bot.HornSecondsRemaining = 0f;
            bot.HornCooldownSeconds = 0f;
            bot.Braking = false;
            bot.DriverState = default;
            bot.BackfirePulseSeconds = 0f;
            bot.BackfireArmed = true;
            bot.CrashRecoverySeconds = 0f;
            bot.StartDelaySeconds = 0f;
            bot.EngineStartSecondsRemaining = 0f;

            var state = bot.PhysicsState;
            state.PositionX = bot.PositionX;
            state.PositionY = bot.PositionY;
            state.SpeedKph = bot.SpeedKph;
            bot.PhysicsState = state;
        }

        /// <summary>Called once the car has come to rest; the engine note dies from here.</summary>
        public static void CompleteStop(RoomBot bot)
        {
            bot.RacePhase = BotRacePhase.Normal;
            bot.SpeedKph = 0f;
            bot.Braking = false;
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            bot.PhysicsState = new BotPhysicsState
            {
                PositionX = bot.PositionX,
                PositionY = bot.PositionY,
                SpeedKph = 0f,
                Gear = 1,
                AutoShiftCooldownSeconds = 0f,
                TireWearFraction = 0f,
                TireTemperatureC = float.NaN,
                TireTreadTemperatureC = float.NaN,
                TireCarcassTemperatureC = float.NaN,
                TireSmoothedInputs = default,
                SurfaceTemperatureC = float.NaN
            };
        }
    }
}
