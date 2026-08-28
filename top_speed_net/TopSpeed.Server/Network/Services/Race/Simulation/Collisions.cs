using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Collision;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private const float FallbackWallHalfWidthMeters = RoadModel.DefaultLaneHalfWidth;

        private sealed class CollisionActor
        {
            public uint Id;
            public bool IsBot;
            public byte PlayerNumber;
            public PlayerConnection Player = null!;
            public RoomBot Bot = null!;
        }

        private static ulong MakePairKey(uint first, uint second)
        {
            if (first > second)
            {
                var swap = first;
                first = second;
                second = swap;
            }

            return ((ulong)first << 32) | second;
        }

        private void CheckForBumps()
        {
            foreach (var room in _rooms.Values)
            {
                if ((room.GameRulesFlags & (uint)RoomGameRules.GhostMode) != 0u)
                {
                    room.ActiveBumpPairs.Clear();
                    continue;
                }

                var roadModel = BuildRoadModel(room);
                var racers = room.PlayerIds.Where(id => _players.TryGetValue(id, out var p) && p.State == PlayerState.Racing)
                    .Select(id => _players[id]).ToList();
                var botRacers = room.Bots.Where(bot => bot.State == PlayerState.Racing).ToList();
                var actors = new List<CollisionActor>(racers.Count + botRacers.Count);
                var activePairs = new HashSet<ulong>();

                for (var i = 0; i < racers.Count; i++)
                {
                    var player = racers[i];
                    actors.Add(new CollisionActor
                    {
                        Id = player.Id,
                        IsBot = false,
                        PlayerNumber = player.PlayerNumber,
                        Player = player
                    });
                }

                for (var i = 0; i < botRacers.Count; i++)
                {
                    var bot = botRacers[i];
                    actors.Add(new CollisionActor
                    {
                        Id = bot.Id,
                        IsBot = true,
                        PlayerNumber = bot.PlayerNumber,
                        Bot = bot
                    });
                }

                for (var i = 0; i < actors.Count; i++)
                {
                    for (var j = i + 1; j < actors.Count; j++)
                    {
                        var first = actors[i];
                        var second = actors[j];
                        var firstBody = BuildCollisionBody(first);
                        var secondBody = BuildCollisionBody(second);
                        if (!VehicleCollisionResolver.TryResolve(firstBody, secondBody, out var response))
                            continue;

                        var pairKey = MakePairKey(first.Id, second.Id);
                        activePairs.Add(pairKey);
                        if (room.ActiveBumpPairs.Contains(pairKey))
                            continue;

                        ResolveRoadBounds(roadModel, firstBody.PositionY, out var firstLeft, out var firstRight);
                        ResolveRoadBounds(roadModel, secondBody.PositionY, out var secondLeft, out var secondRight);

                        var firstImpulse = CollisionWallConsequence.Apply(
                            firstBody,
                            response.First,
                            response,
                            firstLeft,
                            firstRight);
                        var secondImpulse = CollisionWallConsequence.Apply(
                            secondBody,
                            response.Second,
                            response,
                            secondLeft,
                            secondRight);

                        ApplyCollisionImpulse(first, firstImpulse);
                        ApplyCollisionImpulse(second, secondImpulse);

                        if (!first.IsBot && !second.IsBot)
                            _bumpEventsHumanHuman++;
                        else
                            _bumpEventsHumanBot++;
                    }
                }

                room.ActiveBumpPairs.RemoveWhere(key => !activePairs.Contains(key));
                foreach (var pairKey in activePairs)
                    room.ActiveBumpPairs.Add(pairKey);
            }
        }

        private static RoadModel? BuildRoadModel(GameRoom room)
        {
            var trackData = room.TrackData;
            if (trackData == null)
                return null;
            var defs = trackData.Definitions;
            if (defs == null || defs.Length == 0)
                return null;
            return new RoadModel(defs);
        }

        private static void ResolveRoadBounds(RoadModel? roadModel, float positionY, out float left, out float right)
        {
            if (roadModel == null)
            {
                left = -FallbackWallHalfWidthMeters;
                right = FallbackWallHalfWidthMeters;
                return;
            }

            var road = roadModel.At(positionY);
            if (road.Right <= road.Left)
            {
                left = -FallbackWallHalfWidthMeters;
                right = FallbackWallHalfWidthMeters;
                return;
            }

            left = road.Left;
            right = road.Right;
        }

        private static VehicleCollisionBody BuildCollisionBody(CollisionActor actor)
        {
            if (actor.IsBot)
            {
                return new VehicleCollisionBody(
                    actor.Bot.PositionX,
                    actor.Bot.PositionY,
                    actor.Bot.SpeedKph,
                    actor.Bot.WidthM,
                    actor.Bot.LengthM,
                    actor.Bot.PhysicsConfig.MassKg);
            }

            return new VehicleCollisionBody(
                actor.Player.PositionX,
                actor.Player.PositionY,
                actor.Player.Speed,
                actor.Player.WidthM,
                actor.Player.LengthM,
                actor.Player.MassKg);
        }

        private void ApplyCollisionImpulse(CollisionActor actor, in VehicleCollisionImpulse impulse)
        {
            if (actor.IsBot)
            {
                ApplyBotCollision(actor.Bot, impulse);
                TriggerBotHorn(actor.Bot, "bump", 0.2f);
                return;
            }

            SendStream(actor.Player, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
            {
                PlayerId = actor.Player.Id,
                PlayerNumber = actor.Player.PlayerNumber,
                BumpX = impulse.BumpX,
                BumpY = impulse.BumpY,
                SpeedDeltaKph = impulse.SpeedDeltaKph
            }), PacketStream.RaceEvent, PacketDeliveryKind.Sequenced);
        }

        private static void ApplyBotCollision(RoomBot bot, in VehicleCollisionImpulse impulse)
        {
            bot.PositionX += 2f * impulse.BumpX;
            bot.PositionY += impulse.BumpY;
            if (bot.PositionY < 0f)
                bot.PositionY = 0f;

            bot.SpeedKph += impulse.SpeedDeltaKph;
            if (bot.SpeedKph < 0f)
                bot.SpeedKph = 0f;

            var state = bot.PhysicsState;
            state.PositionX = bot.PositionX;
            state.PositionY = bot.PositionY;
            state.SpeedKph = bot.SpeedKph;
            bot.PhysicsState = state;

            // Contact throws the car sideways; let the driver settle before it goes hunting for a
            // gap again, otherwise one bump immediately collects the next car.
            var driverState = bot.DriverState;
            TopSpeed.Bots.BotDrivingPlanner.NotifyContact(ref driverState);
            bot.DriverState = driverState;
        }

    }
}

