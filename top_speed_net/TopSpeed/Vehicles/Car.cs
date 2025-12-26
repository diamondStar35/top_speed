using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed class Car : IDisposable
    {
        private const int MaxSurfaceFreq = 100000;

        private static bool s_stickReleased;

        private readonly AudioManager _audio;
        private readonly Track _track;
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
        private int _speed;
        private int _positionX;
        private int _positionY;
        private bool _manualTransmission;
        private bool _backfirePlayed;
        private bool _backfirePlayedAuto;
        private int _hasWipers;
        private int _switchingGear;
        private CarType _carType;
        private ICarListener? _listener;
        private string? _customFile;
        private bool _userDefined;

        private int _acceleration;
        private int _deceleration;
        private int _topSpeed;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private int _gears;
        private int _steering;
        private int _steeringFactor;
        private int _thrust;
        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private int _prevSurfaceFrequency;
        private int _surfaceFrequency;
        private int _laneWidth;
        private float _relPos;
        private int _panPos;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private int _currentAcceleration;
        private int _currentDeceleration;
        private int _speedDiff;
        private int _factor1;
        private double _factor2;
        private int _frame;
        private float _prevThrottleVolume;
        private float _throttleVolume;

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

        private ForceFeedbackEffect? _effectStart;
        private ForceFeedbackEffect? _effectCrash;
        private ForceFeedbackEffect? _effectSpring;
        private ForceFeedbackEffect? _effectGravel;
        private ForceFeedbackEffect? _effectEngine;
        private ForceFeedbackEffect? _effectCurbLeft;
        private ForceFeedbackEffect? _effectCurbRight;
        private ForceFeedbackEffect? _effectBumpLeft;
        private ForceFeedbackEffect? _effectBumpRight;

        public Car(
            AudioManager audio,
            Track track,
            RaceInput input,
            RaceSettings settings,
            int vehicleIndex,
            string? vehicleFile,
            Func<float> currentTime,
            Func<bool> started,
            JoystickDevice? joystick = null)
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
            _currentAcceleration = 0;
            _currentDeceleration = 0;
            _speedDiff = 0;
            _factor1 = 100;
            _factor2 = 1.0;

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

            _acceleration = definition.Acceleration;
            _deceleration = definition.Deceleration;
            _topSpeed = definition.TopSpeed;
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _gears = definition.Gears;
            _steering = definition.Steering;
            _steeringFactor = definition.SteeringFactor;
            _frequency = _idleFreq;

            _soundEngine = CreateRequiredSound(definition.EngineSound, looped: true);
            _soundStart = CreateRequiredSound(definition.StartSound);
            _soundHorn = CreateRequiredSound(definition.HornSound, looped: true);
            _soundThrottle = TryCreateSound(definition.ThrottleSound, looped: true);
            _soundCrash = CreateRequiredSound(definition.CrashSound);
            _soundBrake = CreateRequiredSound(definition.BrakeSound, looped: true);
            _soundBackfire = TryCreateSound(definition.BackfireSound);

            if (definition.HasWipers == 1)
                _hasWipers = 1;

            if (_hasWipers == 1)
                _soundWipers = CreateRequiredSound(Path.Combine(_legacyRoot, "wipers.wav"), looped: true);

            _soundAsphalt = CreateRequiredSound(Path.Combine(_legacyRoot, "asphalt.wav"), looped: true);
            _soundGravel = CreateRequiredSound(Path.Combine(_legacyRoot, "gravel.wav"), looped: true);
            _soundWater = CreateRequiredSound(Path.Combine(_legacyRoot, "water.wav"), looped: true);
            _soundSand = CreateRequiredSound(Path.Combine(_legacyRoot, "sand.wav"), looped: true);
            _soundSnow = CreateRequiredSound(Path.Combine(_legacyRoot, "snow.wav"), looped: true);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"));
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"));
            _soundBadSwitch = CreateRequiredSound(Path.Combine(_legacyRoot, "badswitch.wav"));

            if (joystick != null && joystick.ForceFeedbackCapable && _settings.ForceFeedback && _settings.UseJoystick)
            {
                _effectStart = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "carstart.ffe"));
                _effectCrash = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "crash.ffe"));
                _effectSpring = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "spring.ffe"));
                _effectEngine = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "engine.ffe"));
                _effectCurbLeft = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "curbleft.ffe"));
                _effectCurbRight = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "curbright.ffe"));
                _effectGravel = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "gravel.ffe"));
                _effectBumpLeft = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "bumpleft.ffe"));
                _effectBumpRight = new ForceFeedbackEffect(joystick, Path.Combine(_effectsRoot, "bumpright.ffe"));
                _effectGravel.Gain(0);
            }
        }

        public CarState State => _state;
        public int PositionX => _positionX;
        public int PositionY => _positionY;
        public int Speed => _speed;
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

        public void Initialize(int positionX = 0, int positionY = 0)
        {
            _positionX = positionX;
            _positionY = positionY;
            _laneWidth = _track.LaneWidth * 2;
            _effectSpring?.Play();
        }

        public void SetPosition(int positionX, int positionY)
        {
            _positionX = positionX;
            _positionY = positionY;
        }

        public void FinalizeCar()
        {
            _soundEngine.Stop();
            _soundThrottle?.Stop();
            _effectSpring?.Stop();
        }

        public void Start()
        {
            var delay = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
            PushEvent(CarEventType.CarStart, delay);
            _soundStart.Restart(loop: false);
            _speed = 0;
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
            _effectStart?.Play();
            _effectEngine?.Play();
        }

        public void Crash()
        {
            _speed = 0;
            _throttleVolume = 0.0f;
            _soundCrash.Restart(loop: false);
            _soundEngine.Stop();
            _soundEngine.SeekToStart();
            _soundEngine.SetPanPercent(0);
            if (_soundThrottle != null)
            {
                _soundThrottle.Stop();
                _soundThrottle.SeekToStart();
                _soundThrottle.SetPanPercent(0);
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
            PushEvent(CarEventType.CarRestart, _soundCrash.GetLengthSeconds() + 1.25f);
            _listener?.OnCrash();
            _effectEngine?.Stop();
            _effectCrash?.Play();
            _effectCurbLeft?.Stop();
            _effectCurbRight?.Stop();
        }

        public void MiniCrash(int newPosition)
        {
            _speed /= 4;
            if (_effectBumpLeft != null && _positionX < newPosition)
                _effectBumpLeft.Play();
            if (_effectBumpRight != null && _positionX > newPosition)
                _effectBumpRight.Play();

            _positionX = newPosition;
            _throttleVolume = 0.0f;
            _soundMiniCrash.SeekToStart();
            _soundMiniCrash.Play(loop: false);
        }

        public void Bump(int bumpX, int bumpY, int bumpSpeed)
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
                _effectBumpLeft?.Play();
            }
            else if (bumpX < 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
                _effectBumpRight?.Play();
            }

            if (_speed < 0)
                _speed = 0;
            _soundBump.Play(loop: false);
        }

        public void Stop()
        {
            _soundBrake.Stop();
            _soundWipers?.Stop();
            _effectCurbLeft?.Stop();
            _effectCurbRight?.Stop();
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
            _effectCurbLeft?.Stop();
            _effectCurbRight?.Stop();
            _effectEngine?.Stop();
        }
        public void Run(float elapsed)
        {
            var horning = _input.GetHorn();

            if (_state == CarState.Running && _started())
            {
                _currentSteering = _input.GetSteering();
                _currentThrottle = _input.GetThrottle();
                _currentBrake = _input.GetBrake();
                var gearUp = _input.GetGearUp();
                var gearDown = _input.GetGearDown();

                _currentAcceleration = _acceleration;
                _currentDeceleration = _deceleration;
                _speedDiff = 0;
                switch (_surface)
                {
                    case TrackSurface.Gravel:
                        _currentAcceleration = (_currentAcceleration * 2) / 3;
                        _currentDeceleration = (_currentDeceleration * 2) / 3;
                        break;
                    case TrackSurface.Water:
                        _currentAcceleration = (_currentAcceleration * 3) / 5;
                        _currentDeceleration = (_currentDeceleration * 3) / 5;
                        break;
                    case TrackSurface.Sand:
                        _currentAcceleration = _currentAcceleration / 2;
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
                    _factor1 = CalculateAcceleration();
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

                _factor2 = 1.0;
                if (_currentSteering != 0 && _speed > _topSpeed / 2)
                    _factor2 = 1.0 - (1.5 * _speed / _topSpeed) * Math.Abs(_currentSteering) / 100.0;

                if (_thrust > 10)
                {
                    _speedDiff = (int)(elapsed * _thrust * _currentAcceleration * _factor1 * _factor2 / 100);
                    if (_backfirePlayed)
                        _backfirePlayed = false;
                }
                else if (_thrust < -10)
                {
                    _speedDiff = (int)(elapsed * _thrust * _currentDeceleration);
                }
                else
                {
                    _speedDiff = (int)(elapsed * -1000);
                }

                if (_speedDiff > 0)
                    _speedDiff = (int)(_speedDiff * (2.0f - ((_topSpeed + _speed) * 1.0f / (2.0f * _topSpeed))));
                _speed += _speedDiff;
                if (_speed > _topSpeed)
                    _speed = _topSpeed;
                if (_speed < 0)
                    _speed = 0;

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
                    _effectSpring?.Gain(5000 * _speed / _topSpeed);
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

                _positionY += (int)(_speed * elapsed);
                if (_surface != TrackSurface.Snow)
                {
                    _positionX += (int)(_currentSteering * elapsed * _steering * ((5000.0f + _speed * _steeringFactor / 100.0f) / _topSpeed));
                }
                else
                {
                    _positionX += (int)(_currentSteering * elapsed * (_steering * 1.44f) * ((50000.0f + _speed * _steeringFactor / 100.0f) / _topSpeed));
                }

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    _brakeFrequency = 11025 + 22050 * _speed / _topSpeed;
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    if (_speed <= 5000)
                        _soundBrake.SetVolumePercent(100 - (50 - (_speed / 100)));
                    else
                        _soundBrake.SetVolumePercent(100);
                    if (_manualTransmission)
                        UpdateEngineFreqManual();
                    else
                        UpdateEngineFreq();
                    UpdateSoundRoad();
                    if (_effectGravel != null)
                    {
                        if (_surface == TrackSurface.Gravel)
                            _effectGravel.Gain(_speed * 10000 / _topSpeed);
                        else
                            _effectGravel.Gain(0);
                    }
                    if (_effectSpring != null)
                    {
                        if (_speed == 0)
                            _effectSpring.Gain(10000);
                        else
                            _effectSpring.Gain(10000 * _speed / _topSpeed);
                    }
                    if (_effectEngine != null)
                    {
                        if (_speed < _topSpeed / 10)
                            _effectEngine.Gain(10000 - _speed * 10 / _topSpeed);
                        else
                            _effectEngine.Gain(0);
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
                _speed -= (int)(elapsed * 100 * _deceleration);
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
                            _effectStart?.Stop();
                            _soundEngine.Play(loop: true);
                            _soundWipers?.Play(loop: true);
                            _state = CarState.Running;
                            break;
                        case CarEventType.CarRestart:
                            _effectCrash?.Stop();
                            Start();
                            break;
                        case CarEventType.InGear:
                            _switchingGear = 0;
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
                    if (_speed <= 5000)
                        _soundGravel.SetVolumePercent(100 - (10 - (_speed / 500)));
                    else
                        _soundGravel.SetVolumePercent(100);
                    break;
                case TrackSurface.Water:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 5000)
                        _soundWater.SetVolumePercent(100 - (10 - (_speed / 500)));
                    else
                        _soundWater.SetVolumePercent(100);
                    break;
                case TrackSurface.Sand:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 5000)
                        _soundSand.SetVolumePercent(100 - (10 - (_speed / 500)));
                    else
                        _soundSand.SetVolumePercent(100);
                    break;
                case TrackSurface.Snow:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 5000)
                        _soundSnow.SetVolumePercent(100 - (10 - (_speed / 500)));
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

        public void Evaluate(Track.Road road)
        {
            if (_state == CarState.Stopped)
            {
                if (_frame % 4 == 0)
                {
                    _relPos = (_positionX - road.Left) / (float)_laneWidth;
                    _panPos = CalculatePan(_relPos);
                    _soundStart.SetPanPercent(_panPos);
                    _soundEngine.SetPanPercent(_panPos);
                    _soundHorn.SetPanPercent(_panPos);
                    _soundWipers?.SetPanPercent(_panPos);
                }
            }

            if (_state == CarState.Running && _started())
            {
                if (_frame % 4 == 0)
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
                    _relPos = (_positionX - road.Left) / (float)_laneWidth;
                    _panPos = CalculatePan(_relPos);
                    ApplyPan(_panPos);

                    if (_effectCurbLeft != null)
                    {
                        if (_relPos < 0.05 && _speed > _topSpeed / 10)
                            _effectCurbLeft.Play();
                        else
                            _effectCurbLeft.Stop();
                    }
                    if (_effectCurbRight != null)
                    {
                        if (_relPos > 0.95 && _speed > _topSpeed / 10)
                            _effectCurbRight.Play();
                        else
                            _effectCurbRight.Stop();
                    }
                    if (_relPos < 0 || _relPos > 1)
                    {
                        if (_speed < _topSpeed / 2)
                            MiniCrash((road.Right + road.Left) / 2);
                        else
                            Crash();
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
            _effectStart?.Dispose();
            _effectCrash?.Dispose();
            _effectSpring?.Dispose();
            _effectEngine?.Dispose();
            _effectCurbLeft?.Dispose();
            _effectCurbRight?.Dispose();
            _effectGravel?.Dispose();
            _effectBumpLeft?.Dispose();
            _effectBumpRight?.Dispose();
        }
        private void PushEvent(CarEventType type, float time)
        {
            _events.Add(new CarEvent
            {
                Type = type,
                Time = _currentTime() + time
            });
        }

        private void UpdateEngineFreq()
        {
            var gearRange = _topSpeed / (_gears + 1);
            if ((_speed / gearRange) < 2)
            {
                var gearSpeed = _speed / (2.0f * gearRange);
                _frequency = (int)(gearSpeed * (_topFreq - _idleFreq)) + _idleFreq;
            }
            else
            {
                _gear = _speed / gearRange;
                if (_gear > _gears)
                    _gear = _gears;
                var gearSpeed = (_speed - _gear * gearRange) / (float)gearRange;
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

        private void UpdateEngineFreqManual()
        {
            var gearRange = _topSpeed / _gears;
            if (_gear == 1)
            {
                if (_speed < (int)((4.0f / 3.0f) * gearRange))
                {
                    _frequency = _idleFreq + (int)((_speed * 3.0f / (2 * gearRange)) * (_topFreq - _idleFreq));
                }
                else
                {
                    _frequency = _idleFreq + 2 * (_topFreq - _idleFreq);
                }
            }
            else
            {
                var shiftPoint = ((2.0f / 3.0f) + (_gear - 1)) * gearRange;
                _frequency = (int)((_speed / shiftPoint) * _topFreq);
                if (_frequency > 2 * _topFreq)
                    _frequency = 2 * _topFreq;
                if (_frequency < _idleFreq / 2)
                    _frequency = _idleFreq / 2;
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

        private int CalculateAcceleration()
        {
            var gearSpeed = _topSpeed / _gears;
            var gearCenter = (int)(gearSpeed * (_gear - 0.82f));
            _speedDiff = _speed - gearCenter;
            var relSpeedDiff = _speedDiff / (float)gearSpeed;
            if (Math.Abs(relSpeedDiff) < 1.9f)
            {
                var acceleration = (int)(100.0f * (0.5f + Math.Cos(relSpeedDiff * Math.PI * 0.5f)));
                return acceleration < 5 ? 5 : acceleration;
            }
            else
            {
                var acceleration = (int)(100.0f * (0.5f + Math.Cos(0.95f * Math.PI)));
                return acceleration < 5 ? 5 : acceleration;
            }
        }

        private void UpdateSoundRoad()
        {
            _surfaceFrequency = _speed * 5;
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
            _soundEngine.SetPanPercent(pan);
            _soundThrottle?.SetPanPercent(pan);
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
            var pan = (relPos - 1.0f) * 100.0f;
            if (pan < -100.0f) pan = -100.0f;
            if (pan > 100.0f) pan = 100.0f;
            return (int)pan;
        }

        private AudioSourceHandle CreateRequiredSound(string? path, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Sound path not provided.");
            if (!File.Exists(path))
                throw new FileNotFoundException("Sound file not found.", path);
            return looped ? _audio.CreateLoopingSource(path!) : _audio.CreateSource(path!, streamFromDisk: true);
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            return looped ? _audio.CreateLoopingSource(path!) : _audio.CreateSource(path!, streamFromDisk: true);
        }

        private sealed class CarEvent
        {
            public float Time { get; set; }
            public CarEventType Type { get; set; }
        }

        private enum CarEventType
        {
            CarStart,
            CarRestart,
            InGear
        }
    }
}
