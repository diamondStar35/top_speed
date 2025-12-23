using System;
using DavyKager;

namespace TopSpeed.Speech
{
    internal sealed class SpeechService : IDisposable
    {
        private bool _available;

        public bool IsAvailable => _available;

        public SpeechService()
        {
            try
            {
                Tolk.TrySAPI(true);
                Tolk.Load();
                _available = Tolk.IsLoaded();
            }
            catch (DllNotFoundException)
            {
                _available = false;
            }
            catch (BadImageFormatException)
            {
                _available = false;
            }
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (!_available || string.IsNullOrWhiteSpace(text))
                return;

            Tolk.Output(text, interrupt);
        }

        public void Dispose()
        {
            if (_available)
            {
                Tolk.Unload();
                _available = false;
            }
        }
    }
}
