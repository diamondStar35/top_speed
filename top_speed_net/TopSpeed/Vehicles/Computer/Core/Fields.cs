using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Bots;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Live;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private readonly Track _track;
        private readonly DriveSettings _settings;
        private readonly Func<float> _currentTime;
        private readonly Func<bool> _started;
        private readonly Action<string>? _debugSpeak;
        private readonly int _playerNumber;
        private readonly int _vehicleIndex;
        private readonly List<BotEvent> _events;
        private readonly RemoteVehicleAudio _raceAudio;

        private ComputerState _state;
        private TrackSurface _surface;
        private int _gear;
        private float _speed;
        private float _positionX;
        private float _positionY;
        private float _autoShiftCooldown;
        private float _trackLength;

        private float _surfaceTractionFactor;
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
        private float _peakTorqueNm;
        private float _peakTorqueRpm;
        private float _idleTorqueNm;
        private float _redlineTorqueNm;
        private float _dragCoefficient;
        private float _frontalAreaM2;
        private float _sideAreaM2;
        private float _rollingResistanceCoefficient;
        private float _rollingResistanceSpeedFactor;
        private float _wheelSideDragBaseN;
        private float _wheelSideDragLinearNPerMps;
        private float _launchRpm;
        private float _coupledDrivelineDragNm;
        private float _coupledDrivelineViscousDragNmPerKrpm;
        private float _engineInertiaKgm2;
        private float _engineFrictionTorqueNm;
        private float _drivelineCouplingRate;
        private float _automaticCouplingFactor = 1f;
        private float _cvtRatio;
        private float _effectiveDriveRatio;
        private float _lateralGripCoefficient;
        private float _highSpeedStability;
        private float _wheelbaseM;
        private float _maxSteerDeg;
        private float _widthM;
        private float _lengthM;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private float _pitchCurveExponent;
        private int _gears;
        private float _steering;

        private int _random;
        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private float _laneWidth;
        private float _relPos;
        private float _nextRelPos;
        private float _diffX;
        private float _diffY;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private float _speedDiff;
        private float _lateralVelocityMps;
        private float _yawRateRad;
        private float _tireWearFraction;
        private float _tireTemperatureC;
        private float _tireSmoothedSlipNormalized;
        private float _surfaceTemperatureC;
        private int _difficulty;
        private bool _finished;
        private bool _horning;
        private bool _networkBackfireActive;
        private bool _remoteEngineStartPending;
        private float _remoteEngineStartRemaining;
        private int _remoteEnginePendingFrequency;
        private bool _remoteNetInit;
        private float _remoteTargetX;
        private float _remoteTargetY;
        private float _remoteTargetSpeed;
        private bool _crashLateralAnchored;
        private float _crashLateralFromCenter;
        private int _frame;
        private Vector3 _lastAudioPosition;
        private bool _audioInitialized;
        private float _lastAudioUpdateTime;
        private readonly VehicleRadioController _radio;
        private readonly LiveRadio _liveRadio;
        private bool _radioLoaded;
        private bool _radioPlaying;
        private uint _radioMediaId;
        private int _remoteRadioSenderVolumePercent = 100;

        private Source _soundEngine = default!;
        private Source _soundHorn = default!;
        private Source _soundStart = default!;
        private Source _soundCrash = default!;
        private Source _soundBrake = default!;
        private Source _soundMiniCrash = default!;
        private Source _soundBump = default!;
        private Source? _soundBackfire;
        private int _lastOtherEngineVolumePercent = -1;
        private int _lastOtherEventsVolumePercent = -1;
        private int _lastRadioVolumePercent = -1;

        private EngineModel _engine;
        private TransmissionType _activeTransmissionType = TransmissionType.Atc;
        private AutomaticDrivelineTuning _automaticTuning = AutomaticDrivelineTuning.Default;
        private readonly BotPhysicsConfig _physicsConfig;
    }
}


