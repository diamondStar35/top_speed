using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
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
        private void TryStartBotHorn(GameRoom room, RoomBot bot, float raceDistance)
        {
            if (bot.Horning || bot.HornSecondsRemaining > 0f)
                return;
            if (raceDistance <= 0f)
                return;

            foreach (var id in room.PlayerIds)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;
                if (player.State != PlayerState.Racing && player.State != PlayerState.Finished)
                    continue;

                var delta = bot.PositionY - player.PositionY;
                if (delta < -BotHornMinDistanceMeters)
                {
                    if (_random.Next(2500) == 0)
                        TriggerBotHorn(bot, "overtake", 0.2f);
                    return;
                }
            }
        }

        private void TriggerBotHorn(RoomBot bot, string reason, float minDurationSeconds = 0.2f)
        {
            if (bot.HornCooldownSeconds > 0f)
                return;
            var duration = minDurationSeconds + (_random.Next(80) / 80.0f);
            if (duration <= bot.HornSecondsRemaining)
                return;

            bot.Horning = true;
            bot.HornSecondsRemaining = duration;
            bot.HornCooldownSeconds = 2f;
            if (string.Equals(reason, "overtake", StringComparison.Ordinal))
                _botHornOvertakeEvents++;
            else if (string.Equals(reason, "bump", StringComparison.Ordinal))
                _botHornBumpEvents++;

            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Bot horn triggered: bot={0}, number={1}, reason={2}, duration={3:0.00}s."),
                bot.Id,
                bot.PlayerNumber,
                reason,
                duration));
        }

        private static int CalculateBotEngineFrequency(RoomBot bot, out bool inShiftBand)
        {
            inShiftBand = false;
            var speedKph = Math.Max(0f, bot.SpeedKph);
            var config = bot.PhysicsConfig;
            var profile = bot.AudioProfile;

            var gearForSound = GetGearForSpeed(config, speedKph);
            if (!TryGetGearBand(config, gearForSound, out var gearMinKph, out var gearRangeKph))
                return profile.IdleFrequency;

            int frequency;
            if (gearForSound <= 1)
            {
                var gearSpeed = gearRangeKph <= 0f ? 0f : Math.Min(1.0f, speedKph / gearRangeKph);
                frequency = (int)(gearSpeed * (profile.TopFrequency - profile.IdleFrequency)) + profile.IdleFrequency;
            }
            else
            {
                var gearSpeed = (speedKph - gearMinKph) / gearRangeKph;
                if (gearSpeed < 0.07f)
                {
                    inShiftBand = true;
                    frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (profile.TopFrequency - profile.ShiftFrequency) + profile.ShiftFrequency);
                }
                else
                {
                    if (gearSpeed > 1.0f)
                        gearSpeed = 1.0f;
                    frequency = (int)(gearSpeed * (profile.TopFrequency - profile.ShiftFrequency) + profile.ShiftFrequency);
                }
            }

            var minFrequency = Math.Max(1000, profile.IdleFrequency / 2);
            var maxFrequency = Math.Max(profile.TopFrequency, profile.TopFrequency * 2);
            if (frequency < minFrequency)
                frequency = minFrequency;
            if (frequency > maxFrequency)
                frequency = maxFrequency;
            return frequency;
        }

        private static int GetGearForSpeed(BotPhysicsConfig config, float speedKph)
        {
            var speedMps = Math.Max(0f, speedKph / 3.6f);
            var topSpeedMps = config.TopSpeedKph / 3.6f;
            var autoShiftRpm = config.IdleRpm + ((config.RevLimiter - config.IdleRpm) * 0.92f);
            for (var gear = 1; gear <= config.Gears; gear++)
            {
                var rpm = gear == config.Gears ? config.RevLimiter : autoShiftRpm;
                var gearMax = Math.Min(SpeedMpsFromRpm(config, rpm, gear), topSpeedMps);
                if (speedMps <= gearMax + 0.01f)
                    return gear;
            }

            return config.Gears;
        }

        private static bool TryGetGearBand(BotPhysicsConfig config, int gear, out float minSpeedKph, out float rangeKph)
        {
            minSpeedKph = 0f;
            rangeKph = 0f;

            if (config.Gears <= 0)
                return false;

            var clampedGear = gear;
            if (clampedGear < 1)
                clampedGear = 1;
            if (clampedGear > config.Gears)
                clampedGear = config.Gears;

            var maxSpeedMps = SpeedMpsFromRpm(config, config.RevLimiter, clampedGear);
            var shiftRpm = config.IdleRpm + ((config.RevLimiter - config.IdleRpm) * 0.35f);
            var minSpeedMps = clampedGear == 1 ? 0f : SpeedMpsFromRpm(config, shiftRpm, clampedGear);
            minSpeedKph = minSpeedMps * 3.6f;
            rangeKph = Math.Max(0.1f, (maxSpeedMps - minSpeedMps) * 3.6f);
            return true;
        }

        private static float SpeedMpsFromRpm(BotPhysicsConfig config, float rpm, int gear)
        {
            var ratio = config.GetGearRatio(gear) * config.FinalDriveRatio;
            if (ratio <= 0f)
                return 0f;

            var tireCircumference = config.WheelRadiusM * 2f * (float)Math.PI;
            return (rpm / ratio) * (tireCircumference / 60f);
        }

    }
}

