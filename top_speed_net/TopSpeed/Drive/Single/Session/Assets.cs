using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Drive.Session;
using TS.Audio;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
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
            ConfigureRandomSounds(RandomSoundSlot.Front, "race\\info\\front");
            ConfigureRandomSounds(RandomSoundSlot.Tail, "race\\info\\tail");
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

        private Source? GetNumberSound(int index)
        {
            if (index < 0 || index >= _soundNumbers.Length)
                return null;

            return _soundNumbers[index] ??= LoadLanguageSound($"numbers\\{index}");
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

        private Source? GetPlayerNumberSoundByIndex(int index)
        {
            if (index < 0 || index >= _soundPlayerNr.Length)
                return null;

            return _soundPlayerNr[index] ??= LoadLanguageSound($"race\\info\\player{index + 1}");
        }

        private Source? GetPlayerNumberInfoSoundByIndex(int index)
        {
            if (index < 0 || index >= _soundPlayerNrInfo.Length)
                return null;

            return _soundPlayerNrInfo[index] ??= LoadLanguageSound($"race\\info\\player{index + 1}");
        }

        private Source? GetPositionSoundByIndex(int index)
        {
            var slots = Math.Max(0, Math.Min(_nComputerPlayers + 1, _soundPosition.Length));
            if (index < 0 || index >= slots)
                return null;

            var positionIndex = index == slots - 1 ? MaxPlayers : Math.Min(MaxPlayers, index + 1);
            return _soundPosition[index] ??= LoadLanguageSound($"race\\info\\youarepos{positionIndex}");
        }

        private Source? GetNumberedFinishedSound(int index)
        {
            if (index < 0 || index >= _soundFinished.Length)
                return null;

            return _soundFinished[index] ??= LoadLanguageSound(RaceInfoSounds.NumberedFinishedKey(index));
        }

        private Source? GetFinishedLastSound()
        {
            return _soundFinishedLast ??= LoadLanguageSound(RaceInfoSounds.FinishedLastKey);
        }

        private Source? GetFinishedSoundByIndex(int index)
        {
            var racers = Math.Max(0, Math.Min(_nComputerPlayers + 1, _soundFinished.Length));
            if (index < 0 || index >= racers)
                return null;

            return RaceInfoSounds.IsLastPlace(index, racers)
                ? GetFinishedLastSound()
                : GetNumberedFinishedSound(index);
        }

        private void PreloadRaceSpeechSources()
        {
            GetNumberSound(_playerNumber + 1);

            var players = Math.Max(0, Math.Min(_nComputerPlayers + 1, MaxPlayers));
            for (var i = 0; i < players; i++)
            {
                GetPlayerNumberSoundByIndex(i);
                GetPlayerNumberInfoSoundByIndex(i);
                GetPositionSoundByIndex(i);
                GetFinishedSoundByIndex(i);
            }

            PreloadRandomSounds();
        }

        private void PreloadRandomSounds()
        {
            for (var slot = 0; slot < _randomSounds.Length && slot < _totalRandomSounds.Length; slot++)
            {
                var count = Math.Min(_totalRandomSounds[slot], _randomSounds[slot].Length);
                for (var variant = 0; variant < count; variant++)
                    GetRandomSound(slot, variant);
            }
        }

        private void LoadRaceUiSounds()
        {
            _soundYouAre = LoadLanguageSound("race\\youare");
            _soundPlayer = LoadLanguageSound("race\\player");
            _soundTheme = LoadLanguageMusicSound("music\\theme4", streamFromDisk: false);
            _soundPause = LoadLanguageSound("race\\pause");
            _soundResume = LoadLanguageSound("race\\unpause");
            _soundTurnEndDing = LoadLegacySound("ding.ogg");
            _soundLetsPit = TryLoadLanguageSound("race\\letspit", allowFallback: false);
            _soundRightTires = TryLoadPitSound("tirechangeright.ogg");
            _soundLeftTires = TryLoadPitSound("tirechangeleft.ogg");
            _soundFuelingUp = TryLoadPitSound("refueling.ogg");
            _soundExitPitRoad = TryLoadLanguageSound("race\\exitpitroad", allowFallback: false);
            _soundTheme.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
        }

        private void QueueRaceIntro()
        {
            QueueIntroSound(_soundYouAre);
            QueueIntroSound(_soundPlayer);
            QueueIntroSound(GetNumberSound(_playerNumber + 1));
        }

        private void QueueIntroSound(Source? sound)
        {
            if (sound == null)
                return;

            _session.QueueEvent(new Event(Events.PlaySound, sound), 0f);
        }

        private void AnnounceFinishOrder(int playerNumber)
        {
            var playerSound = GetPlayerNumberInfoSoundByIndex(playerNumber);
            var finishSound = GetFinishedSoundByIndex(_positionFinish);
            if (playerSound == null || finishSound == null)
                return;

            SpeakRaceInfoIfLoaded(playerSound, true);
            SpeakRaceInfoIfLoaded(finishSound, true);
            _positionFinish++;
        }

        private void SpeakIfLoaded(Source? sound, bool unKey = false)
        {
            if (sound == null)
                return;
            Speak(sound, unKey);
        }

        private void SpeakRaceInfoIfLoaded(Source? sound, bool unKey = false)
        {
            if (sound == null)
                return;
            SpeakRaceInfo(sound, unKey);
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
