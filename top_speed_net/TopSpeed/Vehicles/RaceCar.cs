using System;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal sealed class RaceCar : Car
    {
        public RaceCar(
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
            : base(raceAudio, track, input, settings, vehicleIndex, vehicleFile, currentTime, started, vibrationDevice, physicsToggles)
        {
        }
    }
}


