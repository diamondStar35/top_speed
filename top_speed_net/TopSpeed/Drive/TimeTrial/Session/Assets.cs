using System;
using System.IO;
using TopSpeed.Common;
using TopSpeed.Core;
using TS.Audio;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
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
            Finish = 13
        }

        private void ConfigureDefaultRandomSounds()
        {
            ConfigureRandomSounds(RandomSoundSlot.EasyLeft, "race\\copilot\\easyleft");
            ConfigureRandomSounds(RandomSoundSlot.Left, "race\\copilot\\left");
            ConfigureRandomSounds(RandomSoundSlot.HardLeft, "race\\copilot\\hardleft");
            ConfigureRandomSounds(RandomSoundSlot.HairpinLeft, "race\\copilot\\hairpinleft");
            ConfigureRandomSounds(RandomSoundSlot.EasyRight, "race\\copilot\\easyright");
            ConfigureRandomSounds(RandomSoundSlot.Right, "race\\copilot\\right");
            ConfigureRandomSounds(RandomSoundSlot.HardRight, "race\\copilot\\hardright");
            ConfigureRandomSounds(RandomSoundSlot.HairpinRight, "race\\copilot\\hairpinright");
            ConfigureRandomSounds(RandomSoundSlot.Asphalt, "race\\copilot\\asphalt");
            ConfigureRandomSounds(RandomSoundSlot.Gravel, "race\\copilot\\gravel");
            ConfigureRandomSounds(RandomSoundSlot.Water, "race\\copilot\\water");
            ConfigureRandomSounds(RandomSoundSlot.Sand, "race\\copilot\\sand");
            ConfigureRandomSounds(RandomSoundSlot.Snow, "race\\copilot\\snow");
            ConfigureRandomSounds(RandomSoundSlot.Finish, "race\\info\\finish");
        }

        private void ConfigureRandomSounds(RandomSoundSlot slot, string baseName)
        {
            var slotIndex = (int)slot;
            _randomSoundBaseNames[slotIndex] = baseName;
            _totalRandomSounds[slotIndex] = 1;

            for (var i = 1; i < RandomSoundMax; i++)
            {
                if (ResolveLanguageSoundPath($"{baseName}{i + 1}", allowFallback: false) == null)
                {
                    _totalRandomSounds[slotIndex] = i;
                    break;
                }
            }
        }

        private Source? GetRandomSoundBySlot(int slot)
        {
            if (slot < 0 || slot >= _randomSounds.Length || slot >= _totalRandomSounds.Length)
                return null;

            var count = _totalRandomSounds[slot];
            if (count <= 0)
                return null;

            return GetRandomSound(slot, TopSpeed.Common.Algorithm.RandomInt(count));
        }

        private Source? GetRandomSound(int slot, int variantIndex)
        {
            if (slot < 0 || slot >= _randomSounds.Length)
                return null;
            if (variantIndex < 0 || variantIndex >= _randomSounds[slot].Length)
                return null;

            var cached = _randomSounds[slot][variantIndex];
            if (cached != null)
                return cached;

            var baseName = _randomSoundBaseNames[slot];
            if (string.IsNullOrWhiteSpace(baseName))
                return null;

            Source? sound = variantIndex == 0
                ? LoadLanguageSound($"{baseName}1")
                : TryLoadLanguageSound($"{baseName}{variantIndex + 1}", allowFallback: false);
            _randomSounds[slot][variantIndex] = sound;
            return sound;
        }

        private void PreloadRaceSpeechSources()
        {
            for (var slot = 0; slot < _randomSounds.Length && slot < _totalRandomSounds.Length; slot++)
            {
                var count = Math.Min(_totalRandomSounds[slot], _randomSounds[slot].Length);
                for (var variant = 0; variant < count; variant++)
                    GetRandomSound(slot, variant);
            }
        }

        private Source LoadLanguageSound(string key, bool streamFromDisk = false)
        {
            var sound = TryLoadLanguageSound(key, allowFallback: true, streamFromDisk: streamFromDisk);
            if (sound != null)
                return sound;

            var errorPath = AssetPaths.ResolveLegacySoundPath("error.wav");
            if (errorPath != null)
                return LoadBusSource(errorPath, AudioEngineOptions.CopilotBusName, streamFromDisk: false);

            throw new FileNotFoundException($"Missing language sound {key}.");
        }

        private Source? TryLoadLanguageSound(string key, bool allowFallback, bool streamFromDisk = false)
        {
            var path = ResolveLanguageSoundPath(key, allowFallback);
            if (path != null)
                return LoadBusSource(path, AudioEngineOptions.CopilotBusName, streamFromDisk);

            return null;
        }

        private string? ResolveLanguageSoundPath(string key, bool allowFallback)
        {
            return allowFallback
                ? AssetPaths.ResolveLanguageSoundPathWithFallback(_settings.Language, key)
                : AssetPaths.ResolveLanguageSoundPath(_settings.Language, key);
        }

        private Source LoadLanguageMusicSound(string key, bool streamFromDisk)
        {
            var path = AssetPaths.ResolveLanguageSoundPathWithFallback(_settings.Language, key);
            if (path == null)
                throw new FileNotFoundException($"Missing language sound {key}.");

            return LoadBusSource(path, AudioEngineOptions.MusicBusName, streamFromDisk);
        }

        private Source LoadLegacySound(string fileName)
        {
            var path = AssetPaths.ResolveLegacySoundPath(fileName);
            if (path == null)
                throw new FileNotFoundException($"Missing legacy sound {fileName}.");

            return LoadBusSource(path, AudioEngineOptions.CopilotBusName, streamFromDisk: false);
        }

        private Source? TryLoadPitSound(string fileName)
        {
            var path = AssetPaths.ResolvePitSoundPath(fileName);
            if (path != null)
                return LoadBusSource(path, AudioEngineOptions.CopilotBusName, streamFromDisk: false);
            return null;
        }

        private Source LoadBusSource(string path, string busName, bool streamFromDisk)
        {
            var asset = _audio.LoadAsset(path, streamFromDisk);
            var source = streamFromDisk
                ? _audio.CreateSource(asset, busName)
                : _audio.CreateLoopingSource(asset, busName);
            return source;
        }
    }
}
