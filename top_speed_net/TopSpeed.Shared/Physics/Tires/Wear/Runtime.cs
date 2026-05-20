using System;

namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearRuntime
    {
        public static TireWearRuntimeResult Resolve(TireWearConfig config, in TireWearState state)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return TireWearRuntimeCore.Resolve(config, state);
        }

        public static float ResolveInitialTemperature(TireWearConfig config, float ambientTemperatureC, float surfaceTemperatureC)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return TireWearEnvironment.ResolveInitialTemperature(config, ambientTemperatureC, surfaceTemperatureC);
        }

        public static TireWearRuntimeResult Step(TireWearConfig config, in TireWearState state, in TireWearInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return TireWearRuntimeCore.Step(config, state, input);
        }
    }
}
