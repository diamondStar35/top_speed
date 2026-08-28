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
        private void SimulateBotRaceStep(GameRoom room, RoomBot bot, RoadModel roadModel, float raceDistance, float deltaSeconds, BotVehicleObservation[] traffic)
        {
            var currentRoad = roadModel.At(bot.PositionY);
            RefreshBotRoadPreview(bot, roadModel, deltaSeconds);

            var driverState = bot.DriverState;
            var ego = new BotEgoState(
                bot.PositionX,
                bot.PositionY,
                bot.SpeedKph,
                bot.PhysicsState.LateralVelocityMps,
                bot.PhysicsState.YawRateRad,
                bot.PhysicsState.Gear,
                bot.PhysicsState.EffectiveDriveRatio);
            var capabilities = bot.Capabilities;
            var drivingInput = new BotDrivingInput(
                (BotDrivingDifficulty)bot.Difficulty,
                (uint)((bot.AddedOrder + 1) * 397 ^ (int)(bot.Id + 1u) * 7919),
                bot.Id,
                deltaSeconds,
                in ego,
                in capabilities,
                bot.RoadPreview,
                traffic);
            var control = BotDrivingPlanner.Step(ref driverState, in drivingInput);
            bot.DriverState = driverState;
            bot.Braking = control.Braking;

            var physicsState = bot.PhysicsState;
            physicsState.PositionX = bot.PositionX;
            physicsState.PositionY = bot.PositionY;
            physicsState.SpeedKph = bot.SpeedKph;
            if (physicsState.Gear <= 0)
                physicsState.Gear = 1;

            var physicsInput = new BotPhysicsInput(
                deltaSeconds,
                currentRoad.Surface,
                (int)Math.Round(control.Throttle),
                brake: (int)Math.Round(control.Brake),
                steering: (int)Math.Round(control.Steering),
                ambientTemperatureC: float.NaN,
                rainGain: 0f,
                stormGain: 0f,
                windGain: 0f);
            BotPhysics.Step(bot.PhysicsConfig, ref physicsState, in physicsInput);

            bot.PhysicsState = physicsState;
            bot.PositionX = physicsState.PositionX;
            bot.PositionY = physicsState.PositionY;
            bot.SpeedKph = physicsState.SpeedKph;
            bot.EngineFrequency = CalculateBotEngineFrequency(bot, out var inShiftBand);
            if (inShiftBand)
            {
                if (bot.BackfireArmed && _random.Next(5) == 0)
                {
                    bot.BackfirePulseSeconds = BotBackfirePulseSeconds;
                    bot.BackfireArmed = false;
                }
            }
            else
            {
                bot.BackfireArmed = true;
            }
            TryStartBotHorn(room, bot, raceDistance);

            var evalRoad = roadModel.At(bot.PositionY);
            var evalLaneHalfWidth = Math.Max(0.1f, Math.Abs(evalRoad.Right - evalRoad.Left) * 0.5f);
            var evalRelPos = BotRaceRules.CalculateRelativeLanePosition(bot.PositionX, evalRoad.Left, evalLaneHalfWidth);
            if (BotRaceRules.IsOutsideRoad(evalRelPos))
            {
                var center = BotRaceRules.RoadCenter(evalRoad.Left, evalRoad.Right);
                var fullCrash = BotRaceRules.IsFullCrash(physicsState.Gear, bot.SpeedKph);
                if (fullCrash)
                {
                    physicsState.PositionX = center;
                    physicsState.SpeedKph = 0f;
                    physicsState.Gear = 1;
                    physicsState.AutoShiftCooldownSeconds = 0f;
                    bot.PhysicsState = physicsState;
                    bot.PositionX = center;
                    bot.SpeedKph = 0f;
                    bot.EngineStartSecondsRemaining = 0f;
                    bot.StartDelaySeconds = 0f;
                    bot.RacePhase = BotRacePhase.Crashing;
                    bot.CrashRecoverySeconds = BotRaceRules.DefaultBotCrashRecoverySeconds;
                    bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                    bot.Horning = false;
                    bot.HornSecondsRemaining = 0f;
                    bot.Braking = false;
                    bot.BackfirePulseSeconds = 0f;
                    bot.BackfireArmed = true;
                    bot.DriverState = default;
                    bot.RoadPreviewRefreshSeconds = 0f;
                    _botCrashEvents++;
                    _logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Bot crashed: room={0}, bot={1}, number={2}, y={3:0.0}."),
                        room.Id,
                        bot.Id,
                        bot.PlayerNumber,
                        bot.PositionY));
                    _notify.ToRoom(room, PacketSerializer.WritePlayer(Command.PlayerCrashed, bot.Id, bot.PlayerNumber), PacketStream.RaceEvent);
                    return;
                }

                physicsState.PositionX = center;
                physicsState.SpeedKph /= 4f;
                bot.PhysicsState = physicsState;
                bot.PositionX = center;
                bot.SpeedKph = Math.Max(0f, physicsState.SpeedKph);
            }

            if (bot.PositionY < raceDistance)
                return;

            _race.ResolveBotFinish(room, bot, raceDistance, out _);
            _botFinishEvents++;
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Bot finished: room={0}, bot={1}, number={2}, place={3}."),
                room.Id,
                bot.Id,
                bot.PlayerNumber,
                room.RaceResults.Count));
            _race.UpdateStopState(room);
        }

        /// <summary>
        /// The sample under the car is refreshed every tick because the steering loop reads the
        /// corridor from it; the lookahead ladder only needs the planner's cadence.
        /// </summary>
        private static void RefreshBotRoadPreview(RoomBot bot, RoadModel roadModel, float deltaSeconds)
        {
            bot.RoadPreviewRefreshSeconds -= deltaSeconds;
            var rebuildLadder = bot.RoadPreviewRefreshSeconds <= 0f;
            if (rebuildLadder)
            {
                bot.RoadPreviewRefreshSeconds = BotRoadSampling.RefreshIntervalSeconds;
                BotRoadSampling.FillDistances(bot.SpeedKph, bot.RoadPreviewDistances);
            }

            var count = rebuildLadder ? bot.RoadPreview.Length : 1;
            for (var i = 0; i < count; i++)
            {
                var distance = bot.RoadPreviewDistances[i];
                var sample = roadModel.At(bot.PositionY + distance);
                bot.RoadPreview[i] = new BotRoadPreview(
                    distance,
                    sample.Left,
                    sample.Right,
                    sample.Surface,
                    sample.Type,
                    roadModel.CenterDriftPerMeter(sample.Type),
                    Math.Max(1f, sample.Length - sample.RelPos));
            }
        }
    }
}



