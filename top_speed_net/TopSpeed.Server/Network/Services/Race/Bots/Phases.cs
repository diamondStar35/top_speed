using System;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static void UpdateBotSignals(RoomBot bot, float deltaSeconds)
        {
            if (bot.HornCooldownSeconds > 0f)
                bot.HornCooldownSeconds = Math.Max(0f, bot.HornCooldownSeconds - deltaSeconds);
            if (bot.BackfirePulseSeconds > 0f)
            {
                bot.BackfirePulseSeconds -= deltaSeconds;
                if (bot.BackfirePulseSeconds < 0f)
                    bot.BackfirePulseSeconds = 0f;
            }

            if (bot.HornSecondsRemaining > 0f)
            {
                bot.HornSecondsRemaining -= deltaSeconds;
                if (bot.HornSecondsRemaining <= 0f)
                {
                    bot.HornSecondsRemaining = 0f;
                    bot.Horning = false;
                }
            }
        }

        private bool TryAdvanceAwaitingStart(GameRoom room, RoomBot bot, float deltaSeconds)
        {
            if (bot.State != PlayerState.AwaitingStart)
                return false;

            if (bot.StartDelaySeconds > 0f)
            {
                bot.StartDelaySeconds -= deltaSeconds;
                if (bot.StartDelaySeconds > 0f)
                    return true;
                bot.StartDelaySeconds = 0f;
            }

            if (bot.EngineStartSecondsRemaining <= 0f)
            {
                bot.EngineStartSecondsRemaining = BotRaceRules.DefaultBotEngineStartSeconds;
                bot.SpeedKph = 0f;
                bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                return true;
            }

            bot.EngineStartSecondsRemaining -= deltaSeconds;
            if (bot.EngineStartSecondsRemaining > 0f)
            {
                bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                return true;
            }

            bot.EngineStartSecondsRemaining = 0f;
            bot.State = PlayerState.Racing;
            bot.RacePhase = BotRacePhase.Normal;
            bot.CrashRecoverySeconds = 0f;
            bot.SpeedKph = 0f;
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            bot.BackfireArmed = true;
            bot.Braking = false;
            _botStartEvents++;
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Bot started racing: room={0}, bot={1}, number={2}."),
                room.Id,
                bot.Id,
                bot.PlayerNumber));
            return false;
        }

        private bool TryAdvanceCrashPhase(GameRoom room, RoomBot bot, float deltaSeconds)
        {
            if (bot.RacePhase != BotRacePhase.Crashing)
                return false;

            bot.CrashRecoverySeconds -= deltaSeconds;
            bot.SpeedKph = 0f;
            var crashState = bot.PhysicsState;
            crashState.SpeedKph = 0f;
            crashState.Gear = 1;
            crashState.AutoShiftCooldownSeconds = 0f;
            bot.PhysicsState = crashState;
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            bot.Horning = false;
            bot.HornSecondsRemaining = 0f;
            bot.BackfirePulseSeconds = 0f;
            bot.BackfireArmed = true;
            bot.Braking = false;
            if (bot.CrashRecoverySeconds > 0f)
                return true;

            bot.CrashRecoverySeconds = 0f;
            bot.RacePhase = BotRacePhase.Restarting;
            bot.StartDelaySeconds = BotRaceRules.DefaultBotRestartDelaySeconds;
            bot.EngineStartSecondsRemaining = 0f;
            _botRestartEvents++;
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Bot restarting after crash: room={0}, bot={1}, number={2}, restartDelay={3:0.00}s, startDelay={4:0.00}s."),
                room.Id,
                bot.Id,
                bot.PlayerNumber,
                BotRaceRules.DefaultBotRestartDelaySeconds,
                BotRaceRules.DefaultBotEngineStartSeconds));
            return true;
        }

        private bool TryAdvanceRestartPhase(GameRoom room, RoomBot bot, float deltaSeconds)
        {
            if (bot.RacePhase != BotRacePhase.Restarting)
                return false;

            bot.SpeedKph = 0f;
            var restartState = bot.PhysicsState;
            restartState.SpeedKph = 0f;
            restartState.Gear = 1;
            restartState.AutoShiftCooldownSeconds = 0f;
            bot.PhysicsState = restartState;
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            bot.Horning = false;
            bot.HornSecondsRemaining = 0f;
            bot.BackfirePulseSeconds = 0f;
            bot.BackfireArmed = true;
            bot.Braking = false;
            if (bot.StartDelaySeconds > 0f)
            {
                bot.StartDelaySeconds -= deltaSeconds;
                if (bot.StartDelaySeconds > 0f)
                    return true;
                bot.StartDelaySeconds = 0f;
            }

            if (bot.EngineStartSecondsRemaining <= 0f)
            {
                bot.EngineStartSecondsRemaining = BotRaceRules.DefaultBotEngineStartSeconds;
                return true;
            }

            bot.EngineStartSecondsRemaining -= deltaSeconds;
            if (bot.EngineStartSecondsRemaining > 0f)
                return true;

            bot.EngineStartSecondsRemaining = 0f;
            bot.RacePhase = BotRacePhase.Normal;
            _botResumeEvents++;
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Bot recovered and resumed: room={0}, bot={1}, number={2}."),
                room.Id,
                bot.Id,
                bot.PlayerNumber));
            return true;
        }
    }
}

