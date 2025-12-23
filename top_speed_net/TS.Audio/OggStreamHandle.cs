using System;
using System.Runtime.InteropServices;
using System.Threading;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed class OggStreamHandle : IDisposable
    {
        private readonly AudioOutput _output;
        private readonly string[] _filePaths;
        private MaSound? _sound;
        private bool _loopSingle;
        private int _currentIndex;
        private int _advanceRequested;
        private bool _disposed;
        private GCHandle _gcHandle;
        private ma_sound_end_proc _endCallback;
        private float _volume = 1.0f;
        private float _pitch = 1.0f;
        private float _pan = 0.0f;

        public OggStreamHandle(AudioOutput output, params string[] filePaths)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("At least one file path is required.", nameof(filePaths));

            _output = output;
            _filePaths = filePaths;
            _gcHandle = GCHandle.Alloc(this);
            _endCallback = OnSoundEnd;
        }

        public void Play(bool loop)
        {
            _loopSingle = loop;
            EnsureSoundInitialized();
            if (_sound == null)
                return;
            _sound.SetLooping(ShouldLoopIndex(_currentIndex));
            _sound.Start();
        }

        public void Stop()
        {
            _sound?.Stop();
        }

        public bool IsPlaying => _sound != null && _sound.IsPlaying();

        public void SetVolume(float volume)
        {
            _volume = volume;
            _sound?.SetVolume(volume);
        }

        public void SetPitch(float pitch)
        {
            _pitch = pitch;
            _sound?.SetPitch(pitch);
        }

        public void SetPan(float pan)
        {
            _pan = pan;
            _sound?.SetPan(pan);
        }

        public void Update()
        {
            if (_disposed)
                return;

            if (Interlocked.Exchange(ref _advanceRequested, 0) == 0)
                return;

            AdvanceTrack();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _sound?.Dispose();
            _sound = null;
            if (_gcHandle.IsAllocated)
                _gcHandle.Free();
            _output.RemoveStream(this);
        }

        private void EnsureSoundInitialized()
        {
            if (_sound != null)
                return;

            InitSoundForIndex(_currentIndex);
        }

        private void InitSoundForIndex(int index)
        {
            _sound?.Dispose();
            _sound = new MaSound();

            ma_sound_flags flags = ma_sound_flags.stream;
            var result = _sound.InitializeFromFile(_output.Engine, _filePaths[index], flags, null!);
            if (result != ma_result.success)
                throw new InvalidOperationException("Failed to init stream: " + result);

            _sound.SetSpatializationEnabled(false);
            _sound.SetVolume(_volume);
            _sound.SetPitch(_pitch);
            _sound.SetPan(_pan);
            _sound.SetLooping(ShouldLoopIndex(index));
            _sound.SetEndCallback(_endCallback, GCHandle.ToIntPtr(_gcHandle));
        }

        private void AdvanceTrack()
        {
            if (_disposed)
                return;

            if (_filePaths.Length <= 1)
            {
                if (_loopSingle && _sound != null)
                {
                    _sound.SeekToPCMFrame(0);
                    _sound.Start();
                }
                return;
            }

            if (_currentIndex < _filePaths.Length - 1)
            {
                _currentIndex++;
                InitSoundForIndex(_currentIndex);
                _sound?.Start();
                return;
            }

            if (ShouldLoopIndex(_currentIndex) && _sound != null)
            {
                _sound.SeekToPCMFrame(0);
                _sound.Start();
            }
        }

        private bool ShouldLoopIndex(int index)
        {
            if (_filePaths.Length <= 1)
                return _loopSingle;

            return index == _filePaths.Length - 1;
        }

        private static void OnSoundEnd(IntPtr pUserData, ma_sound_ptr pSound)
        {
            var handle = GCHandle.FromIntPtr(pUserData);
            var stream = handle.Target as OggStreamHandle;
            if (stream == null || stream._disposed)
                return;

            Interlocked.Exchange(ref stream._advanceRequested, 1);
        }
    }
}
