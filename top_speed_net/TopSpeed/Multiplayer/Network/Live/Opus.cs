using System;
using Concentus.Enums;
using Concentus.Structs;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Live
{
    internal sealed class Opus
    {
        private readonly OpusEncoder _encoder;
        private readonly byte[] _payloadBuffer;
        private readonly int _samplesPerFrame;
        private ushort _nextSequence;

        public Opus(byte channels)
        {
            if (channels < ProtocolConstants.LiveChannelsMin || channels > ProtocolConstants.LiveChannelsMax)
                throw new ArgumentOutOfRangeException(nameof(channels));

            Profile = new LiveAudioProfile(
                LiveCodec.Opus,
                (ushort)ProtocolConstants.LiveSampleRate,
                channels,
                (byte)ProtocolConstants.LiveFrameMs);

            _samplesPerFrame = ProtocolConstants.LiveSampleRate * ProtocolConstants.LiveFrameMs / 1000;
            _payloadBuffer = new byte[ProtocolConstants.MaxLiveFrameBytes];
            _encoder = OpusEncoder.Create(Profile.SampleRate, Profile.Channels, OpusApplication.OPUS_APPLICATION_AUDIO);
            _encoder.Bitrate = 64000;
            _encoder.SignalType = OpusSignal.OPUS_SIGNAL_MUSIC;
            _nextSequence = 0;
        }

        public LiveAudioProfile Profile { get; }

        public void Reset()
        {
            _nextSequence = 0;
        }

        public bool TryEncode(in LivePcmFrame input, out LiveOpusFrame output)
        {
            output = new LiveOpusFrame(0, 0, Array.Empty<byte>());
            if (input.Samples == null)
                return false;
            if (input.SampleRate != Profile.SampleRate || input.Channels != Profile.Channels || input.FrameMs != Profile.FrameMs)
                return false;

            var requiredSamples = _samplesPerFrame * Profile.Channels;
            if (input.Samples.Length < requiredSamples)
                return false;

            int encoded;
            try
            {
                encoded = _encoder.Encode(
                    input.Samples,
                    0,
                    _samplesPerFrame,
                    _payloadBuffer,
                    0,
                    _payloadBuffer.Length);
            }
            catch
            {
                return false;
            }

            if (encoded <= 0 || encoded > _payloadBuffer.Length)
                return false;

            var payload = new byte[encoded];
            Buffer.BlockCopy(_payloadBuffer, 0, payload, 0, encoded);
            output = new LiveOpusFrame(_nextSequence, input.Timestamp, payload);
            _nextSequence++;
            return true;
        }
    }
}

