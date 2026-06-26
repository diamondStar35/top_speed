using System;
using TopSpeed.Drive.Session;
using TopSpeed.Localization;
using AudioSource = TS.Audio.Source;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void Speak(AudioSource sound, bool unKey = false)
        {
            SpeakCore(sound, queueToRaceInfo: false, unKey);
        }

        private void SpeakRaceInfo(AudioSource sound, bool unKey = false)
        {
            SpeakCore(sound, queueToRaceInfo: true, unKey);
        }

        private void SpeakCore(AudioSource sound, bool queueToRaceInfo, bool unKey)
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

        private void QueueSound(AudioSource? sound)
        {
            if (sound != null)
                _soundQueue.Enqueue(sound);
        }

        private void QueueRaceInfoSound(AudioSource? sound)
        {
            if (sound != null)
                _raceInfoQueue.Enqueue(sound);
        }

        private void QueueTrackInfoSound(AudioSource? sound)
        {
            if (sound != null)
                _trackInfoQueue.Enqueue(sound);
        }

        private bool TrySendRace(bool sent)
        {
            if (sent)
                return true;
            if (_sendFailureAnnounced)
                return false;

            _sendFailureAnnounced = true;
            SpeakText(LocalizationService.Mark("Network send failed. Please check your connection."));
            return false;
        }
    }
}
