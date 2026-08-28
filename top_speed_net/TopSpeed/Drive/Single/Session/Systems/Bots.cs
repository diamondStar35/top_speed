using System;
using System.Collections.Generic;
using TopSpeed.Bots;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Single.Session.Systems
{
    internal sealed class Bots : TopSpeed.Drive.Session.Subsystem
    {
        private readonly ComputerPlayer?[] _players;
        private readonly int _playerCount;
        private readonly int _humanPlayerNumber;
        private readonly Vehicles.ICar _car;
        private readonly Tracks.Track _track;
        private readonly int _lapLimit;
        private readonly Func<int> _readRaceTimeMs;
        private readonly Action _updatePositions;
        private readonly Action<int, int> _recordFinish;
        private readonly Action<int> _announceFinishOrder;
        private readonly Func<bool> _checkFinish;
        private readonly Func<bool> _includeHumanInTraffic;
        private readonly Action<float> _queueFinish;

        public Bots(
            string name,
            int order,
            ComputerPlayer?[] players,
            int playerCount,
            int humanPlayerNumber,
            Vehicles.ICar car,
            Tracks.Track track,
            int lapLimit,
            Func<int> readRaceTimeMs,
            Action updatePositions,
            Action<int, int> recordFinish,
            Action<int> announceFinishOrder,
            Func<bool> checkFinish,
            Func<bool> includeHumanInTraffic,
            Action<float> queueFinish)
            : base(name, order)
        {
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _playerCount = playerCount;
            _humanPlayerNumber = humanPlayerNumber;
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _lapLimit = lapLimit;
            _readRaceTimeMs = readRaceTimeMs ?? throw new ArgumentNullException(nameof(readRaceTimeMs));
            _updatePositions = updatePositions ?? throw new ArgumentNullException(nameof(updatePositions));
            _recordFinish = recordFinish ?? throw new ArgumentNullException(nameof(recordFinish));
            _announceFinishOrder = announceFinishOrder ?? throw new ArgumentNullException(nameof(announceFinishOrder));
            _checkFinish = checkFinish ?? throw new ArgumentNullException(nameof(checkFinish));
            _includeHumanInTraffic = includeHumanInTraffic ?? throw new ArgumentNullException(nameof(includeHumanInTraffic));
            _queueFinish = queueFinish ?? throw new ArgumentNullException(nameof(queueFinish));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            _updatePositions();
            var traffic = BuildTrafficSnapshot();

            for (var botIndex = 0; botIndex < _playerCount; botIndex++)
            {
                var bot = _players[botIndex];
                if (bot == null)
                    continue;

                bot.Run(elapsed, _car.PositionX, _car.PositionY, traffic);
                if (_track.Lap(bot.PositionY) <= _lapLimit || bot.Finished)
                    continue;

                bot.StopAtFinish();
                _recordFinish(bot.PlayerNumber, _readRaceTimeMs());
                _announceFinishOrder(bot.PlayerNumber);
                if (_checkFinish())
                    _queueFinish(context.ProgressSeconds);
            }
        }

        private BotVehicleObservation[] BuildTrafficSnapshot()
        {
            var traffic = new List<BotVehicleObservation>(_playerCount + 1);
            if (_includeHumanInTraffic())
            {
                traffic.Add(new BotVehicleObservation(
                    (uint)_humanPlayerNumber,
                    isHuman: true,
                    _car.PositionX,
                    _car.PositionY,
                    _car.Speed,
                    _car.WidthM,
                    _car.LengthM,
                    _car.LateralVelocityMps));
            }

            for (var i = 0; i < _playerCount; i++)
            {
                var bot = _players[i];
                if (bot == null || bot.Finished)
                    continue;
                traffic.Add(new BotVehicleObservation(
                    (uint)bot.PlayerNumber,
                    isHuman: false,
                    bot.PositionX,
                    bot.PositionY,
                    bot.Speed,
                    bot.WidthM,
                    bot.LengthM,
                    bot.LateralVelocityMps));
            }

            return traffic.ToArray();
        }
    }
}
