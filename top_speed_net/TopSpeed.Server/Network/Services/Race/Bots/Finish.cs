using TopSpeed.Bots;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal static class ServerBotFinish
    {
        public static void StopMotion(RoomBot bot, float finishY)
        {
            bot.State = PlayerState.Finished;
            bot.PositionY = finishY;
            bot.SpeedKph = 0f;
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            bot.Horning = false;
            bot.HornSecondsRemaining = 0f;
            bot.BackfirePulseSeconds = 0f;
            bot.BackfireArmed = true;
            bot.RacePhase = BotRacePhase.Normal;
            bot.CrashRecoverySeconds = 0f;
            bot.StartDelaySeconds = 0f;
            bot.EngineStartSecondsRemaining = 0f;
            bot.PhysicsState = new BotPhysicsState
            {
                PositionX = bot.PositionX,
                PositionY = bot.PositionY,
                SpeedKph = 0f,
                Gear = 1,
                AutoShiftCooldownSeconds = 0f,
                TireWearFraction = 0f,
                TireTemperatureC = float.NaN,
                TireCarcassTemperatureC = float.NaN,
                TireSmoothedSlipNormalized = 0f,
                SurfaceTemperatureC = float.NaN
            };
        }
    }
}
