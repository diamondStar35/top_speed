using System;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Drive.Panels;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Core;
using TS.Audio;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private enum RandomSoundSlot
        {
            EasyLeft = 0,
            Left = 1,
            HardLeft = 2,
            HairpinLeft = 3,
            EasyRight = 4,
            Right = 5,
            HardRight = 6,
            HairpinRight = 7,
            Asphalt = 8,
            Gravel = 9,
            Water = 10,
            Sand = 11,
            Snow = 12,
            Finish = 13,
            Front = 14,
            Tail = 15
        }

        private (Track Track, ICar Car, VehicleRadioController LocalRadio, RadioVehiclePanel RadioPanel, VehiclePanelManager PanelManager)
            CreateRuntimeObjects(
                string trackName,
                TrackData trackData,
                int vehicleIndex,
                string? vehicleFile)
        {
            var loadedTrack = Track.LoadFromData(trackName, trackData, _audio, trackData.UserDefined);
            var car = CarFactory.CreateDefault(
                _raceAudio,
                loadedTrack,
                _input,
                _settings,
                vehicleIndex,
                vehicleFile,
                () => _session.Context.RuntimeSeconds,
                () => _started,
                _vibrationDevice);
            var localRadio = new VehicleRadioController(_audio);
            var radioPanel = new RadioVehiclePanel(
                _input,
                _audio,
                _settings,
                localRadio,
                _fileDialogs,
                NextLocalMediaId,
                SpeakText,
                HandleLocalRadioMediaLoaded,
                HandleLocalRadioPlaybackChanged);
            var panelManager = new VehiclePanelManager(new IVehicleRacePanel[]
            {
                new ControlVehiclePanel(),
                radioPanel
            });

            return (loadedTrack, car, localRadio, radioPanel, panelManager);
        }

        private Source?[] CreateNumberSounds()
        {
            return new Source?[101];
        }

        private static Source?[] CreateLapSoundSlots(int laps)
        {
            return new Source?[Math.Max(0, Math.Min(MaxLaps, laps) - 1)];
        }

        private Source? GetLapSound(int index)
        {
            if (index < 0 || index >= _soundLaps.Length)
                return null;

            return _soundLaps[index] ??= LoadLanguageSound($"race\\info\\laps2go{index + 1}");
        }

        private Source[] CreateUnkeySounds()
        {
            var sounds = new Source[MaxUnkeys];
            for (var i = 0; i < MaxUnkeys; i++)
                sounds[i] = LoadLegacySound($"unkey{i + 1}.wav");

            return sounds;
        }

        private static (Source?[][] Sounds, int[] Totals) CreateRandomSoundContainers()
        {
            var sounds = new Source?[RandomSoundGroups][];
            var totals = new int[RandomSoundGroups];
            for (var i = 0; i < RandomSoundGroups; i++)
                sounds[i] = new Source?[RandomSoundMax];

            return (sounds, totals);
        }
    }
}
