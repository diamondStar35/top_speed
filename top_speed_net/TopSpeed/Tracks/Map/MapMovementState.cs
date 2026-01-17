using System.Numerics;

namespace TopSpeed.Tracks.Map
{
    internal struct MapMovementState
    {
        public int CellX;
        public int CellZ;
        public MapDirection Heading;
        public Vector3 WorldPosition;
        public float DistanceMeters;
        public float PendingMeters;
    }
}
