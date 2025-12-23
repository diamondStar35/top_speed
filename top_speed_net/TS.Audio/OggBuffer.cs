using System;

namespace TS.Audio
{
    public class OggBuffer : IDisposable
    {
        public enum Status
        {
            stopped,
            playing
        }

        private readonly OggStreamHandle _stream;

        public Status status => _stream != null && _stream.IsPlaying ? Status.playing : Status.stopped;

        internal OggBuffer(OggStreamHandle stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public void stopOgg()
        {
            _stream.Stop();
        }

        public bool isPlaying()
        {
            return status == Status.playing;
        }

        public void play(bool loop)
        {
            _stream.Play(loop);
        }

        public void play()
        {
            play(false);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
