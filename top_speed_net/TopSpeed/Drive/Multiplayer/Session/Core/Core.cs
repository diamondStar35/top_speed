using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Network;
using TopSpeed.Network.Live;
using TopSpeed.Runtime;
using TopSpeed.Speech;
using TopSpeed.Vehicles.Control;
using AudioSource = TS.Audio.Source;
using NetworkSession = TopSpeed.Network.MultiplayerSession;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession : IDisposable
    {
        public MultiplayerSession(
            AudioManager audio,
            SpeechService speech,
            DriveSettings settings,
            DriveInput input,
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            string? vehicleName,
            IVibrationDevice? vibrationDevice,
            IFileDialogs fileDialogs,
            NetworkSession network,
            uint raceInstanceId,
            Func<byte, string> resolvePlayerName,
            Func<byte, string?>? resolveRemoteVehicleFile = null,
            Func<byte, string?>? resolveRemoteVehicleName = null)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _vibrationDevice = vibrationDevice;
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _raceAudio = new RaceAudioFactory(_audio);
            _raceInstanceId = raceInstanceId;
            _resolvePlayerName = resolvePlayerName ?? throw new ArgumentNullException(nameof(resolvePlayerName));
            _resolveRemoteVehicleFile = resolveRemoteVehicleFile;
            _resolveRemoteVehicleName = resolveRemoteVehicleName;
            _localVehicleName = string.IsNullOrWhiteSpace(vehicleName) ? null : vehicleName;
            _finishLockController = new FinishLockInputController(input);
            _soundQueue = new Queue();
            _raceInfoQueue = new Queue();
            _trackInfoQueue = new Queue();
            _manualTransmission = !automaticTransmission;
            _lapLimit = laps;
            _participants = new ParticipantState(MaxPlayers);
            _snapshots = new SnapshotState(SnapshotBufferMax);
            _runtime = new RuntimeState();
            _soundPosition = new AudioSource?[MaxPlayers];
            _soundPlayerNr = new AudioSource?[MaxPlayers];
            _soundPlayerNrInfo = new AudioSource?[MaxPlayers];
            _soundFinished = new AudioSource?[MaxPlayers];
            _liveTx = new Tx(_network);

            var runtimeObjects = CreateRuntimeObjects(trackName, trackData, vehicleIndex, vehicleFile);
            _track = runtimeObjects.Track;
            _car = runtimeObjects.Car;
            _localRadio = runtimeObjects.LocalRadio;
            _radioPanel = runtimeObjects.RadioPanel;
            _panelManager = runtimeObjects.PanelManager;

            _soundNumbers = CreateNumberSounds();
            _soundLaps = CreateLapSounds(_lapLimit);
            (_randomSounds, _totalRandomSounds) = CreateRandomSoundContainers();
            _randomSoundBaseNames = new string?[RandomSoundGroups];
            ConfigureDefaultRandomSounds();
            _soundUnkey = CreateUnkeySounds();
            _soundStart = LoadLanguageSound("race\\start321");
            LoadRaceUiSounds();
            PreloadRaceSpeechSources();

            var subsystems = CreateSubsystems();
            _trackAudio = subsystems.TrackAudio;
            _panels = subsystems.Panels;
            _coreRequests = subsystems.CoreRequests;
            _generalRequests = subsystems.GeneralRequests;
            _vehicle = subsystems.Vehicle;
            _progress = subsystems.Progress;
            _sync = subsystems.Sync;
            _commentary = subsystems.Commentary;
            _playerInfo = subsystems.PlayerInfo;
            _exit = subsystems.Exit;

            _session = CreateSession();
        }

        public void ReplaceNetwork(NetworkSession network)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _liveTx.ReplaceSession(network);
        }
    }
}
