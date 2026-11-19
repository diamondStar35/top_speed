using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        public virtual void Evaluate(Track.Road road)
        {
            var roadWidth = road.Right - road.Left;
            if (roadWidth > 0f)
                _laneWidth = roadWidth;
            else
                roadWidth = _laneWidth;

            var updateAudioThisFrame = true;

            if (_state == CarState.Stopped || _state == CarState.Starting || _state == CarState.Crashed)
            {
                if (updateAudioThisFrame)
                {
                    _relPos = roadWidth <= 0f
                        ? 0.5f
                        : (_positionX - road.Left) / roadWidth;
                    _panPos = CalculatePan(_relPos);
                    _soundStart.SetPanPercent(_panPos);
                    _soundStop?.SetPanPercent(_panPos);
                    _soundHorn.SetPanPercent(_panPos);
                    _soundWipers?.SetPanPercent(_panPos);
                    UpdateSpatialAudio(road);
                }
            }

            if (_state == CarState.Running && _started())
            {
                if (updateAudioThisFrame)
                {
                    UpdateSpatialAudio(road);
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
                if (updateAudioThisFrame)
                {
                    _relPos = roadWidth <= 0f
                        ? 0.5f
                        : (_positionX - road.Left) / roadWidth;
                    _panPos = CalculatePan(_relPos);
                    _soundStart.SetPanPercent(_panPos);
                    _soundStop?.SetPanPercent(_panPos);
                    _soundHorn.SetPanPercent(_panPos);
                    _soundWipers?.SetPanPercent(_panPos);
                    UpdateSpatialAudio(road);
                }
            }
            _frame++;
        }

    }
}

