using System;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed class MediaTransfer
    {
        public uint MediaId { get; set; }
        public uint TransferId { get; set; }
        public TopSpeed.Protocol.MediaTransferState State { get; set; } = TopSpeed.Protocol.MediaTransferState.Idle;
        public string Extension { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Offset { get; set; }
        public ushort NextChunkIndex { get; set; }

        public bool IsComplete => Data.Length > 0 && Offset >= Data.Length;
    }
}
