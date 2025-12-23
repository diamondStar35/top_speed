using System;

namespace TopSpeed.Common
{
    internal static class Algorithm
    {
        private static readonly Random Random = new Random(unchecked((int)DateTime.Now.Ticks));

        public static int Min(int a, int b) => a < b ? a : b;

        public static float Min(float a, float b) => a < b ? a : b;

        public static int Max(int a, int b) => a > b ? a : b;

        public static float Max(float a, float b) => a > b ? a : b;

        public static int Abs(int value) => Math.Abs(value);

        public static float Abs(float value) => Math.Abs(value);

        public static int Sign(int value) => value < 0 ? -1 : 1;

        public static float Sign(float value) => value < 0 ? -1f : 1f;

        public static int Modulo(int a, int b)
        {
            if (b == 0)
                return 0;
            var n = a / b;
            return a - n * b;
        }

        public static float Modulo(float a, float b)
        {
            if (Math.Abs(b) < 0.000001f)
                return 0f;
            var n = (int)(a / b);
            return a - n * b;
        }

        public static uint FloatToUInt32(float value)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
        }

        public static int RandomInt(int max)
        {
            if (max <= 0)
                return 0;
            lock (Random)
            {
                return Random.Next(max);
            }
        }
    }
}
