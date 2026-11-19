using System;
using SoundFlow.Abstracts;
using SoundFlow.Codecs.FFMpeg;
using SoundFlow.Enums;
using SoundFlow.Providers;
using TopSpeed.Protocol;
using SfAudioFormat = SoundFlow.Structs.AudioFormat;

namespace TopSpeed.Network.Live
{
    internal sealed class Source : IDisposable
    {
        private static readonly Lazy<AudioEngine> DecoderEngine = new Lazy<AudioEngine>(CreateEngine);
        private readonly SoundFlow.Interfaces.ISoundDataProvider _provider;
        private readonly int _channels;
        private readonly int _framesPerPacket;
        private readonly float[] _floatBuffer;
        private readonly short[] _sampleBuffer;

        private Source(SoundFlow.Interfaces.ISoundDataProvider provider, int channels, int framesPerPacket)
        {
            _provider = provider;
            _channels = channels;
            _framesPerPacket = framesPerPacket;
            _floatBuffer = new float[_channels * _framesPerPacket];
            _sampleBuffer = new short[_channels * _framesPerPacket];
        }

        public static bool TryOpen(string filePath, out Source? source)
        {
            source = null;
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!System.IO.File.Exists(filePath))
                return false;

            var format = new SfAudioFormat
            {
                Format = SampleFormat.F32,
                Channels = ProtocolConstants.LiveChannelsMax,
                Layout = SfAudioFormat.GetLayoutFromChannels(ProtocolConstants.LiveChannelsMax),
                SampleRate = ProtocolConstants.LiveSampleRate
            };

            SoundFlow.Interfaces.ISoundDataProvider provider;
            try
            {
                provider = new StreamDataProvider(
                    DecoderEngine.Value,
                    format,
                    new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read));
            }
            catch
            {
                return false;
            }

            if (provider.Length == 0)
            {
                provider.Dispose();
                return false;
            }

            var frameCount = ProtocolConstants.LiveSampleRate * ProtocolConstants.LiveFrameMs / 1000;
            source = new Source(provider, ProtocolConstants.LiveChannelsMax, frameCount);
            return true;
        }

        public bool TryRead(out short[] samples)
        {
            samples = _sampleBuffer;
            var targetFrames = (ulong)_framesPerPacket;
            ulong writtenFrames = 0;
            var wraps = 0;
            var stalledReads = 0;

            while (writtenFrames < targetFrames)
            {
                var sampleOffset = (int)(writtenFrames * (ulong)_channels);
                var samplesToRead = (int)((targetFrames - writtenFrames) * (ulong)_channels);
                var readSamples = _provider.ReadBytes(_floatBuffer.AsSpan(sampleOffset, samplesToRead));

                if (readSamples > 0)
                {
                    writtenFrames += (ulong)(readSamples / _channels);
                    stalledReads = 0;
                    continue;
                }

                if (!_provider.CanSeek)
                    return false;

                _provider.Seek(0);

                wraps++;
                if (wraps > _framesPerPacket)
                    return false;

                stalledReads++;
                if (stalledReads > 2)
                    return false;
            }

            ConvertToPcm16(_floatBuffer, _sampleBuffer);
            return true;
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        private static AudioEngine CreateEngine()
        {
            var engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
            engine.RegisterCodecFactory(new FFmpegCodecFactory());
            return engine;
        }

        private static void ConvertToPcm16(float[] source, short[] destination)
        {
            var count = Math.Min(source.Length, destination.Length);
            for (var i = 0; i < count; i++)
            {
                var sample = source[i];
                if (sample < -1f)
                    sample = -1f;
                else if (sample > 1f)
                    sample = 1f;
                destination[i] = (short)Math.Round(sample * short.MaxValue);
            }
        }
    }
}

