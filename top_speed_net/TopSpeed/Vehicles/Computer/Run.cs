using TopSpeed.Audio;
using TopSpeed.Bots;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Surface;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private const float ParkingHoldBrakeInput = 1f;

        public void Run(float elapsed, float playerX, float playerY)
        {
            RefreshCategoryVolumes();
            if (_positionY < 0f)
                _positionY = 0f;

            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;

            if (!_horning && _diffY < -100.0f)
            {
                if (Algorithm.RandomInt(2500) == 1)
                {
                    var duration = Algorithm.RandomInt(80);
                    _horning = true;
                    PushEvent(BotEventType.StopHorn, 0.2f + (duration / 80.0f));
                }
            }

            if (_state == ComputerState.Running && _started())
            {
                AI();
                if (_currentBrake != 0 && _surface == TrackSurface.Asphalt)
                {
                    if (!_soundBrake.IsPlaying)
                        _soundBrake.Play(loop: true);
                }
                else if (_soundBrake.IsPlaying)
                {
                    _soundBrake.Stop();
                }

                var beforeSpeed = _speed;
                var physicsState = new BotPhysicsState
                {
                    PositionX = _positionX,
                    PositionY = _positionY,
                    SpeedKph = _speed,
                    LateralVelocityMps = _lateralVelocityMps,
                    YawRateRad = _yawRateRad,
                    Gear = _gear,
                    AutoShiftCooldownSeconds = _autoShiftCooldown,
                    AutomaticCouplingFactor = _automaticCouplingFactor,
                    CvtRatio = _cvtRatio,
                    EffectiveDriveRatio = _effectiveDriveRatio,
                    TireWearFraction = _tireWearFraction,
                    TireTemperatureC = _tireTemperatureC,
                    TireTreadTemperatureC = _tireTreadTemperatureC,
                    TireCarcassTemperatureC = _tireCarcassTemperatureC,
                    TireSmoothedSlipNormalized = _tireSmoothedSlipNormalized,
                    SurfaceTemperatureC = _surfaceTemperatureC
                };
                var weather = _track.GetActiveWeatherProfile();
                var physicsInput = new BotPhysicsInput(
                    elapsed,
                    _surface,
                    _currentThrottle,
                    _currentBrake,
                    _currentSteering,
                    ambientTemperatureC: weather.TemperatureC,
                    rainGain: weather.RainGain,
                    stormGain: weather.StormGain,
                    windGain: weather.WindGain);
                BotPhysics.Step(_physicsConfig, ref physicsState, physicsInput);

                _positionX = physicsState.PositionX;
                _positionY = physicsState.PositionY;
                _speed = physicsState.SpeedKph;
                _lateralVelocityMps = physicsState.LateralVelocityMps;
                _yawRateRad = physicsState.YawRateRad;
                _gear = physicsState.Gear;
                _autoShiftCooldown = physicsState.AutoShiftCooldownSeconds;
                _automaticCouplingFactor = physicsState.AutomaticCouplingFactor;
                _cvtRatio = physicsState.CvtRatio;
                _effectiveDriveRatio = physicsState.EffectiveDriveRatio;
                _tireWearFraction = physicsState.TireWearFraction;
                _tireTemperatureC = physicsState.TireTemperatureC;
                _tireTreadTemperatureC = physicsState.TireTreadTemperatureC;
                _tireCarcassTemperatureC = physicsState.TireCarcassTemperatureC;
                _tireSmoothedSlipNormalized = physicsState.TireSmoothedSlipNormalized;
                _surfaceTemperatureC = physicsState.SurfaceTemperatureC;
                _speedDiff = _speed - beforeSpeed;

                var driveRatioOverride = _effectiveDriveRatio > 0f ? _effectiveDriveRatio : (float?)null;
                var syncState = EngineStateRuntime.Resolve(
                    new EngineStateRuntimeInput(
                        _physicsConfig.Powertrain,
                        _activeTransmissionType,
                        isNeutralGear: false,
                        combustionEnabled: true,
                        engineStalled: false,
                        drivelineLocked: _automaticCouplingFactor >= 0.98f,
                        drivelineDisengaged: _automaticCouplingFactor <= 0.05f,
                        _speed / 3.6f,
                        System.Math.Max(0f, System.Math.Min(100f, _currentThrottle)) / 100f,
                        _automaticCouplingFactor,
                        switchingGear: 0,
                        _engine.Rpm,
                        Calculator.RpmAtSpeed(
                            _physicsConfig.Powertrain,
                            _speed / 3.6f,
                            _gear,
                            driveRatioOverride)));

                _engine.SyncFromSpeed(
                    _speed,
                    _gear,
                    elapsed,
                    _currentThrottle,
                    inReverse: false,
                    couplingMode: (EngineCouplingMode)syncState.CouplingMode,
                    couplingFactor: _automaticCouplingFactor,
                    driveRatioOverride: driveRatioOverride,
                    minimumCoupledRpm: syncState.MinimumCoupledRpm);
                UpdateEngineFreq();

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    var speedRatio = NormalizeSpeedByTopSpeed(_speed, 1f);
                    _brakeFrequency = (int)(11025 + (22050 * speedRatio));
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                }

                var road = _track.RoadComputer(_positionY);
                if (!_finished)
                    Evaluate(road);
            }
            else if (_state == ComputerState.Stopping)
            {
                var surface = SurfaceModel.Resolve(_surface, _surfaceTractionFactor);
                var longitudinal = LongitudinalStep.Compute(
                    new LongitudinalStepInput(
                        _physicsConfig.Powertrain,
                        elapsed,
                        System.Math.Max(0f, _speed / 3.6f),
                        throttle: 0f,
                        brake: ParkingHoldBrakeInput,
                        surfaceTractionModifier: 1f,
                        surfaceBrakeModifier: surface.Brake > 0f ? surface.Brake : 1f,
                        surfaceRollingResistanceModifier: surface.RollingResistance > 0f ? surface.RollingResistance : 1f,
                        longitudinalGripFactor: 1f,
                        _gear,
                        inReverse: false,
                        isNeutral: false,
                        transmissionType: _activeTransmissionType,
                        drivelineCouplingFactor: _automaticCouplingFactor,
                        creepAccelerationMps2: 0f,
                        currentEngineRpm: _engine.Rpm,
                        requestDrive: false,
                        requestBrake: true,
                        applyEngineBraking: false,
                        resistanceEnvironment: ResistanceEnvironment.Calm,
                        driveRatioOverride: _effectiveDriveRatio > 0f ? _effectiveDriveRatio : (float?)null));
                _speed = System.Math.Max(0f, _speed + longitudinal.SpeedDeltaKph);
                _speedDiff = longitudinal.SpeedDeltaKph;
                UpdateEngineFreq();
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                }
                if (_speed <= 0.05f)
                {
                    _speed = 0f;
                    _speedDiff = 0f;
                    _state = ComputerState.Stopped;
                    _soundEngine.Stop();
                }
                _frame++;
            }

            if (_horning && _state == ComputerState.Running)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else
            {
                if (_soundHorn.IsPlaying)
                    _soundHorn.Stop();
            }

            if (_crashLateralAnchored && !_soundCrash.IsPlaying)
                _crashLateralAnchored = false;

            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time < _currentTime())
                {
                    _events.RemoveAt(i);
                    switch (e.Type)
                    {
                        case BotEventType.CarStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} engine start.");
                            _soundEngine.SetFrequency(_idleFreq);
                            _soundEngine.Play(loop: true);
                            _state = ComputerState.Running;
                            break;
                        case BotEventType.CarComputerStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarComputerStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} start trigger.");
                            Start();
                            break;
                        case BotEventType.CarRestart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarRestart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} restart trigger.");
                            Start();
                            break;
                        case BotEventType.StopHorn:
                            _horning = false;
                            break;
                        case BotEventType.StartHorn:
                            _horning = true;
                            break;
                    }
                }
            }

            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);
        }

        public void Evaluate(Track.Road road)
        {
            if (_state == ComputerState.Running && _started())
            {
                if (_frame % 4 == 0)
                {
                    var laneHalfWidth = System.Math.Max(0.1f, System.Math.Abs(road.Right - road.Left) * 0.5f);
                    _relPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, road.Left, laneHalfWidth);
                    if (BotRaceRules.IsOutsideRoad(_relPos))
                    {
                        var fullCrash = BotRaceRules.IsFullCrash(_gear, _speed);
                        if (fullCrash)
                            Crash(BotRaceRules.RoadCenter(road.Left, road.Right));
                        else
                            MiniCrash(BotRaceRules.RoadCenter(road.Left, road.Right));
                    }
                }
            }

            _surface = road.Surface;
            _frame++;
        }

        private void PushEvent(BotEventType type, float time)
        {
            _events.Add(new BotEvent { Type = type, Time = _currentTime() + time });
        }
    }
}
