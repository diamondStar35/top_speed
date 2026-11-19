using TopSpeed.Common;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Events;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private const int ManualShiftClutchThreshold = 70;

        public void SetNeutralGear()
        {
            _gear = NeutralGear;
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
        }

        private void HandleTransmissionInput(in CarControlIntent intent)
        {
            if (!intent.GearUp && !intent.GearDown)
                _stickReleased = true;

            if (_manualTransmission)
            {
                HandleManualShift(intent.GearUp, intent.GearDown, intent.Clutch);
                return;
            }

            if (IsShiftOnDemandActive())
            {
                HandleShiftOnDemandShift(intent.GearUp, intent.GearDown);
                return;
            }

            HandleAutomaticDirectionShift(intent.GearUp, intent.GearDown);
        }

        private void HandleManualShift(bool gearUp, bool gearDown, int clutch)
        {
            if (gearDown && _stickReleased)
            {
                if (!CanShiftManual(clutch))
                {
                    _stickReleased = false;
                    _soundBadSwitch.Play(loop: false);
                    return;
                }

                if (_gear > FirstForwardGear)
                {
                    _stickReleased = false;
                    _switchingGear = -1;
                    --_gear;
                    if (_soundEngine.GetPitch() > 3f * _topFreq / (2f * _soundEngine.InputSampleRate))
                        _soundBadSwitch.Play(loop: false);
                    if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                        PlayRandomBackfire();
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear == FirstForwardGear)
                {
                    _stickReleased = false;
                    _switchingGear = -1;
                    _gear = NeutralGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear == NeutralGear)
                {
                    _stickReleased = false;
                    if (_speed <= ReverseShiftMaxSpeedKmh)
                    {
                        _switchingGear = -1;
                        _gear = ReverseGear;
                        PushEvent(EventType.InGear, 0.2f);
                    }
                    else
                    {
                        _soundBadSwitch.Play(loop: false);
                    }
                }
            }
            else if (gearUp && _stickReleased)
            {
                if (!CanShiftManual(clutch))
                {
                    _stickReleased = false;
                    _soundBadSwitch.Play(loop: false);
                    return;
                }

                if (_gear == ReverseGear)
                {
                    _stickReleased = false;
                    _switchingGear = 1;
                    _gear = NeutralGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear == NeutralGear)
                {
                    _stickReleased = false;
                    _switchingGear = 1;
                    _gear = FirstForwardGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear < _gears)
                {
                    _stickReleased = false;
                    _switchingGear = 1;
                    ++_gear;
                    if (_soundEngine.GetPitch() < _idleFreq / (float)_soundEngine.InputSampleRate)
                        _soundBadSwitch.Play(loop: false);
                    if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                        PlayRandomBackfire();
                    PushEvent(EventType.InGear, 0.2f);
                }
            }
        }

        private static bool CanShiftManual(int clutch)
        {
            return clutch >= ManualShiftClutchThreshold;
        }

        private void HandleShiftOnDemandShift(bool gearUp, bool gearDown)
        {
            if (gearDown && _stickReleased)
            {
                _stickReleased = false;

                if (_gear > FirstForwardGear)
                {
                    _switchingGear = -1;
                    --_gear;
                    if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                        PlayRandomBackfire();
                    PushEvent(EventType.InGear, 0.2f);
                    return;
                }

                if (_gear == FirstForwardGear)
                {
                    _switchingGear = -1;
                    _gear = NeutralGear;
                    PushEvent(EventType.InGear, 0.2f);
                    return;
                }

                if (_gear == NeutralGear)
                {
                    if (_speed <= ReverseShiftMaxSpeedKmh)
                    {
                        _switchingGear = -1;
                        _gear = ReverseGear;
                        PushEvent(EventType.InGear, 0.2f);
                    }
                    else
                    {
                        _currentThrottle = 0;
                        _currentBrake = -100;
                        _soundBadSwitch.Play(loop: false);
                    }
                }
            }
            else if (gearUp && _stickReleased)
            {
                _stickReleased = false;
                if (_gear == ReverseGear)
                {
                    _switchingGear = 1;
                    _gear = NeutralGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear == NeutralGear)
                {
                    _switchingGear = 1;
                    _gear = FirstForwardGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear < _gears)
                {
                    _switchingGear = 1;
                    ++_gear;
                    if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                        PlayRandomBackfire();
                    PushEvent(EventType.InGear, 0.2f);
                }
            }
        }

        private void HandleAutomaticDirectionShift(bool gearUp, bool gearDown)
        {
            if (gearDown && _stickReleased)
            {
                _stickReleased = false;
                if (_gear == FirstForwardGear)
                {
                    _switchingGear = -1;
                    _gear = NeutralGear;
                    PushEvent(EventType.InGear, 0.2f);
                    return;
                }

                if (_gear == NeutralGear)
                {
                    if (_speed <= ReverseShiftMaxSpeedKmh)
                    {
                        _switchingGear = -1;
                        _gear = ReverseGear;
                        PushEvent(EventType.InGear, 0.2f);
                    }
                    else
                    {
                        _currentThrottle = 0;
                        _currentBrake = -100;
                        _soundBadSwitch.Play(loop: false);
                    }
                }
            }
            else if (gearUp && _stickReleased)
            {
                _stickReleased = false;
                if (_gear == ReverseGear)
                {
                    _switchingGear = 1;
                    _gear = NeutralGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear == NeutralGear)
                {
                    _switchingGear = 1;
                    _gear = FirstForwardGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
            }
        }
    }
}

