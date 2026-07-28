using System;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class DefaultControlArbiter : IControlArbiter
    {
        public CarControlIntent ResolveIntent(
            ICarController primaryController,
            ICarController? overrideController,
            in CarControlContext context)
        {
            if (overrideController != null)
            {
                var overrideIntent = overrideController.ReadIntent(context);
                // The override controller (e.g. the pit-lane auto-driver) owns steering, throttle and
                // gears, but the player can still sound the horn throughout that window. Overlay the
                // primary controller's horn so honking works while pitting. Override controllers that
                // already surface the player's horn keep it; this only fills in the ones that don't.
                if (!overrideIntent.Horn && primaryController != null && primaryController.ReadIntent(context).Horn)
                    overrideIntent = new CarControlIntent(
                        overrideIntent.Steering,
                        overrideIntent.Throttle,
                        overrideIntent.Brake,
                        overrideIntent.Clutch,
                        horn: true,
                        overrideIntent.GearUp,
                        overrideIntent.GearDown);
                return overrideIntent;
            }
            if (primaryController == null)
                throw new InvalidOperationException("Primary car controller is not configured.");

            return primaryController.ReadIntent(context);
        }
    }
}

