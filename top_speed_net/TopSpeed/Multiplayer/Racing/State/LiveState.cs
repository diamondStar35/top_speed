using System;
using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed class LiveState
    {
        private const int MaxBufferedFrames = 64;

        public LiveState(PacketPlayerLiveStart start, long receivedUtcTicks)
        {
            StreamId = start.StreamId;
            Codec = start.Codec;
            SampleRate = start.SampleRate;
            Channels = start.Channels;
            FrameMs = start.FrameMs;
            LastReceivedUtcTicks = receivedUtcTicks;
            Frames = new Queue<LiveFrame>(MaxBufferedFrames);
        }

        public uint StreamId { get; }
        public LiveCodec Codec { get; }
        public ushort SampleRate { get; }
        public byte Channels { get; }
        public byte FrameMs { get; }
        public bool HasSequence { get; private set; }
        public ushort NextSequence { get; private set; }
        public uint LastTimestamp { get; private set; }
        public long LastReceivedUtcTicks { get; private set; }
        public Queue<LiveFrame> Frames { get; }
        public long ReceivedFrames { get; private set; }
        public long DroppedFrames { get; private set; }
        public long ForwardedFrames { get; private set; }
        public long DecodeDroppedFrames { get; private set; }

        public bool TryPush(PacketPlayerLiveFrame frame, long receivedUtcTicks)
        {
            if (frame.StreamId != StreamId)
                return false;
            if (frame.Data == null || frame.Data.Length == 0 || frame.Data.Length > ProtocolConstants.MaxLiveFrameBytes)
                return false;

            ReceivedFrames++;
            if (HasSequence && frame.Sequence != NextSequence)
            {
                if (!IsNewerSequence(frame.Sequence, NextSequence))
                {
                    DroppedFrames++;
                    return false;
                }

                Frames.Clear();
            }

            if (Frames.Count >= MaxBufferedFrames)
            {
                Frames.Dequeue();
                DroppedFrames++;
            }

            var payload = new byte[frame.Data.Length];
            Buffer.BlockCopy(frame.Data, 0, payload, 0, frame.Data.Length);
            Frames.Enqueue(new LiveFrame(frame.Sequence, frame.Timestamp, payload, receivedUtcTicks));
            HasSequence = true;
            NextSequence = unchecked((ushort)(frame.Sequence + 1));
            LastTimestamp = frame.Timestamp;
            LastReceivedUtcTicks = receivedUtcTicks;
            return true;
        }

        public void MarkForwarded()
        {
            ForwardedFrames++;
        }

        public void MarkDecodeDropped()
        {
            DecodeDroppedFrames++;
        }

        private static bool IsNewerSequence(ushort sequence, ushort expected)
        {
            var delta = (ushort)(sequence - expected);
            return delta != 0 && delta < 32768;
        }
    }

    internal readonly struct LiveFrame
    {
        public LiveFrame(ushort sequence, uint timestamp, byte[] payload, long receivedUtcTicks)
        {
            Sequence = sequence;
            Timestamp = timestamp;
            Payload = payload ?? Array.Empty<byte>();
            ReceivedUtcTicks = receivedUtcTicks;
        }

        public ushort Sequence { get; }
        public uint Timestamp { get; }
        public byte[] Payload { get; }
        public long ReceivedUtcTicks { get; }
    }
}
