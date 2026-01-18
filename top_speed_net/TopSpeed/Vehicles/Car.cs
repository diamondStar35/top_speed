using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Tracks.Geometry;
using TopSpeed.Tracks.Map;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed class Car : IDisposable
    {
        private const int MaxSurfaceFreq = 100000;
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float CrashVibrationSeconds = 1.5f;
        private const float BumpVibrationSeconds = 0.2f;
        private const float AutoShiftHysteresis = 0.05f;
        private const float AutoShiftCooldownSeconds = 0.15f;
        private const float YieldSpeedKph = 10.0f;
        private static bool s_stickReleased;

        private readonly AudioManager _audio;
        private readonly MapTrack _track;
        private readonly RaceInput _input;
        private readonly RaceSettings _settings;
        private readonly Func<float> _currentTime;
        private readonly Func<bool> _started;
        private readonly string _legacyRoot;
        private readonly string _effectsRoot;
        private readonly List<CarEvent> _events;

        private CarState _state;
        private TrackSurface _surface;
        private int _gear;
        private float _speed;
        private float _positionX;
        private float _positionY;
        private MapMovementState _mapState;
        private bool _manualTransmission;
        private bool _backfirePlayed;
        private bool _backfirePlayedAuto;
        private int _hasWipers;
        private int _switchingGear;
        private float _autoShiftCooldown;
        private CarType _carType;
        private ICarListener? _listener;
        private string? _customFile;
        private bool _userDefined;

        private float _surfaceTractionFactor;
        private float _deceleration;
        private float _topSpeed;
        private float _massKg;
        private float _drivetrainEfficiency;
        private float _engineBrakingTorqueNm;
        private float _tireGripCoefficient;
        private float _brakeStrength;
        private float _wheelRadiusM;
        private float _engineBraking;
        private float _idleRpm;
        private float _revLimiter;
        private float _finalDriveRatio;
        private float _powerFactor;
        private VehiclePowertrainParameters _powertrainParams;
        private float _peakTorqueNm;
        private float _peakTorqueRpm;
        private float _idleTorqueNm;
        private float _redlineTorqueNm;
        private float _dragCoefficient;
        private float _frontalAreaM2;
        private float _rollingResistanceCoefficient;
        private float _launchRpm;
        private float _lastDriveRpm;
        private float _lateralGripCoefficient;
        private float _highSpeedStability;
        private float _wheelbaseM;
        private float _maxSteerDeg;
        private VehicleDynamicsModel _dynamicsModel;
        private VehicleDynamicsState _dynamicsState;
        private VehicleDynamicsParameters _dynamicsParams;
        private BicycleDynamicsParameters _bicycleParams;
        private float _widthM;
        private float _lengthM;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private int _gears;
        private float _thrust;
        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private int _prevSurfaceFrequency;
        private int _surfaceFrequency;
        private float _laneWidth;
        private float _relPos;
        private int _panPos;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private float _currentSurfaceTractionFactor;
        private float _currentDeceleration;
        private float _speedDiff;
        private int _factor1;
        private int _frame;
        private float _prevThrottleVolume;
        private float _throttleVolume;
        private float _lastAudioX;
        private float _lastAudioY;
        private Vector3 _worldPosition;
        private Vector3 _worldForward;
        private Vector3 _worldUp;
        private Vector3 _worldVelocity;
        private bool _audioInitialized;
        private float _lastAudioElapsed;
        private float _lastHeadingDegrees;
        private float _turnTickAccumulator;
        private bool _turnTickInitialized;

        private AudioSourceHandle _soundEngine;
        private AudioSourceHandle? _soundThrottle;
        private AudioSourceHandle _soundHorn;
        private AudioSourceHandle _soundStart;
        private AudioSourceHandle _soundBrake;
        private AudioSourceHandle _soundCrash;
        private AudioSourceHandle _soundMiniCrash;
        private AudioSourceHandle _soundAsphalt;
        private AudioSourceHandle _soundGravel;
        private AudioSourceHandle _soundWater;
        private AudioSourceHandle _soundSand;
        private AudioSourceHandle _soundSnow;
        private AudioSourceHandle? _soundWipers;
        private AudioSourceHandle _soundBump;
        private AudioSourceHandle _soundBadSwitch;
        private AudioSourceHandle? _soundBackfire;
        private AudioSourceHandle? _soundTurnTick;

        private readonly IVibrationDevice? _vibration;

        private EngineModel _engine;

        public Car(
            AudioManager audio,
            MapTrack track,
            RaceInput input,
            RaceSettings settings,
            int vehicleIndex,
            string? vehicleFile,
            Func<float> currentTime,
            Func<bool> started,
            IVibrationDevice? vibrationDevice = null)
        {
            _audio = audio;
            _track = track;
            _input = input;
            _settings = settings;
            _currentTime = currentTime;
            _started = started;
            _legacyRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _effectsRoot = Path.Combine(AssetPaths.Root, "Effects");
            _events = new List<CarEvent>();

            _surface = track.InitialSurface;
            _gear = 1;
            _state = CarState.Stopped;
            _manualTransmission = false;
            _hasWipers = 0;
            _switchingGear = 0;
            _speed = 0;
            _frame = 1;
            _throttleVolume = 0.0f;
            _prevThrottleVolume = 0.0f;
            _prevFrequency = 0;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _prevSurfaceFrequency = 0;
            _surfaceFrequency = 0;
            _laneWidth = track.LaneWidth * 2;
            _relPos = 0f;
            _panPos = 0;
            _currentSteering = 0;
            _currentThrottle = 0;
            _currentBrake = 0;
            _currentSurfaceTractionFactor = 0;
            _currentDeceleration = 0;
            _speedDiff = 0;
            _factor1 = 100;

            VehicleDefinition definition;
            if (string.IsNullOrWhiteSpace(vehicleFile))
            {
                definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
                _carType = definition.CarType;
            }
            else
            {
                definition = VehicleLoader.LoadCustom(vehicleFile!, track.Weather);
                _carType = definition.CarType;
                _customFile = definition.CustomFile;
                _userDefined = true;
            }

            _dynamicsModel = definition.DynamicsModel;
            VehicleName = definition.Name;
            _surfaceTractionFactor = Math.Max(0.01f, SanitizeFinite(definition.SurfaceTractionFactor, 0.01f));
            _deceleration = Math.Max(0.01f, SanitizeFinite(definition.Deceleration, 0.01f));
            _topSpeed = Math.Max(1f, SanitizeFinite(definition.TopSpeed, 1f));
            _massKg = Math.Max(1f, SanitizeFinite(definition.MassKg, 1f));
            _drivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, SanitizeFinite(definition.DrivetrainEfficiency, 0.85f)));
            _engineBrakingTorqueNm = Math.Max(0f, SanitizeFinite(definition.EngineBrakingTorqueNm, 0f));
            _tireGripCoefficient = Math.Max(0.1f, SanitizeFinite(definition.TireGripCoefficient, 0.1f));
            _brakeStrength = Math.Max(0.1f, SanitizeFinite(definition.BrakeStrength, 0.1f));
            _wheelRadiusM = Math.Max(0.01f, SanitizeFinite(definition.TireCircumferenceM, 0f) / (2.0f * (float)Math.PI));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, SanitizeFinite(definition.EngineBraking, 0.3f)));
            _idleRpm = Math.Max(0f, SanitizeFinite(definition.IdleRpm, 0f));
            _revLimiter = Math.Max(_idleRpm, SanitizeFinite(definition.RevLimiter, _idleRpm));
            _finalDriveRatio = Math.Max(0.1f, SanitizeFinite(definition.FinalDriveRatio, 0.1f));
            _powerFactor = Math.Max(0.1f, SanitizeFinite(definition.PowerFactor, 0.1f));
            _peakTorqueNm = Math.Max(0f, SanitizeFinite(definition.PeakTorqueNm, 0f));
            _peakTorqueRpm = Math.Max(_idleRpm + 100f, SanitizeFinite(definition.PeakTorqueRpm, _idleRpm + 100f));
            _idleTorqueNm = Math.Max(0f, SanitizeFinite(definition.IdleTorqueNm, 0f));
            _redlineTorqueNm = Math.Max(0f, SanitizeFinite(definition.RedlineTorqueNm, 0f));
            _dragCoefficient = Math.Max(0.01f, SanitizeFinite(definition.DragCoefficient, 0.01f));
            _frontalAreaM2 = Math.Max(0.1f, SanitizeFinite(definition.FrontalAreaM2, 0.1f));
            _rollingResistanceCoefficient = Math.Max(0.001f, SanitizeFinite(definition.RollingResistanceCoefficient, 0.001f));
            _launchRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, SanitizeFinite(definition.LaunchRpm, _idleRpm)));
            _lateralGripCoefficient = Math.Max(0.1f, SanitizeFinite(definition.LateralGripCoefficient, 0.1f));
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, SanitizeFinite(definition.HighSpeedStability, 0f)));
            _wheelbaseM = Math.Max(0.5f, SanitizeFinite(definition.WheelbaseM, 0.5f));
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, SanitizeFinite(definition.MaxSteerDeg, 35f)));
            _widthM = Math.Max(0.5f, SanitizeFinite(definition.WidthM, 0.5f));
            _lengthM = Math.Max(0.5f, SanitizeFinite(definition.LengthM, 0.5f));
            var dynamicsSetup = VehicleDynamicsSetupBuilder.Build(
                definition,
                _massKg,
                _tireGripCoefficient,
                _lateralGripCoefficient,
                _dragCoefficient,
                _frontalAreaM2,
                _rollingResistanceCoefficient,
                _topSpeed,
                _wheelbaseM,
                _maxSteerDeg,
                _widthM,
                _lengthM);
            _dynamicsParams = dynamicsSetup.FourWheel;
            _bicycleParams = dynamicsSetup.Bicycle;
            _powertrainParams = new VehiclePowertrainParameters
            {
                MassKg = _massKg,
                WheelRadiusM = _wheelRadiusM,
                FinalDriveRatio = _finalDriveRatio,
                DrivetrainEfficiency = _drivetrainEfficiency,
                EngineBrakingTorqueNm = _engineBrakingTorqueNm,
                EngineBraking = _engineBraking,
                IdleRpm = _idleRpm,
                RevLimiter = _revLimiter,
                LaunchRpm = _launchRpm,
                PowerFactor = _powerFactor,
                PeakTorqueNm = _peakTorqueNm,
                PeakTorqueRpm = _peakTorqueRpm,
                IdleTorqueNm = _idleTorqueNm,
                RedlineTorqueNm = _redlineTorqueNm,
                TireGripCoefficient = _tireGripCoefficient,
                BrakeStrength = _brakeStrength,
                DragCoefficient = _dragCoefficient,
                FrontalAreaM2 = _frontalAreaM2,
                RollingResistanceCoefficient = _rollingResistanceCoefficient
            };
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _gears = Math.Max(1, definition.Gears);
            _frequency = _idleFreq;

            // Initialize engine model
            _engine = new EngineModel(
                definition.IdleRpm,
                definition.MaxRpm,
                definition.RevLimiter,
                definition.AutoShiftRpm,
                definition.EngineBraking,
                definition.TopSpeed,
                definition.FinalDriveRatio,
                definition.TireCircumferenceM,
                definition.Gears,
                definition.GearRatios);

            _soundEngine = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Engine), looped: true, allowHrtf: true);
            _soundStart = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Start));
            _soundHorn = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Horn), looped: true);
            _soundThrottle = TryCreateSound(definition.GetSoundPath(VehicleAction.Throttle), looped: true, allowHrtf: true);
            _soundCrash = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Crash));
            _soundBrake = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Brake), looped: true);
            _soundBackfire = TryCreateSound(definition.GetSoundPath(VehicleAction.Backfire));

            if (definition.HasWipers == 1)
                _hasWipers = 1;

            if (_hasWipers == 1)
                _soundWipers = CreateRequiredSound(Path.Combine(_legacyRoot, "wipers.wav"), looped: true);

            _soundAsphalt = CreateRequiredSound(Path.Combine(_legacyRoot, "asphalt.wav"), looped: true, allowHrtf: false);
            _soundGravel = CreateRequiredSound(Path.Combine(_legacyRoot, "gravel.wav"), looped: true, allowHrtf: false);
            _soundWater = CreateRequiredSound(Path.Combine(_legacyRoot, "water.wav"), looped: true, allowHrtf: false);
            _soundSand = CreateRequiredSound(Path.Combine(_legacyRoot, "sand.wav"), looped: true, allowHrtf: false);
            _soundSnow = CreateRequiredSound(Path.Combine(_legacyRoot, "snow.wav"), looped: true, allowHrtf: false);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"));
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"));
            _soundBadSwitch = CreateRequiredSound(Path.Combine(_legacyRoot, "badswitch.wav"));
            _soundTurnTick = TryCreateSound(Path.Combine(_legacyRoot, "turn.wav"), spatialize: false);

            _soundCrash.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundWipers?.SetDopplerFactor(0f);

            _vibration = vibrationDevice != null && vibrationDevice.IsAvailable && vibrationDevice.ForceFeedbackCapable && _settings.ForceFeedback && _settings.UseJoystick
                ? vibrationDevice
                : null;
            if (_vibration != null)
            {
                _vibration.LoadEffect(VibrationEffectType.Start, Path.Combine(_effectsRoot, "carstart.ffe"));
                _vibration.LoadEffect(VibrationEffectType.Crash, Path.Combine(_effectsRoot, "crash.ffe"));
                _vibration.LoadEffect(VibrationEffectType.Spring, Path.Combine(_effectsRoot, "spring.ffe"));
                _vibration.LoadEffect(VibrationEffectType.Engine, Path.Combine(_effectsRoot, "engine.ffe"));
                _vibration.LoadEffect(VibrationEffectType.CurbLeft, Path.Combine(_effectsRoot, "curbleft.ffe"));
                _vibration.LoadEffect(VibrationEffectType.CurbRight, Path.Combine(_effectsRoot, "curbright.ffe"));
                _vibration.LoadEffect(VibrationEffectType.Gravel, Path.Combine(_effectsRoot, "gravel.ffe"));
                _vibration.LoadEffect(VibrationEffectType.BumpLeft, Path.Combine(_effectsRoot, "bumpleft.ffe"));
                _vibration.LoadEffect(VibrationEffectType.BumpRight, Path.Combine(_effectsRoot, "bumpright.ffe"));
                _vibration.Gain(VibrationEffectType.Gravel, 0);
            }

            _soundEngine.SetDopplerFactor(0f);
            _soundThrottle?.SetDopplerFactor(0f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundAsphalt.SetDopplerFactor(0f);
            _soundGravel.SetDopplerFactor(0f);
            _soundWater.SetDopplerFactor(0f);
            _soundSand.SetDopplerFactor(0f);
            _soundSnow.SetDopplerFactor(0f);
        }

        public CarState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float Speed => _speed;
        public float HeadingRadians => _dynamicsState.Yaw;
        public float HeadingDegrees => NormalizeDegrees(_dynamicsState.Yaw * 180.0f / (float)Math.PI);
        public float FrontWheelAngleDegrees => _dynamicsState.SteerWheelAngleDeg;
        public Vector3 WorldPosition => _worldPosition;
        public Vector3 WorldForward => _worldForward;
        public Vector3 WorldUp => _worldUp;
        public Vector3 WorldVelocity => _worldVelocity;
        public int Frequency => _frequency;
        public int Gear => _gear;
        public bool ManualTransmission
        {
            get => _manualTransmission;
            set => _manualTransmission = value;
        }
        public CarType CarType => _carType;
        public ICarListener? Listener
        {
            get => _listener;
            set => _listener = value;
        }
        public bool EngineRunning => _soundEngine.IsPlaying;
        public bool Braking => _soundBrake.IsPlaying;
        public bool Horning => _soundHorn.IsPlaying;
        public bool UserDefined => _userDefined;
        public string? CustomFile => _customFile;
        public string VehicleName { get; private set; } = "Vehicle";
        public float WidthM => _widthM;
        public float LengthM => _lengthM;

        // Engine simulation properties for reporting
        public float SpeedKmh => _engine.SpeedKmh;
        public float EngineRpm => _engine.Rpm;
        public float DistanceMeters => _mapState.DistanceMeters;
        public MapMovementState MapState => _mapState;

        public void Initialize(float positionX = 0, float positionY = 0)
        {
            if (Math.Abs(positionX) > 0.001f || Math.Abs(positionY) > 0.001f)
                _mapState = _track.CreateStateFromWorld(new Vector3(positionX, 0f, positionY), _track.Map.StartHeading);
            else
                _mapState = _track.CreateStartState();
            _positionX = 0f;
            _positionY = _mapState.DistanceMeters;
            _laneWidth = _track.LaneWidth * 2;
            _audioInitialized = false;
            _lastAudioX = _mapState.WorldPosition.X;
            _lastAudioY = _mapState.WorldPosition.Z;
            _lastAudioElapsed = 0f;
            var pose = _track.GetPose(_mapState);
            _dynamicsState = new VehicleDynamicsState
            {
                VelLong = 0f,
                VelLat = 0f,
                Yaw = pose.HeadingRadians,
                YawRate = 0f,
                SteerInput = 0f,
                SteerWheelAngleRad = 0f,
                SteerWheelAngleDeg = 0f
            };
            _turnTickInitialized = false;
            _turnTickAccumulator = 0f;
            _lastHeadingDegrees = HeadingDegrees;
            _worldPosition = pose.Position;
            _worldForward = pose.Tangent;
            _worldUp = Vector3.UnitY;
            _worldVelocity = Vector3.Zero;
            _vibration?.PlayEffect(VibrationEffectType.Spring);
        }

        public void SetPosition(float positionX, float positionY)
        {
            _mapState = _track.CreateStateFromWorld(new Vector3(positionX, 0f, positionY), _mapState.Heading);
            _positionX = 0f;
            _positionY = _mapState.DistanceMeters;
            var pose = _track.GetPose(_mapState);
            _worldPosition = pose.Position;
        }

        public void FinalizeCar()
        {
            _soundEngine.Stop();
            _soundThrottle?.Stop();
            _vibration?.StopEffect(VibrationEffectType.Spring);
        }

        public void Start()
        {
            var delay = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
            PushEvent(CarEventType.CarStart, delay);
            _soundStart.Restart(loop: false);
            _speed = 0;
            _dynamicsState = new VehicleDynamicsState
            {
                VelLong = 0f,
                VelLat = 0f,
                Yaw = _track.GetPose(_mapState).HeadingRadians,
                YawRate = 0f,
                SteerInput = 0f,
                SteerWheelAngleRad = 0f,
                SteerWheelAngleDeg = 0f
            };
            _turnTickInitialized = false;
            _turnTickAccumulator = 0f;
            _lastHeadingDegrees = HeadingDegrees;
            _engine.Reset();
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _prevSurfaceFrequency = 0;
            _surfaceFrequency = 0;
            _switchingGear = 0;
            _throttleVolume = 0.0f;
            _soundAsphalt.SetFrequency(_surfaceFrequency);
            _soundGravel.SetFrequency(_surfaceFrequency);
            _soundWater.SetFrequency(_surfaceFrequency);
            _soundSand.SetFrequency(_surfaceFrequency);
            _soundSnow.SetFrequency(_surfaceFrequency);
            _state = CarState.Starting;
            _listener?.OnStart();
            _vibration?.PlayEffect(VibrationEffectType.Start);
            _vibration?.PlayEffect(VibrationEffectType.Engine);
        }

        /// <summary>
        /// Restarts the car after a crash, preserving distance traveled.
        /// </summary>
        public void RestartAfterCrash()
        {
            var delay = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
            PushEvent(CarEventType.CarStart, delay);
            _soundStart.Restart(loop: false);
            _speed = 0;
            _dynamicsState = new VehicleDynamicsState
            {
                VelLong = 0f,
                VelLat = 0f,
                Yaw = _track.GetPose(_mapState).HeadingRadians,
                YawRate = 0f,
                SteerInput = 0f,
                SteerWheelAngleRad = 0f,
                SteerWheelAngleDeg = 0f
            };
            _turnTickInitialized = false;
            _turnTickAccumulator = 0f;
            _lastHeadingDegrees = HeadingDegrees;
            // Do NOT call _engine.Reset() - preserve distance
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _prevSurfaceFrequency = 0;
            _surfaceFrequency = 0;
            _switchingGear = 0;
            _throttleVolume = 0.0f;
            _soundAsphalt.SetFrequency(_surfaceFrequency);
            _soundGravel.SetFrequency(_surfaceFrequency);
            _soundWater.SetFrequency(_surfaceFrequency);
            _soundSand.SetFrequency(_surfaceFrequency);
            _soundSnow.SetFrequency(_surfaceFrequency);
            _state = CarState.Starting;
            _listener?.OnStart();
            _vibration?.PlayEffect(VibrationEffectType.Start);
            _vibration?.PlayEffect(VibrationEffectType.Engine);
        }

        public void Crash()
        {
            _speed = 0;
            _engine.ResetForCrash();
            _throttleVolume = 0.0f;
            _soundCrash.Restart(loop: false);
            _soundEngine.Stop();
            _soundEngine.SeekToStart();
            if (_soundThrottle != null)
            {
                _soundThrottle.Stop();
                _soundThrottle.SeekToStart();
            }
            _soundStart.SetPanPercent(0);
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    _soundAsphalt.Stop();
                    _soundAsphalt.SetPanPercent(0);
                    _soundAsphalt.SetVolumePercent(90);
                    break;
                case TrackSurface.Gravel:
                    _soundGravel.Stop();
                    _soundGravel.SetPanPercent(0);
                    _soundGravel.SetVolumePercent(90);
                    break;
                case TrackSurface.Water:
                    _soundWater.Stop();
                    _soundWater.SetPanPercent(0);
                    _soundWater.SetVolumePercent(90);
                    break;
                case TrackSurface.Sand:
                    _soundSand.Stop();
                    _soundSand.SetPanPercent(0);
                    _soundSand.SetVolumePercent(90);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.Stop();
                    _soundSnow.SetPanPercent(0);
                    _soundSnow.SetVolumePercent(90);
                    break;
            }
            _soundBrake.Stop();
            _soundBrake.SeekToStart();
            _soundBrake.SetPanPercent(0);
            if (_hasWipers == 1 && _soundWipers != null)
            {
                _soundWipers.Stop();
                _soundWipers.SeekToStart();
                _soundWipers.SetPanPercent(0);
            }
            _soundHorn.Stop();
            _soundHorn.SeekToStart();
            _soundHorn.SetPanPercent(0);
            _gear = 1;
            _switchingGear = 0;
            _state = CarState.Crashing;
            // Transition to Crashed state after crash animation completes (player must manually restart)
            PushEvent(CarEventType.CrashComplete, _soundCrash.GetLengthSeconds() + 1.25f);
            _listener?.OnCrash();
            _vibration?.StopEffect(VibrationEffectType.Engine);
            _vibration?.PlayEffect(VibrationEffectType.Crash);
            PushEvent(CarEventType.StopVibration, CrashVibrationSeconds, VibrationEffectType.Crash);
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
        }

        public void MiniCrash(float newPosition)
        {
            _speed /= 4;
            if (_positionX < newPosition)
                _vibration?.PlayEffect(VibrationEffectType.BumpLeft);
            if (_positionX > newPosition)
                _vibration?.PlayEffect(VibrationEffectType.BumpRight);
            PushEvent(CarEventType.StopBumpVibration, BumpVibrationSeconds);

            _positionX = newPosition;
            _throttleVolume = 0.0f;
            _soundMiniCrash.SeekToStart();
            _soundMiniCrash.Play(loop: false);
        }

        public void Bump(float bumpX, float bumpY, float bumpSpeed)
        {
            if (bumpY != 0)
            {
                _speed -= bumpSpeed;
                _positionY += bumpY;
            }

            if (bumpX > 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
                _vibration?.PlayEffect(VibrationEffectType.BumpLeft);
            }
            else if (bumpX < 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
                _vibration?.PlayEffect(VibrationEffectType.BumpRight);
            }

            if (_speed < 0)
                _speed = 0;
            _soundBump.Play(loop: false);
            PushEvent(CarEventType.StopBumpVibration, BumpVibrationSeconds);
        }

        public void Stop()
        {
            _soundBrake.Stop();
            _soundWipers?.Stop();
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
            _state = CarState.Stopping;
        }

        public void Quiet()
        {
            _soundBrake.Stop();
            _soundEngine.SetVolumePercent(90);
            _soundThrottle?.Stop();
            _soundBackfire?.SetVolumePercent(90);
            _soundAsphalt.SetVolumePercent(90);
            _soundGravel.SetVolumePercent(90);
            _soundWater.SetVolumePercent(90);
            _soundSand.SetVolumePercent(90);
            _soundSnow.SetVolumePercent(90);
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
            _vibration?.StopEffect(VibrationEffectType.Engine);
        }
        public void Run(float elapsed)
        {
            _lastAudioElapsed = elapsed;
            var horning = _input.GetHorn();

            if (_state == CarState.Running && _started())
            {
                if (!IsFinite(_speed))
                    _speed = 0f;
                if (!IsFinite(_positionX))
                    _positionX = 0f;
                if (!IsFinite(_positionY))
                    _positionY = 0f;

                _currentSteering = _input.GetSteering();
                _currentThrottle = _input.GetThrottle();
                _currentBrake = _input.GetBrake();
                var gearUp = _input.GetGearUp();
                var gearDown = _input.GetGearDown();

                _currentSurfaceTractionFactor = _surfaceTractionFactor;
                _currentDeceleration = _deceleration;
                _speedDiff = 0;
                switch (_surface)
                {
                    case TrackSurface.Gravel:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 2) / 3;
                        _currentDeceleration = (_currentDeceleration * 2) / 3;
                        break;
                    case TrackSurface.Water:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 3) / 5;
                        _currentDeceleration = (_currentDeceleration * 3) / 5;
                        break;
                    case TrackSurface.Sand:
                        _currentSurfaceTractionFactor = _currentSurfaceTractionFactor / 2;
                        _currentDeceleration = (_currentDeceleration * 3) / 2;
                        break;
                    case TrackSurface.Snow:
                        _currentDeceleration = _currentDeceleration / 2;
                        break;
                }

                _factor1 = 100;
                if (_manualTransmission)
                {
                    if (!gearUp && !gearDown)
                        s_stickReleased = true;
                    if (gearDown && _gear > 1 && s_stickReleased)
                    {
                        s_stickReleased = false;
                        _switchingGear = -1;
                        --_gear;
                        if (_soundEngine.GetPitch() > 3f * _topFreq / (2f * _soundEngine.InputSampleRate))
                            _soundBadSwitch.Play(loop: false);
                        if (_soundBackfire != null)
                        {
                            if (!_soundBackfire.IsPlaying && Algorithm.RandomInt(5) == 1)
                                _soundBackfire.Play(loop: false);
                        }
                        PushEvent(CarEventType.InGear, 0.2f);
                    }
                    else if (gearUp && _gear < _gears && s_stickReleased)
                    {
                        s_stickReleased = false;
                        _switchingGear = 1;
                        ++_gear;
                        if (_soundEngine.GetPitch() < _idleFreq / (float)_soundEngine.InputSampleRate)
                            _soundBadSwitch.Play(loop: false);
                        if (_soundBackfire != null)
                        {
                            if (!_soundBackfire.IsPlaying && Algorithm.RandomInt(5) == 1)
                                _soundBackfire.Play(loop: false);
                        }
                        PushEvent(CarEventType.InGear, 0.2f);
                    }
                }

                if (_soundThrottle != null)
                {
                    if (_soundEngine.IsPlaying)
                    {
                        if (_currentThrottle > 50)
                        {
                            if (!_soundThrottle.IsPlaying)
                            {
                                if (_throttleVolume < 80.0f)
                                    _throttleVolume = 80.0f;
                                _soundThrottle.SetVolumePercent((int)_throttleVolume);
                                _prevThrottleVolume = _throttleVolume;
                                _soundThrottle.Play(loop: true);
                            }
                            else
                            {
                                if (_throttleVolume >= 80.0f)
                                    _throttleVolume += (100.0f - _throttleVolume) * elapsed;
                                else
                                    _throttleVolume = 80.0f;
                                if (_throttleVolume > 100.0f)
                                    _throttleVolume = 100.0f;
                                if ((int)_throttleVolume != (int)_prevThrottleVolume)
                                {
                                    _soundThrottle.SetVolumePercent((int)_throttleVolume);
                                    _prevThrottleVolume = _throttleVolume;
                                }
                            }
                        }
                        else
                        {
                            _throttleVolume -= 10.0f * elapsed;
                            var min = _speed * 95 / _topSpeed;
                            if (_throttleVolume < min)
                                _throttleVolume = min;
                            if ((int)_throttleVolume != (int)_prevThrottleVolume)
                            {
                                _soundThrottle.SetVolumePercent((int)_throttleVolume);
                                _prevThrottleVolume = _throttleVolume;
                            }
                        }
                    }
                    else if (_soundThrottle.IsPlaying)
                    {
                        _soundThrottle.Stop();
                    }
                }

                _thrust = _currentThrottle;
                if (_currentThrottle == 0)
                    _thrust = _currentBrake;
                else if (_currentBrake == 0)
                    _thrust = _currentThrottle;
                else if (-_currentBrake > _currentThrottle)
                    _thrust = _currentBrake;

                var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
                var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
                var surfaceTractionMod = _surfaceTractionFactor > 0f
                    ? _currentSurfaceTractionFactor / _surfaceTractionFactor
                    : 1.0f;

                var speedForwardMps = Math.Abs(_dynamicsState.VelLong);
                var driveForce = 0f;
                if (_thrust > 10)
                {
                    var driveRpm = VehiclePowertrainMath.CalculateDriveRpm(_engine, _gear, speedForwardMps, throttle, _powertrainParams);
                    var engineTorque = VehiclePowertrainMath.CalculateEngineTorqueNm(driveRpm, _powertrainParams) * throttle * _powerFactor;
                    var gearRatio = _engine.GetGearRatio(_gear);
                    var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
                    driveForce = wheelTorque / _wheelRadiusM;
                    driveForce *= (_factor1 / 100f);
                    _lastDriveRpm = VehiclePowertrainMath.CalculateDriveRpm(_engine, _gear, speedForwardMps, throttle, _powertrainParams);
                    if (_backfirePlayed)
                        _backfirePlayed = false;
                }
                else
                {
                    _lastDriveRpm = 0f;
                }

                var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
                var brakeForce = brakeInput > 0f
                    ? _massKg * (VehiclePowertrainMath.CalculateBrakeDecel(brakeInput, surfaceDecelMod, _powertrainParams) / 3.6f)
                    : 0f;
                var engineBrakeForce = throttle > 0.05f
                    ? 0f
                    : _massKg * (VehiclePowertrainMath.CalculateEngineBrakingDecel(_engine, _gear, surfaceDecelMod, _powertrainParams) / 3.6f);

                var inputs = new VehicleDynamicsInputs
                {
                    Elapsed = elapsed,
                    SteeringCommand = _currentSteering,
                    DriveForce = driveForce,
                    BrakeForce = brakeForce,
                    EngineBrakeForce = engineBrakeForce,
                    SurfaceTractionMod = surfaceTractionMod,
                    TireGripCoefficient = _tireGripCoefficient,
                    LateralGripCoefficient = _lateralGripCoefficient
                };
                var dynamics = _dynamicsModel == VehicleDynamicsModel.Bicycle
                    ? BicycleDynamics.Step(ref _dynamicsState, _bicycleParams, inputs)
                    : VehicleDynamics.Step(ref _dynamicsState, _dynamicsParams, inputs);
                _speed = dynamics.SpeedKph;
                _speedDiff = dynamics.SpeedDiffKph;
                var longitudinalGripFactor = dynamics.LongitudinalGripFactor;

                if (!IsFinite(_speed))
                {
                    _speed = 0f;
                    _speedDiff = 0f;
                }
                if (!IsFinite(_lastDriveRpm))
                    _lastDriveRpm = _idleRpm;

                var speedForGearKph = Math.Abs(_dynamicsState.VelLong) * 3.6f;
                if (_manualTransmission)
                {
                    var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                    if (speedForGearKph > gearMax)
                        speedForGearKph = gearMax;
                }
                else
                {
                    UpdateAutomaticGear(elapsed, speedForGearKph / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
                }

                // Update engine model for RPM and distance tracking (reporting only)
                _engine.SyncFromSpeed(speedForGearKph, _gear, elapsed, _currentThrottle);
                if (_lastDriveRpm > 0f && _lastDriveRpm > _engine.Rpm)
                    _engine.OverrideRpm(_lastDriveRpm);

                if (_thrust <= 0)
                {
                    if (_soundBackfire != null)
                    {
                        if (!_soundBackfire.IsPlaying && !_backfirePlayed)
                        {
                            if (Algorithm.RandomInt(5) == 1)
                                _soundBackfire.Play(loop: false);
                        }
                        _backfirePlayed = true;
                    }
                }

                if (_thrust < -50 && _speed > 0)
                {
                    BrakeSound();
                    _vibration?.Gain(VibrationEffectType.Spring, (int)(50.0f * _speed / _topSpeed));
                    _currentSteering = _currentSteering * 2 / 3;
                }
                else if (_currentSteering != 0 && _speed > _topSpeed / 2)
                {
                    if (_thrust > -50)
                        BrakeCurveSound();
                }
                else
                {
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    _soundAsphalt.SetVolumePercent(90);
                    _soundGravel.SetVolumePercent(90);
                    _soundWater.SetVolumePercent(90);
                    _soundSand.SetVolumePercent(90);
                    _soundSnow.SetVolumePercent(90);
                }
                var heading = MapMovement.DirectionFromYaw(_dynamicsState.Yaw);
                var distanceMeters = (_speed / 3.6f) * elapsed;
                var previousPosition = _worldPosition;

                var yaw = _dynamicsState.Yaw;
                var forward = new Vector3((float)Math.Sin(yaw), 0f, (float)Math.Cos(yaw));
                var right = new Vector3(forward.Z, 0f, -forward.X);
                var velocity = (forward * _dynamicsState.VelLong) + (right * _dynamicsState.VelLat);
                var nextPosition = _worldPosition + (velocity * elapsed);

                var canMove = _track.IsWithinTrack(nextPosition);
                if (canMove && !_track.IsSectorTransitionAllowed(_worldPosition, nextPosition, heading))
                    canMove = false;

                if (canMove)
                {
                    _worldPosition = nextPosition;
                    _mapState.WorldPosition = _worldPosition;
                    var (cellX, cellZ) = _track.Map.WorldToCell(_worldPosition);
                    _mapState.CellX = cellX;
                    _mapState.CellZ = cellZ;
                    _mapState.Heading = heading;
                    _mapState.HeadingDegrees = HeadingDegrees;
                    _mapState.DistanceMeters += distanceMeters;
                    ApplySectorSpeedRules(nextPosition, heading);
                }
                else
                {
                    _speed = 0f;
                    _dynamicsState.VelLong = 0f;
                    _dynamicsState.VelLat = 0f;
                }

                _positionY = _mapState.DistanceMeters;
                var worldVelocity = elapsed > 0f ? (_worldPosition - previousPosition) / elapsed : Vector3.Zero;
                _worldForward = forward.LengthSquared() > 0.0001f ? Vector3.Normalize(forward) : Vector3.UnitZ;
                _worldUp = Vector3.UnitY;
                _worldVelocity = worldVelocity;
                UpdateTurnTick();

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    _brakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    if (_speed <= 50.0f)
                        _soundBrake.SetVolumePercent((int)(100 - (50 - (_speed)))); 
                    else
                        _soundBrake.SetVolumePercent(100);
                    if (_manualTransmission)
                        UpdateEngineFreqManual();
                    else
                        UpdateEngineFreq();
                    UpdateSoundRoad();
                    if (_vibration != null)
                    {
                        if (_surface == TrackSurface.Gravel)
                            _vibration.Gain(VibrationEffectType.Gravel, (int)(_speed * 10000 / _topSpeed));
                        else
                            _vibration.Gain(VibrationEffectType.Gravel, 0);

                        if (_speed == 0)
                            _vibration.Gain(VibrationEffectType.Spring, 10000);
                        else
                            _vibration.Gain(VibrationEffectType.Spring, (int)(10000 * _speed / _topSpeed));

                        if (_speed < _topSpeed / 10)
                            _vibration.Gain(VibrationEffectType.Engine, (int)(10000 - _speed * 10 / _topSpeed));
                        else
                            _vibration.Gain(VibrationEffectType.Engine, 0);
                    }
                }

                switch (_surface)
                {
                    case TrackSurface.Asphalt:
                        if (!_soundAsphalt.IsPlaying)
                        {
                            _soundAsphalt.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundAsphalt.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Gravel:
                        if (!_soundGravel.IsPlaying)
                        {
                            _soundGravel.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundGravel.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Water:
                        if (!_soundWater.IsPlaying)
                        {
                            _soundWater.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundWater.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Sand:
                        if (!_soundSand.IsPlaying)
                        {
                            _soundSand.SetFrequency((int)(_surfaceFrequency / 2.5f));
                            _soundSand.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Snow:
                        if (!_soundSnow.IsPlaying)
                        {
                            _soundSnow.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundSnow.Play(loop: true);
                        }
                        break;
                }
            }
            else if (_state == CarState.Stopping)
            {
                _speed -= (elapsed * 100 * _deceleration);
                if (_speed < 0)
                    _speed = 0;
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    UpdateEngineFreq();
                    UpdateSoundRoad();
                }
            }

            if (horning && _state != CarState.Stopped && _state != CarState.Crashing)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else
            {
                if (_soundHorn.IsPlaying)
                    _soundHorn.Stop();
            }

            var now = _currentTime();
            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time < now)
                {
                    switch (e.Type)
                    {
                        case CarEventType.CarStart:
                            _soundEngine.SetFrequency(_idleFreq);
                            _soundThrottle?.SetFrequency(_idleFreq);
                            _vibration?.StopEffect(VibrationEffectType.Start);
                            _soundEngine.Play(loop: true);
                            _soundWipers?.Play(loop: true);
                            _engine.StartEngine();  // Set RPM to idle
                            _state = CarState.Running;
                            break;
                        case CarEventType.CarRestart:
                            _vibration?.StopEffect(VibrationEffectType.Crash);
                            Start();
                            break;
                        case CarEventType.CrashComplete:
                            // Crash animation done - set to Crashed state, awaiting manual restart
                            _vibration?.StopEffect(VibrationEffectType.Crash);
                            _state = CarState.Crashed;
                            break;
                        case CarEventType.InGear:
                            _switchingGear = 0;
                            break;
                        case CarEventType.StopVibration:
                            if (e.Effect.HasValue)
                                _vibration?.StopEffect(e.Effect.Value);
                            break;
                        case CarEventType.StopBumpVibration:
                            _vibration?.StopEffect(VibrationEffectType.BumpLeft);
                            _vibration?.StopEffect(VibrationEffectType.BumpRight);
                            break;
                    }
                    _events.RemoveAt(i);
                }
            }
        }

        public void BrakeSound()
        {
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    if (!_soundBrake.IsPlaying)
                    {
                        _soundAsphalt.SetVolumePercent(90);
                        _soundBrake.Play(loop: true);
                    }
                    break;
                case TrackSurface.Gravel:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        _soundGravel.SetVolumePercent((int)(100 - (10 - (_speed / 5))));
                    else
                        _soundGravel.SetVolumePercent(100);
                    break;
                case TrackSurface.Water:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        _soundWater.SetVolumePercent((int)(100 - (10 - (_speed / 5))));
                    else
                        _soundWater.SetVolumePercent(100);
                    break;
                case TrackSurface.Sand:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        _soundSand.SetVolumePercent((int)(100 - (10 - (_speed / 5))));
                    else
                        _soundSand.SetVolumePercent(100);
                    break;
                case TrackSurface.Snow:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        _soundSnow.SetVolumePercent((int)(100 - (10 - (_speed / 5))));
                    else
                        _soundSnow.SetVolumePercent(100);
                    break;
            }
        }

        public void BrakeCurveSound()
        {
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    _soundAsphalt.SetVolumePercent(92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Gravel:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    _soundGravel.SetVolumePercent(92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Water:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    _soundWater.SetVolumePercent(92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Sand:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    _soundSand.SetVolumePercent(92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Snow:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    _soundSnow.SetVolumePercent(92 * Math.Abs(_currentSteering) / 100);
                    break;
            }
        }

        public void Evaluate(TrackRoad road)
        {
            var roadWidth = road.Right - road.Left;
            if (roadWidth > 0f)
                _laneWidth = roadWidth;
            else
                roadWidth = _laneWidth;

            var updateAudioThisFrame = true;

            if (_state == CarState.Stopped)
            {
                if (updateAudioThisFrame)
                {
                    _relPos = roadWidth <= 0f
                        ? 0.5f
                        : (_positionX - road.Left) / roadWidth;
                    _panPos = CalculatePan(_relPos);
                    _soundStart.SetPanPercent(_panPos);
                    _soundHorn.SetPanPercent(_panPos);
                    _soundWipers?.SetPanPercent(_panPos);
                    UpdateSpatialAudio(road);
                }
            }

            if (_state == CarState.Running && _started())
            {
                if (road.IsOutOfBounds)
                {
                    Crash();
                    _frame++;
                    return;
                }
                if (updateAudioThisFrame)
                {
                    if (_surface == TrackSurface.Asphalt && road.Surface != TrackSurface.Asphalt)
                    {
                        _soundAsphalt.Stop();
                        SwitchSurfaceSound(road.Surface);
                    }
                    else if (_surface == TrackSurface.Gravel && road.Surface != TrackSurface.Gravel)
                    {
                        _soundGravel.Stop();
                        SwitchSurfaceSound(road.Surface);
                    }
                    else if (_surface == TrackSurface.Water && road.Surface != TrackSurface.Water)
                    {
                        _soundWater.Stop();
                        SwitchSurfaceSound(road.Surface);
                    }
                    else if (_surface == TrackSurface.Sand && road.Surface != TrackSurface.Sand)
                    {
                        _soundSand.Stop();
                        SwitchSurfaceSound(road.Surface);
                    }
                    else if (_surface == TrackSurface.Snow && road.Surface != TrackSurface.Snow)
                    {
                        _soundSnow.Stop();
                        SwitchSurfaceSound(road.Surface);
                    } 

                    _surface = road.Surface;
                    _relPos = roadWidth <= 0f
                        ? 0.5f
                        : (_positionX - road.Left) / roadWidth;
                    _panPos = CalculatePan(_relPos);
                    ApplyPan(_panPos);
                    UpdateSpatialAudio(road);

                    if (_vibration != null)
                    {
                        if (_relPos < 0.05 && _speed > _topSpeed / 10)
                            _vibration.PlayEffect(VibrationEffectType.CurbLeft);
                        else
                            _vibration.StopEffect(VibrationEffectType.CurbLeft);

                        if (_relPos > 0.95 && _speed > _topSpeed / 10)
                            _vibration.PlayEffect(VibrationEffectType.CurbRight);
                        else
                            _vibration.StopEffect(VibrationEffectType.CurbRight);
                    }
                    if (_relPos < 0 || _relPos > 1)
                    {
                        var fullCrash = _gear > 1 || _speed >= 50.0f;
                        if (fullCrash)
                            Crash();
                        else
                            MiniCrash((road.Right + road.Left) / 2);
                    }
                }
            }
            else if (_state == CarState.Crashing)
            {
                _positionX = (road.Right + road.Left) / 2;
            }
            _frame++;
        }

        public bool Backfiring() => _soundBackfire != null && _soundBackfire.IsPlaying;

        public void Pause()
        {
            _soundEngine.Stop();
            _soundThrottle?.Stop();
            if (_soundBrake.IsPlaying)
                _soundBrake.Stop();
            if (_soundHorn.IsPlaying)
                _soundHorn.Stop();
            if (_soundBackfire != null && _soundBackfire.IsPlaying)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
            }
            if (_soundTurnTick != null && _soundTurnTick.IsPlaying)
            {
                _soundTurnTick.Stop();
                _soundTurnTick.SeekToStart();
            }
            _soundWipers?.Stop();
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    _soundAsphalt.Stop();
                    break;
                case TrackSurface.Gravel:
                    _soundGravel.Stop();
                    break;
                case TrackSurface.Water:
                    _soundWater.Stop();
                    break;
                case TrackSurface.Sand:
                    _soundSand.Stop();
                    break;
                case TrackSurface.Snow:
                    _soundSnow.Stop();
                    break;
            }
        }

        public void Unpause()
        {
            _soundEngine.Play(loop: true);
            _soundThrottle?.Play(loop: true);
            _soundWipers?.Play(loop: true);
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    _soundAsphalt.Play(loop: true);
                    break;
                case TrackSurface.Gravel:
                    _soundGravel.Play(loop: true);
                    break;
                case TrackSurface.Water:
                    _soundWater.Play(loop: true);
                    break;
                case TrackSurface.Sand:
                    _soundSand.Play(loop: true);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.Play(loop: true);
                    break;
            }
        }

        public void Dispose()
        {
            StopAllVibrations();
            _soundEngine.Dispose();
            _soundThrottle?.Dispose();
            _soundHorn.Dispose();
            _soundStart.Dispose();
            _soundCrash.Dispose();
            _soundBrake.Dispose();
            _soundAsphalt.Dispose();
            _soundGravel.Dispose();
            _soundWater.Dispose();
            _soundSand.Dispose();
            _soundSnow.Dispose();
            _soundMiniCrash.Dispose();
            _soundWipers?.Dispose();
            _soundBump.Dispose();
            _soundBadSwitch.Dispose();
            _soundBackfire?.Dispose();
            _soundTurnTick?.Dispose();
        }

        private void StopAllVibrations()
        {
            if (_vibration == null)
                return;
            foreach (VibrationEffectType effect in Enum.GetValues(typeof(VibrationEffectType)))
                _vibration.StopEffect(effect);
        }

        private void PushEvent(CarEventType type, float time, VibrationEffectType? effect = null)
        {
            _events.Add(new CarEvent
            {
                Type = type,
                Time = _currentTime() + time,
                Effect = effect
            });
        }

        private void UpdateEngineFreq()
        {
            var gearForSound = _gear;
            if (gearForSound > _gears)
                gearForSound = _gears;
            if (gearForSound < 1)
                gearForSound = 1;

            UpdateEngineFreqForGear(gearForSound);
        }

        private void UpdateEngineFreqManual()
        {
            var gearRange = _engine.GetGearRangeKmh(_gear);
            var gearMin = _engine.GetGearMinSpeedKmh(_gear);

            if (_gear == 1)
            {
                // Gear 1: frequency scales with speed relative to gear range   
                if (_speed < (4.0f / 3.0f) * gearRange)
                {
                    _frequency = _idleFreq + (int)((_speed * 3.0f / (2.0f * gearRange)) * (_topFreq - _idleFreq));
                }
                else
                {
                    // Cap at 2x the frequency range above idle when speed exceeds gear capability
                    _frequency = _idleFreq + 2 * (_topFreq - _idleFreq);        
                }
            }
            else
            {
                // Higher gears: frequency = (speed / shiftPoint) * topFreq     
                // where shiftPoint = gearMin + (2/3) * gearRange
                var shiftPoint = gearMin + ((2.0f / 3.0f) * gearRange);
                if (shiftPoint > 0f)
                    _frequency = (int)((_speed / shiftPoint) * _topFreq);
                else
                    _frequency = _idleFreq;

                // Clamp frequency to valid range
                if (_frequency > 2 * _topFreq)
                    _frequency = 2 * _topFreq;
                if (_frequency < _idleFreq / 2)
                    _frequency = _idleFreq / 2;
            }

            // Smooth gear transition
            if (_switchingGear != 0)
                _frequency = (2 * _prevFrequency + _frequency) / 3;

            // Apply frequency change to sound
            if (_frequency != _prevFrequency)
            {
                _soundEngine.SetFrequency(_frequency);
                if (_soundThrottle != null)
                {
                    if ((int)_throttleVolume != (int)_prevThrottleVolume)
                    {
                        _soundThrottle.SetVolumePercent((int)_throttleVolume);
                        _prevThrottleVolume = _throttleVolume;
                    }
                    _soundThrottle.SetFrequency(_frequency);
                }
                _prevFrequency = _frequency;
            }
        }

        private void UpdateEngineFreqForGear(int gear)
        {
            var clampedGear = gear;
            if (clampedGear > _gears)
                clampedGear = _gears;
            if (clampedGear < 1)
                clampedGear = 1;

            var gearRange = _engine.GetGearRangeKmh(clampedGear);
            var gearMin = _engine.GetGearMinSpeedKmh(clampedGear);

            if (clampedGear == 1)
            {
                var gearSpeed = gearRange <= 0f ? 0f : Math.Min(1.0f, _speed / gearRange);
                _frequency = (int)(gearSpeed * (_topFreq - _idleFreq)) + _idleFreq;
            }
            else
            {
                var gearSpeed = (_speed - gearMin) / (float)gearRange;
                if (gearSpeed <= 0f)
                {
                    _frequency = _idleFreq;
                    if (_soundBackfire != null && _backfirePlayedAuto)
                        _backfirePlayedAuto = false;
                }
                else
                {
                    if (gearSpeed > 1.0f)
                        gearSpeed = 1.0f;
                    if (gearSpeed < 0.07f)
                    {
                        _frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (_topFreq - _shiftFreq) + _shiftFreq);
                        if (_soundBackfire != null)
                        {
                            if (!_backfirePlayedAuto)
                            {
                                if (Algorithm.RandomInt(5) == 1 && !_soundBackfire.IsPlaying)
                                    _soundBackfire.Play(loop: false);
                            }
                            _backfirePlayedAuto = true;
                        }
                    }
                    else
                    {
                        _frequency = (int)(gearSpeed * (_topFreq - _shiftFreq) + _shiftFreq);
                        if (_soundBackfire != null && _backfirePlayedAuto)
                            _backfirePlayedAuto = false;
                    }
                }
            }

            if (_switchingGear != 0)
                _frequency = (2 * _prevFrequency + _frequency) / 3;
            if (_frequency != _prevFrequency)
            {
                _soundEngine.SetFrequency(_frequency);
                if (_soundThrottle != null)
                {
                    if ((int)_throttleVolume != (int)_prevThrottleVolume)
                    {
                        _soundThrottle.SetVolumePercent((int)_throttleVolume);
                        _prevThrottleVolume = _throttleVolume;
                    }
                    _soundThrottle.SetFrequency(_frequency);
                }
                _prevFrequency = _frequency;
            }
        }

        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;

            if (_autoShiftCooldown > 0f)
            {
                _autoShiftCooldown -= elapsed;
                return;
            }

            var decision = VehiclePowertrainMath.SelectAutomaticGear(
                _engine,
                _gear,
                _gears,
                speedMps,
                throttle,
                surfaceTractionMod,
                longitudinalGripFactor,
                _powertrainParams);
            if (decision.ShouldShift)
                ShiftAutomaticGear(decision.TargetGear);
        }

        private void ShiftAutomaticGear(int newGear)
        {
            if (newGear == _gear)
                return;
            _switchingGear = newGear > _gear ? 1 : -1;
            _gear = newGear;
            PushEvent(CarEventType.InGear, 0.2f);
            _autoShiftCooldown = AutoShiftCooldownSeconds;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float SanitizeFinite(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private void UpdateSoundRoad()
        {
            _surfaceFrequency = (int)(_speed * 500);
            if (_surfaceFrequency != _prevSurfaceFrequency)
            {
                switch (_surface)
                {
                    case TrackSurface.Asphalt:
                        _soundAsphalt.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                        break;
                    case TrackSurface.Gravel:
                        _soundGravel.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                        break;
                    case TrackSurface.Water:
                        _soundWater.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                        break;
                    case TrackSurface.Sand:
                        _soundSand.SetFrequency((int)(_surfaceFrequency / 2.5f));
                        break;
                    case TrackSurface.Snow:
                        _soundSnow.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                        break;
                }
                _prevSurfaceFrequency = _surfaceFrequency;
            }
        }

        private void SwitchSurfaceSound(TrackSurface surface)
        {
            switch (surface)
            {
                case TrackSurface.Gravel:
                    _soundGravel.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundGravel.Play(loop: true);
                    break;
                case TrackSurface.Water:
                    _soundWater.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundWater.Play(loop: true);
                    break;
                case TrackSurface.Sand:
                    _soundSand.SetFrequency((int)(_surfaceFrequency / 2.5f));
                    _soundSand.Play(loop: true);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundSnow.Play(loop: true);
                    break;
                case TrackSurface.Asphalt:
                    _soundAsphalt.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundAsphalt.Play(loop: true);
                    break;
            }
        }

        private void ApplyPan(int pan)
        {
            _soundHorn.SetPanPercent(pan);
            _soundBrake.SetPanPercent(pan);
            _soundBackfire?.SetPanPercent(pan);
            _soundWipers?.SetPanPercent(pan);
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    _soundAsphalt.SetPanPercent(pan);
                    break;
                case TrackSurface.Gravel:
                    _soundGravel.SetPanPercent(pan);
                    break;
                case TrackSurface.Water:
                    _soundWater.SetPanPercent(pan);
                    break;
                case TrackSurface.Sand:
                    _soundSand.SetPanPercent(pan);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.SetPanPercent(pan);
                    break;
            }
        }

        private static int CalculatePan(float relPos)
        {
            var pan = (relPos - 0.5f) * 200.0f;
            if (pan < -100.0f) pan = -100.0f;
            if (pan > 100.0f) pan = 100.0f;
            return (int)pan;
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            if (degrees < 0f)
                degrees += 360f;
            return degrees;
        }

        private static float NormalizeDegreesDelta(float degreesDelta)
        {
            degreesDelta %= 360f;
            if (degreesDelta > 180f)
                degreesDelta -= 360f;
            if (degreesDelta < -180f)
                degreesDelta += 360f;
            return degreesDelta;
        }

        private void UpdateTurnTick()
        {
            if (_soundTurnTick == null)
                return;

            var currentHeading = HeadingDegrees;
            if (!_turnTickInitialized)
            {
                _turnTickInitialized = true;
                _lastHeadingDegrees = currentHeading;
                _turnTickAccumulator = 0f;
                return;
            }

            var delta = NormalizeDegreesDelta(currentHeading - _lastHeadingDegrees);
            _lastHeadingDegrees = currentHeading;
            var absDelta = Math.Abs(delta);
            if (!IsFinite(absDelta) || absDelta <= 0f)
                return;

            _turnTickAccumulator += absDelta;
            if (_turnTickAccumulator < 1f)
                return;

            _turnTickAccumulator %= 1f;
            _soundTurnTick.Stop();
            _soundTurnTick.SeekToStart();
            _soundTurnTick.Play(loop: false);
        }

        private void ApplySectorSpeedRules(Vector3 worldPosition, MapDirection heading)
        {
            if (!_track.TryGetSectorRules(worldPosition, heading, out _, out var rules, out _, out _))
                return;

            var speedCap = rules.MaxSpeedKph;
            if (rules.RequiresYield)
            {
                var yieldCap = YieldSpeedKph;
                speedCap = speedCap.HasValue ? Math.Min(speedCap.Value, yieldCap) : yieldCap;
            }

            if (rules.RequiresStop)
                speedCap = 0f;

            if (!speedCap.HasValue)
                return;
            if (_speed <= speedCap.Value)
                return;

            var cap = Math.Max(0f, speedCap.Value);
            var factor = _speed > 0f ? cap / _speed : 0f;
            _speed = cap;
            _dynamicsState.VelLong *= factor;
            _dynamicsState.VelLat *= factor;
        }

        private AudioSourceHandle CreateRequiredSound(string? path, bool looped = false, bool spatialize = true, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Sound path not provided.");
            if (!File.Exists(path))
                throw new FileNotFoundException("Sound file not found.", path);
            if (!spatialize)
            {
                return looped
                    ? _audio.CreateLoopingSource(path!, useHrtf: false)
                    : _audio.CreateSource(path!, streamFromDisk: true, useHrtf: false);
            }

            return looped
                ? _audio.CreateLoopingSpatialSource(path!, allowHrtf: allowHrtf)
                : _audio.CreateSpatialSource(path!, streamFromDisk: true, allowHrtf: allowHrtf);
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false, bool spatialize = true, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            if (!spatialize)
            {
                return looped
                    ? _audio.CreateLoopingSource(path!, useHrtf: false)
                    : _audio.CreateSource(path!, streamFromDisk: true, useHrtf: false);
            }

            return looped
                ? _audio.CreateLoopingSpatialSource(path!, allowHrtf: allowHrtf)
                : _audio.CreateSpatialSource(path!, streamFromDisk: true, allowHrtf: allowHrtf);
        }

        private void UpdateSpatialAudio(TrackRoad road)
        {
            var elapsed = _lastAudioElapsed;
            if (elapsed <= 0f)
                return;

            var worldPos = _worldPosition;
            var velocity = Vector3.Zero;
            if (_audioInitialized && elapsed > 0f)
            {
                var velUnits = new Vector3((worldPos.X - _lastAudioX) / elapsed, 0f, (worldPos.Z - _lastAudioY) / elapsed);
                velocity = AudioWorld.ToMeters(velUnits);
            }
            _lastAudioX = worldPos.X;
            _lastAudioY = worldPos.Z;
            _audioInitialized = true;

            var engineOffsetZ = _lengthM * 0.35f;
            var brakeOffsetZ = -_lengthM * 0.25f;

            var forward = _worldForward.LengthSquared() > 0.0001f ? Vector3.Normalize(_worldForward) : new Vector3(0f, 0f, 1f);
            var up = _worldUp.LengthSquared() > 0.0001f ? Vector3.Normalize(_worldUp) : Vector3.UnitY;
            var rightVec = Vector3.Cross(up, forward);
            if (rightVec.LengthSquared() < 0.0001f)
                rightVec = Vector3.UnitX;
            else
                rightVec = Vector3.Normalize(rightVec);

            var enginePos = worldPos + (forward * engineOffsetZ);
            var brakePos = worldPos + (forward * brakeOffsetZ);
            var vehiclePos = worldPos;

            SetSpatial(_soundEngine, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundThrottle, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundHorn, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundBrake, AudioWorld.ToMeters(brakePos), velocity);
            SetSpatial(_soundBackfire, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundStart, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundCrash, AudioWorld.ToMeters(vehiclePos), velocity);
            SetSpatial(_soundMiniCrash, AudioWorld.ToMeters(vehiclePos), velocity);
            SetSpatial(_soundBump, AudioWorld.ToMeters(vehiclePos), velocity);
            SetSpatial(_soundBadSwitch, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundWipers, AudioWorld.ToMeters(vehiclePos), velocity);

            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    SetSpatial(_soundAsphalt, AudioWorld.ToMeters(vehiclePos), velocity);
                    break;
                case TrackSurface.Gravel:
                    SetSpatial(_soundGravel, AudioWorld.ToMeters(vehiclePos), velocity);
                    break;
                case TrackSurface.Water:
                    SetSpatial(_soundWater, AudioWorld.ToMeters(vehiclePos), velocity);
                    break;
                case TrackSurface.Sand:
                    SetSpatial(_soundSand, AudioWorld.ToMeters(vehiclePos), velocity);
                    break;
                case TrackSurface.Snow:
                    SetSpatial(_soundSnow, AudioWorld.ToMeters(vehiclePos), velocity);
                    break;
            }
        }
        private static void SetSpatial(AudioSourceHandle? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetPosition(position);
            sound.SetVelocity(velocity);
        }

        private sealed class CarEvent
        {
            public float Time { get; set; }
            public CarEventType Type { get; set; }
            public VibrationEffectType? Effect { get; set; }
        }

        private enum CarEventType
        {
            CarStart,
            CarRestart,
            CrashComplete,
            InGear,
            StopVibration,
            StopBumpVibration
        }
    }
}

