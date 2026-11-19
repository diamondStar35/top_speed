using System;
using System.Collections.Generic;
using Concentus.Structs;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TS.Audio;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private sealed class RemoteVoiceStream : IDisposable
        {
            private readonly object _lock = new object();
            private readonly Queue<float[]> _frames;
            private readonly int _maxBufferedFrames;
            private readonly OpusDecoder _decoder;
            private readonly short[] _decodeBuffer;
            private readonly ushort _sampleRate;
            private readonly byte _channels;
            private readonly byte _frameMs;
            private Source _source;
            private float[]? _activeFrame;
            private int _activeOffset;

            public RemoteVoiceStream(AudioManager audio, DriveSettings settings, PacketPlayerVoiceStart start, int maxBufferedFrames)
            {
                StreamId = start.StreamId;
                FrequencyTenths = start.FrequencyTenths;
                PushToTalk = start.PushToTalk;
                LastReceivedUtcTicks = DateTime.UtcNow.Ticks;
                _sampleRate = start.SampleRate;
                _channels = start.Channels;
                _frameMs = start.FrameMs;
                _maxBufferedFrames = Math.Max(2, maxBufferedFrames);
                _frames = new Queue<float[]>(_maxBufferedFrames);
                _decoder = OpusDecoder.Create(start.SampleRate, start.Channels);
                var samplesPerChannel = (start.SampleRate * start.FrameMs) / 1000;
                _decodeBuffer = new short[Math.Max(1, samplesPerChannel * start.Channels)];
                _source = audio.CreateProceduralSource(
                    OnRender,
                    _channels,
                    start.SampleRate,
                    busName: AudioEngineOptions.RadioBusName,
                    spatialize: false,
                    useHrtf: false);
                _source.SetVolumePercent(settings, AudioVolumeCategory.Communicator, 100);
            }

            public uint StreamId { get; }
            public ushort FrequencyTenths { get; }
            public bool PushToTalk { get; }
            public long LastReceivedUtcTicks { get; set; }
            public bool IsAudible { get; private set; }

            public void SetAudible(bool audible)
            {
                lock (_lock)
                {
                    if (IsAudible == audible)
                        return;

                    IsAudible = audible;
                    if (!audible)
                    {
                        _source.Stop();
                        _frames.Clear();
                        _activeFrame = null;
                        _activeOffset = 0;
                    }
                    else if (_frames.Count > 0 && !_source.IsPlaying)
                    {
                        _source.Play(loop: true);
                    }
                }
            }

            public void RefreshVolume(DriveSettings settings)
            {
                lock (_lock)
                    _source.SetVolumePercent(settings, AudioVolumeCategory.Communicator, 100);
            }

            public void PushFrame(PacketPlayerVoiceFrame frame)
            {
                if (frame.Data == null || frame.Data.Length == 0)
                    return;

                var samplesPerChannel = (_sampleRate * _frameMs) / 1000;
                int decoded;
                try
                {
                    decoded = _decoder.Decode(frame.Data, 0, frame.Data.Length, _decodeBuffer, 0, samplesPerChannel, false);
                }
                catch
                {
                    return;
                }

                if (decoded <= 0)
                    return;

                var sampleCount = decoded * _channels;
                var outFrame = new float[sampleCount];
                for (var i = 0; i < sampleCount; i++)
                    outFrame[i] = _decodeBuffer[i] / 32768f;

                lock (_lock)
                {
                    if (!IsAudible)
                        return;

                    if (_frames.Count >= _maxBufferedFrames)
                        _frames.Dequeue();

                    _frames.Enqueue(outFrame);
                    if (!_source.IsPlaying)
                        _source.Play(loop: true);
                }
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    _source.Stop();
                    _source.Dispose();
                    _frames.Clear();
                    _activeFrame = null;
                    _activeOffset = 0;
                    IsAudible = false;
                }
            }

            private void OnRender(float[] buffer, int frames, int channels, ref ulong _frameIndex)
            {
                if (buffer == null || frames <= 0 || channels <= 0)
                    return;

                if (!System.Threading.Monitor.TryEnter(_lock))
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    return;
                }

                try
                {
                    var sampleCount = frames * channels;
                    if (sampleCount <= 0 || sampleCount > buffer.Length)
                        return;

                    var cursor = 0;
                    for (var frame = 0; frame < frames; frame++)
                    {
                        if (_activeFrame == null || _activeOffset + channels > _activeFrame.Length)
                        {
                            if (_frames.Count > 0)
                            {
                                _activeFrame = _frames.Dequeue();
                                _activeOffset = 0;
                            }
                            else
                            {
                                _activeFrame = null;
                            }
                        }

                        if (_activeFrame == null)
                        {
                            for (var ch = 0; ch < channels; ch++)
                                buffer[cursor++] = 0f;
                            continue;
                        }

                        for (var ch = 0; ch < channels; ch++)
                            buffer[cursor++] = _activeFrame[_activeOffset++];
                    }

                    if (cursor < sampleCount)
                    {
                        for (var i = cursor; i < sampleCount; i++)
                            buffer[i] = 0f;
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(_lock);
                }
            }
        }
    }
}
