using System;
using TopSpeed.Drive.Panels;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Core;
using TS.Audio;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
    {
        private (Track Track, ICar Car, VehicleRadioController LocalRadio, RadioVehiclePanel RadioPanel, VehiclePanelManager PanelManager)
            CreateRuntimeObjects(
                string track,
                int vehicleIndex,
                string? vehicleFile)
        {
            var loadedTrack = Track.Load(track, _audio);
            var car = CarFactory.CreateDefault(
                _raceAudio,
                loadedTrack,
                _input,
                _settings,
                vehicleIndex,
                vehicleFile,
                () => _session.Context.RuntimeSeconds,
                () => _started,
                _vibrationDevice,
                _physicsToggles);
            var localRadio = new VehicleRadioController(_audio);
            var radioPanel = new RadioVehiclePanel(
                _input,
                _audio,
                _settings,
                localRadio,
                _fileDialogs,
                NextLocalMediaId,
                SpeakText,
                (_, _) => { },
                (_, _, _) => { });
            var panelManager = new VehiclePanelManager(new IVehicleRacePanel[]
            {
                new ControlVehiclePanel(),
                radioPanel
            });

            return (loadedTrack, car, localRadio, radioPanel, panelManager);
        }

        private static int ApplyAdventureLapOverride(string track, int laps)
        {
            if (!string.IsNullOrWhiteSpace(track) &&
                track.IndexOf("adv", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1;
            }

            return laps;
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

        private Source[] CreateLapSounds(int laps)
        {
            var sounds = new Source[Math.Max(0, Math.Min(MaxLaps, laps) - 1)];
            for (var i = 0; i < sounds.Length; i++)
                sounds[i] = LoadLanguageSound($"race\\info\\laps2go{i + 1}");

            return sounds;
        }
    }
}

