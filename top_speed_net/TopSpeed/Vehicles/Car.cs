using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Core;
using TopSpeed.Vehicles.Physics;

namespace TopSpeed.Vehicles
{
    internal partial class Car : CarBase, ICar
    {
        public CarState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float Speed => _speed;
        public int Frequency => _frequency;
        public int Gear => _gear;
        public bool InReverseGear => _gear == ReverseGear;
        public bool ManualTransmission
        {
            get => _manualTransmission;
            set => ApplyTransmissionRequest(value);
        }
        public bool ShiftOnDemandSupported => _shiftOnDemandSupported && TransmissionTypes.IsAutomaticFamily(_activeTransmissionType);
        public bool ShiftOnDemandEnabled => IsShiftOnDemandActive();
        public CarType CarType => _carType;
        public ICarListener? Listener
        {
            get => _listener;
            set => _listener = value;
        }
        public bool CombustionActive => _combustionState != EngineCombustionState.Off;
        public bool EngineRunning => _engineRotationState != EngineRotationState.Stopped;
        public bool Braking => _soundBrake.IsPlaying;
        public bool Horning => _soundHorn.IsPlaying;
        public bool UserDefined => _userDefined;
        public string? CustomFile => _customFile;
        public string VehicleName { get; private set; } = LocalizationService.Mark("Vehicle");
        public float WidthM => _widthM;
        public float LengthM => _lengthM;
        public float MassKg => _massKg;

        public float SpeedKmh => _engine.SpeedKmh;
        public float EngineRpm => _engine.Rpm;
        public float EngineHorsepower => _engine.Horsepower;
        public float EngineGrossHorsepower => _engine.GrossHorsepower;
        public float EngineNetHorsepower => _engine.NetHorsepower;
        public float DistanceMeters => _engine.DistanceMeters;
        public float FuelLitersRemaining => _fuelState.RemainingLiters;
        public float FuelTankCapacityLiters => _fuelTankCapacityLiters;
        public float FuelBurnLitersPerHour => _fuelBurnLitersPerHour;
        public float FuelEstimatedRangeMeters => _fuelEstimatedRangeMeters;
        public float FuelEfficiencyLitersPer100Km => _fuelEfficiencyLitersPer100Km;
        public float FuelEfficiencyMpg => _fuelEfficiencyMpg;
        public bool FuelLow => _fuelLow;
        public bool FuelEmpty => _fuelEmpty;

        public void SetPhysicsModel(IModel model)
        {
            _physicsModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        private CarControlContext BuildControlContext(float elapsed)
        {
            return new CarControlContext(
                _state,
                _started(),
                _manualTransmission,
                _gear,
                _speed,
                _positionX,
                _positionY,
                elapsed);
        }

        private void UpdateRuntimeContext(float elapsed)
        {
            _runtimeContext.State = _state;
            _runtimeContext.Started = _started();
            _runtimeContext.ManualTransmission = _manualTransmission;
            _runtimeContext.Gear = _gear;
            _runtimeContext.Speed = _speed;
            _runtimeContext.PositionX = _positionX;
            _runtimeContext.PositionY = _positionY;
            _runtimeContext.Elapsed = elapsed;
        }

        private void SetState(CarState nextState)
        {
            var previousState = _state;
            _state = nextState;
            NotifyStateChanged(previousState, nextState);
        }

        public virtual void Run(float elapsed)
        {
            RefreshCategoryVolumes();
            _lastAudioElapsed = elapsed;
            var controlContext = BuildControlContext(elapsed);
            var controlIntent = ResolveControlIntent(controlContext);
            OnBeforeRun(elapsed, controlContext, controlIntent);
            var horning = controlIntent.Horn;

            _physicsModel.Step(this, elapsed, controlIntent);

            _audioFlow.UpdateHorn(_soundHorn, _state, horning);
            _eventProcessor.ProcessDue(_events, _currentTime());

            UpdateRuntimeContext(elapsed);
            OnAfterRun(elapsed, BuildControlContext(elapsed), controlIntent);
        }

        private void ApplyTransmissionRequest(bool manualRequested)
        {
            if (!TransmissionSelect.TryResolveRequested(
                    automaticRequested: !manualRequested,
                    _primaryTransmissionType,
                    _supportedTransmissionTypes,
                    out var resolved))
            {
                resolved = _primaryTransmissionType;
            }

            _activeTransmissionType = resolved;
            _manualTransmission = resolved == TransmissionType.Manual;
            _shiftOnDemandEnabled = false;
        }

        public bool ToggleShiftOnDemand()
        {
            if (!ShiftOnDemandSupported)
                return false;

            _shiftOnDemandEnabled = !_shiftOnDemandEnabled;
            return true;
        }

        private bool IsShiftOnDemandActive()
        {
            return _shiftOnDemandEnabled
                && _shiftOnDemandSupported
                && !_manualTransmission
                && TransmissionTypes.IsAutomaticFamily(_activeTransmissionType);
        }
    }
}

