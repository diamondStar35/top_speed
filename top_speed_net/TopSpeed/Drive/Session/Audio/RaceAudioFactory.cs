using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Drive.Session.Audio
{
    internal sealed class RaceAudioFactory
    {
        private readonly AudioManager _audio;
        private readonly string _legacyRoot;

        public RaceAudioFactory(AudioManager audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _legacyRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
        }

        public PlayerVehicleAudio CreatePlayer(VehicleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var audio = new PlayerVehicleAudio(
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Engine), AudioEngineOptions.VehiclesBusName, looped: true, spatialize: true, allowHrtf: true),
                CreateVehicleSoundOptional(definition.GetSoundPath(VehicleAction.Throttle), AudioEngineOptions.VehiclesBusName, looped: true, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Horn), AudioEngineOptions.VehiclesBusName, looped: true, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Start), AudioEngineOptions.VehiclesBusName, looped: false, spatialize: true, allowHrtf: true),
                CreateVehicleSoundOptional(definition.GetSoundPath(VehicleAction.Stop), AudioEngineOptions.VehiclesBusName, looped: false, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Brake), AudioEngineOptions.VehiclesBusName, looped: true, spatialize: true, allowHrtf: false),
                CreateRequiredVariants(definition.GetSoundPaths(VehicleAction.Crash), definition.GetSoundPath(VehicleAction.Crash), AudioEngineOptions.VehiclesBusName, spatialize: true, allowHrtf: true),
                CreateLegacyVehicleEvent("crashshort.wav"),
                CreateLegacyTrackLoop("asphalt.wav"),
                CreateLegacyTrackLoop("gravel.wav"),
                CreateLegacyTrackLoop("water.wav"),
                CreateLegacyTrackLoop("sand.wav"),
                CreateLegacyTrackLoop("snow.wav"),
                definition.HasWipers == 1
                    ? CreateLegacyVehicleLoop("wipers.wav", allowHrtf: false)
                    : null,
                CreateLegacyVehicleEvent("bump.wav", allowHrtf: false),
                CreateLegacyVehicleEvent("badswitch.wav", allowHrtf: false),
                CreateVehicleSoundRequired(
                    Path.Combine(AssetPaths.SoundsRoot, "Vehicles", "fuel_warning.wav"),
                    AudioEngineOptions.VehiclesBusName,
                    looped: false,
                    spatialize: true,
                    allowHrtf: false),
                CreateOptionalVariants(definition.GetSoundPaths(VehicleAction.Backfire), definition.GetSoundPath(VehicleAction.Backfire), AudioEngineOptions.VehiclesBusName, spatialize: true, allowHrtf: true));
            return audio;
        }

        public RemoteVehicleAudio CreateRemote(VehicleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var audio = new RemoteVehicleAudio(
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Engine), AudioEngineOptions.WorldBusName, looped: true, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Horn), AudioEngineOptions.WorldBusName, looped: true, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Start), AudioEngineOptions.WorldBusName, looped: false, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Crash), AudioEngineOptions.WorldBusName, looped: false, spatialize: true, allowHrtf: true),
                CreateVehicleSoundRequired(definition.GetSoundPath(VehicleAction.Brake), AudioEngineOptions.WorldBusName, looped: true, spatialize: true, allowHrtf: false),
                CreateLegacyWorldEvent("crashshort.wav"),
                CreateLegacyWorldEvent("bump.wav", allowHrtf: false),
                CreateVehicleSoundOptional(definition.GetSoundPath(VehicleAction.Backfire), AudioEngineOptions.WorldBusName, looped: false, spatialize: true, allowHrtf: true));
            return audio;
        }

        private Source[] CreateRequiredVariants(
            IReadOnlyList<string>? paths,
            string? fallbackSinglePath,
            string busName,
            bool spatialize,
            bool allowHrtf)
        {
            if (paths != null && paths.Count > 0)
            {
                var result = new Source[paths.Count];
                for (var i = 0; i < paths.Count; i++)
                    result[i] = CreateVehicleSoundRequired(paths[i], busName, looped: false, spatialize, allowHrtf);
                return result;
            }

            return new[]
            {
                CreateVehicleSoundRequired(fallbackSinglePath, busName, looped: false, spatialize, allowHrtf)
            };
        }

        private Source[] CreateOptionalVariants(
            IReadOnlyList<string>? paths,
            string? fallbackSinglePath,
            string busName,
            bool spatialize,
            bool allowHrtf)
        {
            if (paths != null && paths.Count > 0)
            {
                var items = new List<Source>();
                for (var i = 0; i < paths.Count; i++)
                {
                    var sound = CreateVehicleSoundOptional(paths[i], busName, looped: false, spatialize, allowHrtf);
                    if (sound != null)
                        items.Add(sound);
                }
                return items.ToArray();
            }

            var single = CreateVehicleSoundOptional(fallbackSinglePath, busName, looped: false, spatialize, allowHrtf);
            return single == null
                ? Array.Empty<Source>()
                : new[] { single };
        }

        private Source CreateLegacyTrackLoop(string fileName)
        {
            var path = Path.Combine(_legacyRoot, fileName);
            return CreateVehicleSoundRequired(path, AudioEngineOptions.TrackBusName, looped: true, spatialize: false, allowHrtf: false);
        }

        private Source CreateLegacyVehicleLoop(string fileName, bool allowHrtf)
        {
            var path = Path.Combine(_legacyRoot, fileName);
            return CreateVehicleSoundRequired(path, AudioEngineOptions.VehiclesBusName, looped: true, spatialize: true, allowHrtf);
        }

        private Source CreateLegacyVehicleEvent(string fileName, bool allowHrtf = true)
        {
            var path = Path.Combine(_legacyRoot, fileName);
            return CreateVehicleSoundRequired(path, AudioEngineOptions.VehiclesBusName, looped: false, spatialize: true, allowHrtf);
        }

        private Source CreateLegacyWorldEvent(string fileName, bool allowHrtf = true)
        {
            var path = Path.Combine(_legacyRoot, fileName);
            return CreateVehicleSoundRequired(path, AudioEngineOptions.WorldBusName, looped: false, spatialize: true, allowHrtf);
        }

        private Source CreateVehicleSoundRequired(
            string? path,
            string busName,
            bool looped,
            bool spatialize,
            bool allowHrtf)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Sound path not provided.");

            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                throw new FileNotFoundException("Sound file not found.", resolved);

            return CreateVehicleSound(resolved, busName, looped, spatialize, allowHrtf);
        }

        private Source? CreateVehicleSoundOptional(
            string? path,
            string busName,
            bool looped,
            bool spatialize,
            bool allowHrtf)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                return null;

            return CreateVehicleSound(resolved, busName, looped, spatialize, allowHrtf);
        }

        private Source CreateVehicleSound(
            string path,
            string busName,
            bool looped,
            bool spatialize,
            bool allowHrtf)
        {
            var asset = _audio.LoadAsset(path, streamFromDisk: false);
            if (!spatialize)
            {
                return looped
                    ? _audio.CreateLoopingSource(asset, busName, useHrtf: false)
                    : _audio.CreateSource(asset, busName, useHrtf: false);
            }

            return looped
                ? _audio.CreateLoopingSpatialSource(asset, busName, allowHrtf)
                : _audio.CreateSpatialSource(asset, busName, allowHrtf);
        }
    }
}
