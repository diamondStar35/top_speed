using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Bots;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Live;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer : IDisposable
    {
        public ComputerPlayer(
            AudioManager audio,
            RaceAudioFactory raceAudio,
            Track track,
            DriveSettings settings,
            int vehicleIndex,
            int playerNumber,
            Func<float> currentTime,
            Func<bool> started,
            Action<string>? debugSpeak = null,
            string? customVehicleFile = null)
        {
            if (raceAudio == null)
                throw new ArgumentNullException(nameof(raceAudio));
            _track = track;
            _settings = settings;
            _playerNumber = playerNumber;
            _vehicleIndex = vehicleIndex;
            _currentTime = currentTime;
            _started = started;
            _debugSpeak = debugSpeak;
            _events = new List<BotEvent>();
            _radio = new VehicleRadioController(audio);
            _liveRadio = new LiveRadio(audio, settings);

            _surface = TrackSurface.Asphalt;
            _gear = 1;
            _state = ComputerState.Stopped;
            _horning = false;
            _difficulty = (int)settings.Difficulty;
            _prevFrequency = 0;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _laneWidth = 0;
            _relPos = 0f;
            _nextRelPos = 0f;
            _diffX = 0;
            _diffY = 0;
            _currentSteering = 0;
            _currentThrottle = 0;
            _currentBrake = 0;
            _speedDiff = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _speed = 0;
            _frame = 1;
            _finished = false;
            _random = Algorithm.RandomInt(100);
            _networkBackfireActive = false;
            _remoteEngineStartPending = false;
            _remoteEngineStartRemaining = 0f;
            _remoteEnginePendingFrequency = _idleFreq;
            _crashLateralAnchored = false;
            _crashLateralFromCenter = 0f;
            _radioLoaded = false;
            _radioPlaying = false;
            _radioMediaId = 0;

            VehicleDefinition definition;
            if (!string.IsNullOrWhiteSpace(customVehicleFile))
            {
                try
                {
                    definition = VehicleLoader.LoadCustom(customVehicleFile!, track.Weather);
                }
                catch
                {
                    // Fall back to a built-in vehicle if the downloaded package fails to load.
                    definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
                }
            }
            else
            {
                definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
            }
            _surfaceTractionFactor = definition.SurfaceTractionFactor;
            _topSpeed = definition.TopSpeed;
            var torqueCurve = PowertrainProfileBuilder.Build(definition);
            var build = PowertrainBuild.Create(
                new BuildInput(
                    massKg: definition.MassKg,
                    drivetrainEfficiency: definition.DrivetrainEfficiency,
                    engineBrakingTorqueNm: definition.EngineBrakingTorqueNm,
                    tireGripCoefficient: definition.TireGripCoefficient,
                    brakeStrength: definition.BrakeStrength,
                    wheelRadiusM: definition.TireCircumferenceM / (2.0f * (float)Math.PI),
                    engineBraking: definition.EngineBraking,
                    idleRpm: definition.IdleRpm,
                    revLimiter: definition.RevLimiter,
                    finalDriveRatio: definition.FinalDriveRatio,
                    powerFactor: definition.PowerFactor,
                    peakTorqueNm: definition.PeakTorqueNm,
                    peakTorqueRpm: definition.PeakTorqueRpm,
                    idleTorqueNm: definition.IdleTorqueNm,
                    redlineTorqueNm: definition.RedlineTorqueNm,
                    dragCoefficient: definition.DragCoefficient,
                    frontalAreaM2: definition.FrontalAreaM2,
                    sideAreaM2: definition.SideAreaM2,
                    rollingResistanceCoefficient: definition.RollingResistanceCoefficient,
                    rollingResistanceSpeedFactor: definition.RollingResistanceSpeedFactor,
                    wheelSideDragBaseN: definition.WheelSideDragBaseN,
                    wheelSideDragLinearNPerMps: definition.WheelSideDragLinearNPerMps,
                    launchRpm: definition.LaunchRpm,
                    reversePowerFactor: definition.ReversePowerFactor,
                    reverseGearRatio: definition.ReverseGearRatio,
                    reverseMaxSpeedKph: definition.ReverseMaxSpeedKph,
                    engineInertiaKgm2: definition.EngineInertiaKgm2,
                    engineFrictionTorqueNm: definition.EngineFrictionTorqueNm,
                    drivelineCouplingRate: definition.DrivelineCouplingRate,
                    gears: definition.Gears,
                    torqueCurve: torqueCurve,
                    gearRatios: definition.GearRatios,
                    coupledDrivelineDragNm: definition.CoupledDrivelineDragNm,
                    coupledDrivelineViscousDragNmPerKrpm: definition.CoupledDrivelineViscousDragNmPerKrpm,
                    frictionLinearNmPerKrpm: definition.EngineFrictionLinearNmPerKrpm,
                    frictionQuadraticNmPerKrpm2: definition.EngineFrictionQuadraticNmPerKrpm2,
                    idleControlWindowRpm: definition.IdleControlWindowRpm,
                    idleControlGainNmPerRpm: definition.IdleControlGainNmPerRpm,
                    minCoupledRiseIdleRpmPerSecond: definition.MinCoupledRiseIdleRpmPerSecond,
                    minCoupledRiseFullRpmPerSecond: definition.MinCoupledRiseFullRpmPerSecond,
                    engineOverrunIdleLossFraction: definition.EngineOverrunIdleLossFraction,
                    overrunCurveExponent: definition.OverrunCurveExponent,
                    engineBrakeTransferEfficiency: definition.EngineBrakeTransferEfficiency));

            _massKg = build.Powertrain.MassKg;
            _drivetrainEfficiency = build.Powertrain.DrivetrainEfficiency;
            _engineBrakingTorqueNm = build.Powertrain.EngineBrakingTorqueNm;
            _tireGripCoefficient = build.Powertrain.TireGripCoefficient;
            _brakeStrength = build.Powertrain.BrakeStrength;
            _wheelRadiusM = build.Powertrain.WheelRadiusM;
            _engineBraking = build.Powertrain.EngineBraking;
            _idleRpm = build.Powertrain.IdleRpm;
            _revLimiter = build.Powertrain.RevLimiter;
            _finalDriveRatio = build.Powertrain.FinalDriveRatio;
            _powerFactor = build.Powertrain.PowerFactor;
            _peakTorqueNm = build.Powertrain.PeakTorqueNm;
            _peakTorqueRpm = build.Powertrain.PeakTorqueRpm;
            _idleTorqueNm = build.Powertrain.IdleTorqueNm;
            _redlineTorqueNm = build.Powertrain.RedlineTorqueNm;
            _dragCoefficient = build.Powertrain.DragCoefficient;
            _frontalAreaM2 = build.Powertrain.FrontalAreaM2;
            _sideAreaM2 = build.Powertrain.SideAreaM2;
            _rollingResistanceCoefficient = build.Powertrain.RollingResistanceCoefficient;
            _rollingResistanceSpeedFactor = build.Powertrain.RollingResistanceSpeedFactor;
            _wheelSideDragBaseN = build.Powertrain.WheelSideDragBaseN;
            _wheelSideDragLinearNPerMps = build.Powertrain.WheelSideDragLinearNPerMps;
            _launchRpm = build.Powertrain.LaunchRpm;
            _coupledDrivelineDragNm = build.Powertrain.CoupledDrivelineDragNm;
            _coupledDrivelineViscousDragNmPerKrpm = build.Powertrain.CoupledDrivelineViscousDragNmPerKrpm;
            _engineInertiaKgm2 = build.Powertrain.EngineInertiaKgm2;
            _engineFrictionTorqueNm = build.Powertrain.EngineFrictionTorqueNm;
            _drivelineCouplingRate = build.Powertrain.DrivelineCouplingRate;
            _activeTransmissionType = definition.PrimaryTransmissionType;
            _automaticTuning = definition.AutomaticTuning;
            _automaticCouplingFactor = 1f;
            _cvtRatio = _automaticTuning.Cvt.RatioMax;
            _effectiveDriveRatio = 0f;
            _lateralGripCoefficient = Math.Max(0.1f, definition.LateralGripCoefficient);
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, definition.HighSpeedStability));
            _wheelbaseM = Math.Max(0.5f, definition.WheelbaseM);
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, definition.MaxSteerDeg));
            _widthM = Math.Max(0.5f, definition.WidthM);
            _lengthM = Math.Max(0.5f, definition.LengthM);
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _pitchCurveExponent = VehicleDefinition.ClampPitchCurveExponent(definition.PitchCurveExponent);
            _gears = build.Powertrain.Gears;
            _steering = definition.Steering;
            _frequency = _idleFreq;

            _engine = new EngineModel(
                _idleRpm,
                definition.MaxRpm,
                _revLimiter,
                definition.AutoShiftRpm,
                _engineBraking,
                _topSpeed,
                _finalDriveRatio,
                definition.TireCircumferenceM,
                _gears,
                build.GearRatios,
                _peakTorqueNm,
                _peakTorqueRpm,
                _idleTorqueNm,
                _redlineTorqueNm,
                _engineBrakingTorqueNm,
                _powerFactor,
                _engineInertiaKgm2,
                _engineFrictionTorqueNm,
                _drivelineCouplingRate,
                torqueCurve,
                engineOverrunIdleLossFraction: build.EngineOverrunIdleLossFraction,
                engineFrictionLinearNmPerKrpm: build.FrictionLinearNmPerKrpm,
                engineFrictionQuadraticNmPerKrpm2: build.FrictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: build.IdleControlWindowRpm,
                idleControlGainNmPerRpm: build.IdleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: build.MinCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: build.MinCoupledRiseFullRpmPerSecond,
                overrunCurveExponent: build.OverrunCurveExponent);

            _physicsConfig = new BotPhysicsConfig(
                _surfaceTractionFactor,
                _topSpeed,
                _massKg,
                _drivetrainEfficiency,
                _engineBrakingTorqueNm,
                _tireGripCoefficient,
                _brakeStrength,
                _wheelRadiusM,
                _engineBraking,
                _idleRpm,
                _revLimiter,
                _finalDriveRatio,
                _powerFactor,
                _peakTorqueNm,
                _peakTorqueRpm,
                _idleTorqueNm,
                _redlineTorqueNm,
                _dragCoefficient,
                _frontalAreaM2,
                _sideAreaM2,
                _rollingResistanceCoefficient,
                _rollingResistanceSpeedFactor,
                build.WheelSideDragBaseN,
                build.WheelSideDragLinearNPerMps,
                _launchRpm,
                definition.ReversePowerFactor,
                definition.ReverseGearRatio,
                _engineInertiaKgm2,
                _engineFrictionTorqueNm,
                _drivelineCouplingRate,
                _lateralGripCoefficient,
                _highSpeedStability,
                _wheelbaseM,
                _widthM,
                _lengthM,
                _maxSteerDeg,
                _steering,
                definition.HighSpeedSteerGain,
                definition.HighSpeedSteerStartKph,
                definition.HighSpeedSteerFullKph,
                definition.CombinedGripPenalty,
                definition.SlipAnglePeakDeg,
                definition.SlipAngleFalloff,
                definition.TurnResponse,
                definition.MassSensitivity,
                definition.DownforceGripGain,
                definition.CornerStiffnessFront,
                definition.CornerStiffnessRear,
                definition.YawInertiaScale,
                definition.SteeringCurve,
                definition.TransientDamping,
                _gears,
                torqueCurve,
                build.GearRatios,
                definition.TransmissionPolicy,
                definition.PrimaryTransmissionType,
                definition.AutomaticTuning,
                coupledDrivelineDragNm: build.CoupledDrivelineDragNm,
                coupledDrivelineViscousDragNmPerKrpm: build.CoupledDrivelineViscousDragNmPerKrpm,
                frictionLinearNmPerKrpm: build.FrictionLinearNmPerKrpm,
                frictionQuadraticNmPerKrpm2: build.FrictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: build.IdleControlWindowRpm,
                idleControlGainNmPerRpm: build.IdleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: build.MinCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: build.MinCoupledRiseFullRpmPerSecond,
                engineOverrunIdleLossFraction: build.EngineOverrunIdleLossFraction,
                overrunCurveExponent: build.OverrunCurveExponent,
                engineBrakeTransferEfficiency: build.EngineBrakeTransferEfficiency);

            _raceAudio = raceAudio.CreateRemote(definition);
            BindAudio(_raceAudio);
            ConfigureAudioState();
            RefreshCategoryVolumes(force: true);
        }
    }
}


