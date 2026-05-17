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

            var angle = normalized * (float)(Math.PI / 2.0);

            var enginePos = PlaceOnArc(listenerX, listenerZ, angle, engineForwardOffset);
            var brakeForwardOffset = Math.Max(0.01f, engineForwardOffset * 0.6f);
            var brakePos = PlaceOnArc(listenerX, listenerZ, angle, brakeForwardOffset);
            var vehiclePos = PlaceOnArc(listenerX, listenerZ, angle, vehicleForwardOffset);
            var crashPos = vehiclePos;
            if (_state == CarState.Crashing || _state == CarState.Crashed || _state == CarState.Starting)
            {
                crashPos = new Vector3(
                    AudioWorld.ToMeters(listenerX),
                    0f,
                    AudioWorld.ToMeters(listenerZ));
            }

            SetSpatial("player.engine", _soundEngine, enginePos, velocity);
            SetSpatial("player.throttle", _soundThrottle, enginePos, velocity);
            SetSpatial("player.horn", _soundHorn, enginePos, velocity);
            SetSpatial("player.brake", _soundBrake, brakePos, velocity);
            SetSpatial("player.backfire", _soundBackfire, enginePos, velocity);
            SetSpatial("player.start", _soundStart, enginePos, velocity);
            SetSpatial("player.stop", _soundStop, enginePos, velocity);
            SetSpatial("player.crash", _soundCrash, crashPos, velocity);
            SetSpatial("player.miniCrash", _soundMiniCrash, vehiclePos, velocity);
            SetSpatial("player.bump", _soundBump, vehiclePos, velocity);
            SetSpatial("player.badSwitch", _soundBadSwitch, enginePos, velocity);
            SetSpatial("player.fuelWarning", _soundFuelWarning, enginePos, velocity);
            SetSpatial("player.wipers", _soundWipers, vehiclePos, velocity);

            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    SetSpatial("player.surface.asphalt", _soundAsphalt, vehiclePos, velocity);
                    break;
                case TrackSurface.Gravel:
                    SetSpatial("player.surface.gravel", _soundGravel, vehiclePos, velocity);
                    break;
                case TrackSurface.Water:
                    SetSpatial("player.surface.water", _soundWater, vehiclePos, velocity);
                    break;
                case TrackSurface.Sand:
                    SetSpatial("player.surface.sand", _soundSand, vehiclePos, velocity);
                    break;
                case TrackSurface.Snow:
                    SetSpatial("player.surface.snow", _soundSnow, vehiclePos, velocity);
                    break;
            }
        }

        private static Vector3 PlaceOnArc(float listenerX, float listenerZ, float angle, float forwardOffset)
        {
            var radius = Math.Abs(forwardOffset);
            if (radius < 0.01f)
                radius = 0.01f;

            var offsetX = (float)Math.Sin(angle) * radius;
            var offsetZ = (float)Math.Cos(angle) * radius;
            if (forwardOffset < 0f)
                offsetZ = -offsetZ;

            return new Vector3(
                AudioWorld.ToMeters(listenerX + offsetX),
                0f,
                AudioWorld.ToMeters(listenerZ + offsetZ));
        }

        private static void SetSpatial(string slot, Source? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetTransform(position, velocity);
        }
    }
}

