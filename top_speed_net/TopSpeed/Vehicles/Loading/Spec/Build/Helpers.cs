namespace TopSpeed.Vehicles.Loader
{
    internal static partial class Spec
    {
        private static float ResolveAutoShiftRpm(float configuredAutoShiftRpm, float revLimiter)
        {
            return configuredAutoShiftRpm > 0f ? configuredAutoShiftRpm : revLimiter * 0.92f;
        }
    }
}

