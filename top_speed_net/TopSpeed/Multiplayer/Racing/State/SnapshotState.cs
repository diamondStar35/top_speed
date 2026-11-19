using System.Collections.Generic;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed class SnapshotState
    {
        public SnapshotState(int bufferCapacity)
        {
            Frames = new List<SnapshotFrame>(bufferCapacity);
            MissingPlayers = new List<byte>();
        }

        public List<SnapshotFrame> Frames { get; }
        public List<byte> MissingPlayers { get; }
        public uint LastSequence { get; set; }
        public uint LastTick { get; set; }
        public bool HasSequence { get; set; }
        public float TickNow { get; set; }
        public bool HasTickNow { get; set; }
    }
}
