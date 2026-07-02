using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Drive.Single;
using TopSpeed.Drive.TimeTrial;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Network;
using TopSpeed.Drive;
using TopSpeed.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using DriveMultiplayerSession = TopSpeed.Drive.Multiplayer.MultiplayerSession;
using NetworkMultiplayerSession = TopSpeed.Network.MultiplayerSession;

namespace TopSpeed.Game
{
    internal interface IDriveSessionFactory
    {
        TimeTrialSession CreateTimeTrial(
            string track,
            string trackId,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice);

        SingleSession CreateSingleRace(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice);

        DriveMultiplayerSession CreateMultiplayer(
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            NetworkMultiplayerSession session,
            uint raceInstanceId,
            Func<byte, string> resolvePlayerName,
            Func<byte, string?> resolveRemoteVehicleFile);
    }

    internal sealed class DriveSessionFactory : IDriveSessionFactory
    {
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly DriveSettings _settings;
        private readonly DriveInput _driveInput;
        private readonly IFileDialogs _fileDialogs;

        public DriveSessionFactory(
            AudioManager audio,
            SpeechService speech,
            DriveSettings settings,
            DriveInput driveInput,
            IFileDialogs fileDialogs)
        {
            _audio = audio;
            _speech = speech;
            _settings = settings;
            _driveInput = driveInput;
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
        }

        public TimeTrialSession CreateTimeTrial(
            string track,
            string trackId,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
        {
            return new TimeTrialSession(
                _audio,
                _speech,
                _settings,
                _driveInput,
                track,
                trackId,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                _fileDialogs);
        }

        public SingleSession CreateSingleRace(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
        {
            return new SingleSession(
                _audio,
                _speech,
                _settings,
                _driveInput,
                track,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                _fileDialogs);
        }

        public DriveMultiplayerSession CreateMultiplayer(
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            NetworkMultiplayerSession session,
            uint raceInstanceId,
            Func<byte, string> resolvePlayerName,
            Func<byte, string?> resolveRemoteVehicleFile)
        {
            return new DriveMultiplayerSession(
                _audio,
                _speech,
                _settings,
                _driveInput,
                trackData,
                trackName,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                _fileDialogs,
                session,
                raceInstanceId,
                resolvePlayerName,
                resolveRemoteVehicleFile);
        }
    }
}



