using System;
using Concentus.Enums;
using Concentus.Structs;
using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private sealed class VoiceEncoder
        {
            private readonly OpusEncoder _encoder;
            private readonly byte[] _payloadBuffer;
            private ushort _nextSequence;

            public VoiceEncoder()
            {
                Profile = new LiveAudioProfile(
                    LiveCodec.Opus,
                    (ushort)ProtocolConstants.VoiceSampleRate,
                    (byte)ProtocolConstants.VoiceChannelsMax,
                    (byte)ProtocolConstants.VoiceFrameMs);

                _payloadBuffer = new byte[ProtocolConstants.MaxVoiceFrameBytes];
                _encoder = OpusEncoder.Create(Profile.SampleRate, Profile.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
                _encoder.Bitrate = 32000;
                _encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
            }

            public LiveAudioProfile Profile { get; }

            public void Reset()
            {
                _nextSequence = 0;
            }

            public bool TryEncode(short[] samples, uint timestamp, out LiveOpusFrame frame)
            {
                frame = new LiveOpusFrame(0, 0, Array.Empty<byte>());
                if (samples == null)
                    return false;

                var samplesPerFrame = ProtocolConstants.VoiceSampleRate * ProtocolConstants.VoiceFrameMs / 1000;
                if (samples.Length < samplesPerFrame * Profile.Channels)
                    return false;

                int encoded;
                try
                {
                    encoded = _encoder.Encode(samples, 0, samplesPerFrame, _payloadBuffer, 0, _payloadBuffer.Length);
                }
                catch
                {
                    return false;
                }

                if (encoded <= 0 || encoded > _payloadBuffer.Length)
                    return false;

                var payload = new byte[encoded];
                Buffer.BlockCopy(_payloadBuffer, 0, payload, 0, encoded);
                frame = new LiveOpusFrame(_nextSequence, timestamp, payload);
                _nextSequence++;
                return true;
            }
        }
    }
}
