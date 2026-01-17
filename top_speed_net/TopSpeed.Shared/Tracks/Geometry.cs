using System.Numerics;

namespace TopSpeed.Tracks.Geometry
{
    public readonly struct TrackPose
    {
        public Vector3 Position { get; }
        public Vector3 Tangent { get; }
        public Vector3 Right { get; }
        public Vector3 Up { get; }
        public float HeadingRadians { get; }
        public float BankRadians { get; }

        public TrackPose(Vector3 position, Vector3 tangent, Vector3 right, Vector3 up, float headingRadians, float bankRadians)
        {
            Position = position;
            Tangent = tangent;
            Right = right;
            Up = up;
            HeadingRadians = headingRadians;
            BankRadians = bankRadians;
        }
    }
}
