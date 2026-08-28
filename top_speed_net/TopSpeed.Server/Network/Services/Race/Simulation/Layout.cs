using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static float GetLapDistance(GameRoom room)
        {
            return RaceDistanceRules.CalculateLapDistance(room.TrackData?.Definitions);
        }

        private float GetLaneHalfWidth(GameRoom room)
        {
            if (room.TrackData == null || room.TrackData.Definitions == null || room.TrackData.Definitions.Length == 0)
                return RoadModel.DefaultLaneHalfWidth;

            var model = new RoadModel(room.TrackData.Definitions, RoadModel.DefaultLaneHalfWidth);
            var startRoad = model.At(BotRaceRules.StartLineY);
            var laneHalfWidth = Math.Abs(startRoad.Right - startRoad.Left) * 0.5f;
            return laneHalfWidth > 0f ? laneHalfWidth : RoadModel.DefaultLaneHalfWidth;
        }

        private float GetStartRowSpacing(GameRoom room)
        {
            var maxLength = 4.5f;

            foreach (var playerId in room.PlayerIds)
            {
                if (_players.TryGetValue(playerId, out var player))
                    maxLength = Math.Max(maxLength, player.LengthM);
            }

            for (var i = 0; i < room.Bots.Count; i++)
                maxLength = Math.Max(maxLength, room.Bots[i].LengthM);

            return BotRaceRules.CalculateStartRowSpacing(maxLength);
        }

        private static float CalculateStartX(int gridIndex, float vehicleWidth, float laneHalfWidth)
        {
            return BotRaceRules.CalculateStartX(gridIndex, vehicleWidth, laneHalfWidth);
        }

        private static float CalculateStartY(int gridIndex, float rowSpacing)
        {
            return BotRaceRules.CalculateStartY(gridIndex, rowSpacing);
        }

        private static PacketPlayerData ToBotPacket(RoomBot bot)
        {
            return new PacketPlayerData
            {
                PlayerId = bot.Id,
                PlayerNumber = bot.PlayerNumber,
                Car = bot.Car,
                RaceData = new PlayerRaceData
                {
                    PositionX = bot.PositionX,
                    PositionY = bot.PositionY,
                    Speed = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, (int)Math.Round(bot.SpeedKph))),
                    Frequency = bot.EngineFrequency > 0 ? bot.EngineFrequency : bot.AudioProfile.IdleFrequency
                },
                State = bot.State,
                EngineRunning = (bot.State == PlayerState.Racing && bot.RacePhase == BotRacePhase.Normal)
                    || bot.EngineStartSecondsRemaining > 0f,
                Braking = bot.Braking,
                Horning = bot.Horning,
                Backfiring = bot.BackfirePulseSeconds > 0f,
                MediaLoaded = false,
                MediaPlaying = false,
                MediaId = 0,
                RadioVolumePercent = 100
            };
        }

        private static float GetRaceDistance(GameRoom room)
        {
            return RaceDistanceRules.CalculateRaceDistance(
                room.TrackData?.Definitions,
                room.Laps,
                room.TrackData?.Laps ?? 0);
        }

    }
}

