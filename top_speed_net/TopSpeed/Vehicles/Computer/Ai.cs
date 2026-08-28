using System;
using TopSpeed.Bots;
using TopSpeed.Common;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private void AI(float elapsed, BotVehicleObservation[] traffic)
        {
            RefreshRoadPreview(elapsed);

            var ego = new BotEgoState(_positionX, _positionY, _speed, _lateralVelocityMps, _yawRateRad, _gear, _effectiveDriveRatio);
            var input = new BotDrivingInput(
                (BotDrivingDifficulty)_difficulty,
                (uint)((_playerNumber + 1) * 397 ^ (_random + 1) * 7919),
                (uint)_playerNumber,
                elapsed,
                in ego,
                in _capabilities,
                _drivingRoadPreview,
                traffic);
            var control = BotDrivingPlanner.Step(ref _driverState, in input);
            _currentThrottle = (int)Math.Round(control.Throttle);
            _currentBrake = (int)Math.Round(control.Brake);
            _currentSteering = (int)Math.Round(control.Steering);
        }

        /// <summary>
        /// The sample under the car is refreshed every tick because the steering loop reads the
        /// corridor from it; the lookahead ladder only needs the planner's cadence.
        /// </summary>
        private void RefreshRoadPreview(float elapsed)
        {
            _drivingPreviewRefreshSeconds -= elapsed;
            var rebuildLadder = _drivingPreviewRefreshSeconds <= 0f;
            if (rebuildLadder)
            {
                _drivingPreviewRefreshSeconds = BotRoadSampling.RefreshIntervalSeconds;
                BotRoadSampling.FillDistances(_speed, _drivingPreviewDistances);
            }

            var count = rebuildLadder ? _drivingRoadPreview.Length : 1;
            for (var i = 0; i < count; i++)
            {
                var distance = _drivingPreviewDistances[i];
                var sample = _track.RoadComputer(_positionY + distance);
                _drivingRoadPreview[i] = new BotRoadPreview(
                    distance,
                    sample.Left,
                    sample.Right,
                    sample.Surface,
                    sample.Type,
                    sample.DriftPerMeter,
                    sample.SegmentRemainingM);
            }
        }

        private void Horn()
        {
            if (_hornCooldownSeconds > 0f)
                return;
            var duration = Algorithm.RandomInt(80);
            _hornCooldownSeconds = 2f;
            PushEvent(BotEventType.StartHorn, 0.3f);
            PushEvent(BotEventType.StopHorn, 0.5f + duration / 80.0f);
        }
    }
}
