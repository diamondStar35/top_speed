using System;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles.Core
{
    internal static class CarFactory
    {
        public static ICar CreateDefault(
            RaceAudioFactory raceAudio,
            Track track,
            DriveInput input,
            DriveSettings settings,
            int vehicleIndex,
            string? vehicleFile,
            Func<float> currentTime,
            Func<bool> started,
            IVibrationDevice? vibrationDevice = null,
            RacePhysicsToggles? physicsToggles = null)
        {
            return new RaceCar(raceAudio, track, input, settings, vehicleIndex, vehicleFile, currentTime, started, vibrationDevice, physicsToggles);
        }
    }
}


