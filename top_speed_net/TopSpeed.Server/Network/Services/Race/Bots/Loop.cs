using System;
using System.Collections.Generic;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void UpdateBots(float deltaSeconds)
        {
            foreach (var room in _rooms.Values)
            {
                if (!room.RaceStarted)
                    continue;
                if (room.RacePaused)
                    continue;
                if (room.TrackData == null)
                    continue;

                var definitions = room.TrackData.Definitions;
                if (definitions == null || definitions.Length == 0)
                    continue;

                var laneHalfWidth = GetLaneHalfWidth(room);
                var roadModel = new RoadModel(definitions, laneHalfWidth);
                var raceDistance = GetRaceDistance(room);
                if (roadModel.LapDistance <= 0f || raceDistance <= 0f)
                    continue;

                var traffic = BuildBotTrafficSnapshot(room);

                foreach (var bot in room.Bots)
                {
                    UpdateBotSignals(bot, deltaSeconds);

                    if (bot.State == PlayerState.Finished)
                    {
                        // A finished bot is still a car on the road until it stops rolling.
                        if (bot.RacePhase == BotRacePhase.Stopping)
                            SimulateBotStoppingStep(bot, roadModel, deltaSeconds);
                        continue;
                    }

                    if (bot.State == PlayerState.NotReady)
                        continue;

                    if (TryAdvanceAwaitingStart(room, bot, deltaSeconds))
                        continue;

                    if (bot.State != PlayerState.Racing)
                        continue;

                    if (TryAdvanceCrashPhase(room, bot, deltaSeconds))
                        continue;
                    if (TryAdvanceRestartPhase(room, bot, deltaSeconds))
                        continue;

                    SimulateBotRaceStep(room, bot, roadModel, raceDistance, deltaSeconds, traffic);
                }
            }
        }

        private BotVehicleObservation[] BuildBotTrafficSnapshot(GameRoom room)
        {
            var traffic = new List<BotVehicleObservation>(room.PlayerIds.Count + room.Bots.Count);
            foreach (var id in room.PlayerIds)
            {
                if (!_players.TryGetValue(id, out var player) || player.State != PlayerState.Racing)
                    continue;
                traffic.Add(new BotVehicleObservation(
                    player.Id,
                    isHuman: true,
                    player.PositionX,
                    player.PositionY,
                    player.Speed,
                    player.WidthM,
                    player.LengthM));
            }

            for (var i = 0; i < room.Bots.Count; i++)
            {
                var bot = room.Bots[i];
                if (bot.State != PlayerState.Racing)
                    continue;
                traffic.Add(new BotVehicleObservation(
                    bot.Id,
                    isHuman: false,
                    bot.PositionX,
                    bot.PositionY,
                    bot.SpeedKph,
                    bot.WidthM,
                    bot.LengthM,
                    bot.PhysicsState.LateralVelocityMps));
            }
            return traffic.ToArray();
        }

    }
}
