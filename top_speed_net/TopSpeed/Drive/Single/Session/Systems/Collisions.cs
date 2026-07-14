using System;
using System.Collections.Generic;
using System.Reflection;
using TopSpeed.Collision;
using TopSpeed.Data;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Single.Session.Systems
{
    internal sealed class Collisions : TopSpeed.Drive.Session.Subsystem
    {
        private const float FallbackWallHalfWidthMeters = RoadModel.DefaultLaneHalfWidth;
        private static readonly FieldInfo? TrackRoadModelField =
            typeof(Tracks.Track).GetField("_roadModel", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly struct Actor
        {
            public Actor(uint id, bool isPlayer, ComputerPlayer? bot)
            {
                Id = id;
                IsPlayer = isPlayer;
                Bot = bot;
            }

            public uint Id { get; }
            public bool IsPlayer { get; }
            public ComputerPlayer? Bot { get; }
        }

        private readonly Tracks.Track _track;
        private readonly Vehicles.ICar _car;
        private readonly ComputerPlayer?[] _players;
        private readonly Func<int> _getPlayerNumber;
        private readonly Func<int> _getPlayerCount;
        private readonly Func<bool> _isInPitStop;
        private readonly HashSet<ulong> _activePairs = new HashSet<ulong>();

        public Collisions(
            string name,
            int order,
            Tracks.Track track,
            Vehicles.ICar car,
            ComputerPlayer?[] players,
            Func<int> getPlayerNumber,
            Func<int> getPlayerCount,
            Func<bool> isInPitStop)
            : base(name, order)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _getPlayerNumber = getPlayerNumber ?? throw new ArgumentNullException(nameof(getPlayerNumber));
            _getPlayerCount = getPlayerCount ?? throw new ArgumentNullException(nameof(getPlayerCount));
            _isInPitStop = isInPitStop ?? throw new ArgumentNullException(nameof(isInPitStop));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            var roadModel = ResolveRoadModel();
            var actors = new List<Actor>(_getPlayerCount() + 1);
            var activePairs = new HashSet<ulong>();

            if (_car.State == Vehicles.CarState.Running && !_isInPitStop())
                actors.Add(new Actor((uint)_getPlayerNumber(), isPlayer: true, bot: null));

            for (var i = 0; i < _getPlayerCount(); i++)
            {
                var bot = _players[i];
                if (bot == null)
                    continue;

                if (bot.State == ComputerPlayer.ComputerState.Running && !bot.Finished)
                    actors.Add(new Actor((uint)bot.PlayerNumber, isPlayer: false, bot: bot));
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
                    if (_activePairs.Contains(pairKey))
                        continue;

                    ResolveRoadBounds(roadModel, firstBody.PositionY, out var firstLeft, out var firstRight);
                    ResolveRoadBounds(roadModel, secondBody.PositionY, out var secondLeft, out var secondRight);

                    var firstImpulse = CollisionWallConsequence.Apply(firstBody, response.First, response, firstLeft, firstRight);
                    var secondImpulse = CollisionWallConsequence.Apply(secondBody, response.Second, response, secondLeft, secondRight);

                    ApplyCollisionImpulse(first, firstImpulse);
                    ApplyCollisionImpulse(second, secondImpulse);
                }
            }

            _activePairs.RemoveWhere(key => !activePairs.Contains(key));
            foreach (var pairKey in activePairs)
                _activePairs.Add(pairKey);
        }

        public void Reset()
        {
            _activePairs.Clear();
        }

        private RoadModel? ResolveRoadModel()
        {
            if (TrackRoadModelField == null)
                return null;

            return TrackRoadModelField.GetValue(_track) as RoadModel;
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

        private VehicleCollisionBody BuildCollisionBody(in Actor actor)
        {
            if (actor.IsPlayer)
            {
                return new VehicleCollisionBody(
                    _car.PositionX,
                    _car.PositionY,
                    _car.Speed,
                    _car.WidthM,
                    _car.LengthM,
                    _car.MassKg);
            }

            var bot = actor.Bot!;
            return new VehicleCollisionBody(
                bot.PositionX,
                bot.PositionY,
                bot.Speed,
                bot.WidthM,
                bot.LengthM,
                bot.MassKg);
        }

        private void ApplyCollisionImpulse(in Actor actor, in VehicleCollisionImpulse impulse)
        {
            if (actor.IsPlayer)
            {
                _car.Bump(impulse.BumpX, impulse.BumpY, impulse.SpeedDeltaKph);
                return;
            }

            actor.Bot!.Bump(impulse.BumpX, impulse.BumpY, impulse.SpeedDeltaKph);
        }
    }
}
