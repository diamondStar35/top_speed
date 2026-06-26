using System;
using System.Threading;
using TopSpeed.Audio;
using TopSpeed.Drive.Session;
using TS.Audio;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
    {
        private void Speak(Source sound, bool unKey = false)
        {
            SpeakCore(sound, queueToRaceInfo: false, unKey);
        }

        private void SpeakRaceInfo(Source sound, bool unKey = false)
        {
            SpeakCore(sound, queueToRaceInfo: true, unKey);
        }

        private void SpeakCore(Source sound, bool queueToRaceInfo, bool unKey)
        {
            if (sound == null)
                return;

            var length = Math.Max(0.05f, sound.LengthSeconds);
            _speakTime = Math.Max(_speakTime, _session.Context.ProgressSeconds) + length;
            if (queueToRaceInfo)
                QueueRaceInfoSound(sound);
            else
                QueueSound(sound);

            if (unKey)
            {
                _unkeyQueue++;
                _session.QueueEvent(new Event(Events.PlayUnkey), length);
            }
        }

        private void SpeakText(string text)
        {
            _speech.Speak(text);
        }

        private void QueueSound(Source? sound)
        {
            if (sound == null)
                return;

            _soundQueue.Enqueue(sound);
        }

        private void QueueRaceInfoSound(Source? sound)
        {
            if (sound == null)
                return;

            _raceInfoQueue.Enqueue(sound);
        }

        private void QueueTrackInfoSound(Source? sound)
        {
            if (sound == null)
                return;

            _trackInfoQueue.Enqueue(sound);
        }

        private void RefreshCategoryVolumes()
        {
            var ambientPercent = _settings.AudioVolumes?.AmbientsAndSourcesPercent ?? 100;
            _track.SetAmbientVolumePercent(ambientPercent);
        }

        private void FadeInTheme()
        {
            if (_soundTheme == null)
                return;

            var target = (int)Math.Round(_settings.MusicVolume * 100f);
            var volume = 0;
            _soundTheme.SetVolumePercent(volume);
            for (var i = 0; i < 10; i++)
            {
                volume = Math.Min(target, volume + Math.Max(1, target / 10));
                _soundTheme.SetVolumePercent(volume);
                Thread.Sleep(25);
            }
        }

        private void FadeOutTheme()
        {
            if (_soundTheme == null)
                return;

            var volume = (int)Math.Round(_settings.MusicVolume * 100f);
            for (var i = 0; i < 10; i++)
            {
                volume = Math.Max(0, volume - Math.Max(1, volume / 10));
                _soundTheme.SetVolumePercent(volume);
                Thread.Sleep(25);
            }
        }
    }
}
