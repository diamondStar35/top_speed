using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Vehicles.Events;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void HandleEventCarStart()
        {
            if (_combustionState != EngineCombustionState.Starting)
                return;
            if (!CanStartEngineWithFuel())
            {
                _combustionState = EngineCombustionState.Off;
                SetEngineRotationState(EngineRotationState.Stopped);
                SetState(CarState.Stopped);
                PlayFuelStartBlockedCue();
                return;
            }

            _soundEngine.SetFrequency(_idleFreq);
            _soundThrottle?.SetFrequency(_idleFreq);
            _vibration?.StopEffect(VibrationEffectType.Start);
            _soundWipers?.Play(loop: true);
            ClearStallState();
            _engine.StartEngine();
            _combustionState = EngineCombustionState.On;
            SetEngineRotationState(EngineRotationState.FreeSpinning);
            SetState(CarState.Running);
        }

        private void HandleEventCarRestart()
        {
            _vibration?.StopEffect(VibrationEffectType.Crash);
            Start();
        }

        private void HandleEventCrashComplete()
        {
            _vibration?.StopEffect(VibrationEffectType.Crash);
            SetState(CarState.Crashed);
        }

        private void HandleEventInGear()
        {
            _switchingGear = 0;
        }

        private void HandleEventStopVibration(VibrationEffectType effect)
        {
            _vibration?.StopEffect(effect);
        }

        private void HandleEventStopBumpVibration()
        {
            _vibration?.StopEffect(VibrationEffectType.BumpLeft);
            _vibration?.StopEffect(VibrationEffectType.BumpRight);
        }

        private void PushEvent(EventType type, float time, VibrationEffectType? effect = null)
        {
            _events.Push(_currentTime() + time, type, effect);
        }
    }
}

