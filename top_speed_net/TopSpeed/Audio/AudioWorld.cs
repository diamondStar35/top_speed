using System.Numerics;

namespace TopSpeed.Audio
{
    internal static class AudioWorld
    {
        // Game positions are expected in meters; adjust only if legacy units remain.
        public const float UnitsToMeters = 1.0f;

        public static float ToMeters(float value)
        {
            return value * UnitsToMeters;
        }

        public static Vector3 ToMeters(Vector3 value)
        {
            return value * UnitsToMeters;
        }

        public static Vector3 Position(float x, float z)
        {
            return new Vector3(ToMeters(x), 0f, ToMeters(z));
        }

        public static float WrapDelta(float delta, float length)
        {
            if (length <= 0f)
                return delta;

            delta = ((delta % length) + length) % length;
            if (delta > length * 0.5f)
                delta -= length;
            return delta;
        }

        public static Vector3 PositionWrapped(float sourceX, float sourceZ, float listenerX, float listenerZ, float trackLength)
        {
            var dx = sourceX - listenerX;
            var dz = WrapDelta(sourceZ - listenerZ, trackLength);
            return Position(listenerX + dx, listenerZ + dz);
        }
    }
}
