using System.Numerics;

namespace TS.Audio
{
    public static class CoordinateMapper
    {
        public static Vector3 ToAudioPosition(float x, float y, float z)
        {
            // TDV uses X/Y as horizontal plane and Z as altitude.
            // XAudio (and SteamAudio) use Y as up, so map (x, y, z) -> (x, z, y).
            return new Vector3(x, z, y);
        }

        public static Vector3 ToAudioVelocity(float vx, float vy, float vz, bool includeVertical)
        {
            if (includeVertical)
            {
                return new Vector3(vx, vz, vy);
            }

            return new Vector3(vx, 0f, vy);
        }

        public static Vector3 ToAudioForward(float fx, float fy, float fz)
        {
            return new Vector3(fx, fz, fy);
        }
    }
}
