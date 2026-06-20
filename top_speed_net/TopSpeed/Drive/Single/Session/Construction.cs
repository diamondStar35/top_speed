using System;
using TopSpeed.Data;
using TopSpeed.Drive.Panels;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Core;
using TS.Audio;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
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

        private Source?[] CreateNumberSounds()
        {
            return new Source?[101];
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

        private void CreateComputerPlayers()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var botNumber = i;
                if (botNumber >= _playerNumber)
                    botNumber++;

                _computerPlayers[i] = new ComputerPlayer(
                    _audio,
                    _raceAudio,
                    _track,
                    _settings,
                    TopSpeed.Common.Algorithm.RandomInt(VehicleCatalog.VehicleCount),
                    botNumber,
                    () => _session.Context.RuntimeSeconds,
                    () => _started,
                    null);
            }
        }
    }
}

