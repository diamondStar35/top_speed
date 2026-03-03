using System;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void UpdateSpatialAudio(Track.Road road)
        {
            var elapsed = _lastAudioElapsed;
            if (elapsed <= 0f)
                return;

            var worldX = _positionX;
            var worldZ = _positionY;

            var velocity = Vector3.Zero;
            var velUnits = Vector3.Zero;
            if (_audioInitialized && elapsed > 0f)
            {
                velUnits = new Vector3((worldX - _lastAudioX) / elapsed, 0f, (worldZ - _lastAudioY) / elapsed);
                velocity = AudioWorld.ToMeters(velUnits);
            }
            _lastAudioX = worldX;
            _lastAudioY = worldZ;
            _audioInitialized = true;

            var left = Math.Min(road.Left, road.Right);
            var right = Math.Max(road.Left, road.Right);
            var centerX = (left + right) * 0.5f;
            if (!IsFinite(centerX))
                centerX = worldX;

            var trackHalfWidth = (right - left) * 0.5f;
            if (!IsFinite(trackHalfWidth) || trackHalfWidth <= 0.01f)
                trackHalfWidth = Math.Max(0.01f, _laneWidth);

            var clampedX = worldX;
            var minX = centerX - trackHalfWidth;
            var maxX = centerX + trackHalfWidth;
            if (clampedX < minX)
                clampedX = minX;
            else if (clampedX > maxX)
                clampedX = maxX;

            var normalized = (clampedX - centerX) / trackHalfWidth;
            if (!IsFinite(normalized))
                normalized = 0f;
            if (normalized < -1f)
                normalized = -1f;
            else if (normalized > 1f)
                normalized = 1f;

            var driverOffsetX = -_widthM * 0.25f;
            var driverOffsetZ = _lengthM * 0.1f;
            var listenerX = worldX + driverOffsetX;
            var listenerZ = worldZ + driverOffsetZ;

            var engineOffsetZ = _lengthM * 0.35f;
            var engineForwardOffset = engineOffsetZ - driverOffsetZ;
            if (engineForwardOffset < 0.01f)
                engineForwardOffset = 0.01f;

            var vehicleForwardOffset = -driverOffsetZ;
            if (Math.Abs(vehicleForwardOffset) < 0.01f)
                vehicleForwardOffset = vehicleForwardOffset >= 0f ? 0.01f : -0.01f;

            // Maps the sound to a 180-degree arc relative to the listener.
            var angle = normalized * (float)(Math.PI / 2.0);

            var enginePos = PlaceOnArc(listenerX, listenerZ, angle, engineForwardOffset, engineForwardOffset);
            var brakePos = PlaceOnArc(listenerX, listenerZ, angle, 
                _settings.StereoWidening ? engineForwardOffset : brakeForwardOffset, 
                brakeForwardOffset);
            var vehiclePos = PlaceOnArc(listenerX, listenerZ, angle, 
                _settings.StereoWidening ? engineForwardOffset : vehicleForwardOffset, 
                vehicleForwardOffset);
            var crashPos = vehiclePos;
            if (_state == CarState.Crashing || _state == CarState.Crashed || _state == CarState.Starting)
            {
                crashPos = new Vector3(
                    AudioWorld.ToMeters(listenerX),
                    0f,
                    AudioWorld.ToMeters(listenerZ));
            }

            SetSpatial(_soundEngine, enginePos, velocity);
            SetSpatial(_soundThrottle, enginePos, velocity);
            SetSpatial(_soundHorn, enginePos, velocity);
            SetSpatial(_soundBrake, brakePos, velocity);
            SetSpatial(_soundBackfire, enginePos, velocity);
            SetSpatial(_soundStart, enginePos, velocity);
            SetSpatial(_soundCrash, crashPos, velocity);
            SetSpatial(_soundMiniCrash, vehiclePos, velocity);
            SetSpatial(_soundBump, vehiclePos, velocity);
            SetSpatial(_soundBadSwitch, enginePos, velocity);
            SetSpatial(_soundWipers, vehiclePos, velocity);

            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    SetSpatial(_soundAsphalt, vehiclePos, velocity);
                    break;
                case TrackSurface.Gravel:
                    SetSpatial(_soundGravel, vehiclePos, velocity);
                    break;
                case TrackSurface.Water:
                    SetSpatial(_soundWater, vehiclePos, velocity);
                    break;
                case TrackSurface.Sand:
                    SetSpatial(_soundSand, vehiclePos, velocity);
                    break;
                case TrackSurface.Snow:
                    SetSpatial(_soundSnow, vehiclePos, velocity);
                    break;
            }
        }

        private static Vector3 PlaceOnArc(float listenerX, float listenerZ, float angle, float horizontalRadius, float forwardOffset)
        {
            var hRadius = Math.Abs(horizontalRadius);
            if (hRadius < 0.01f)
                hRadius = 0.01f;

            var zRadius = Math.Abs(forwardOffset);
            if (zRadius < 0.01f)
                zRadius = 0.01f;

            var offsetX = (float)Math.Sin(angle) * hRadius;
            var offsetZ = (float)Math.Cos(angle) * zRadius;
            if (forwardOffset < 0f)
                offsetZ = -offsetZ;

            return new Vector3(
                AudioWorld.ToMeters(listenerX + offsetX),
                0f,
                AudioWorld.ToMeters(listenerZ + offsetZ));
        }

        private static void SetSpatial(AudioSourceHandle? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetPosition(position);
            sound.SetVelocity(velocity);
        }
    }
}
