using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles.Audio
{
    internal interface IFlow
    {
        void RefreshVolumes(
            DriveSettings settings,
            bool force,
            int throttleVolume,
            Source soundEngine,
            Source soundStart,
            Source? soundThrottle,
            Source soundHorn,
            Source soundBrake,
            Source soundMiniCrash,
            Source soundBump,
            Source soundBadSwitch,
            Source soundFuelWarning,
            Source? soundWipers,
            Source soundCrash,
            Source? soundBackfire,
            Source[] soundCrashVariants,
            Source[] soundBackfireVariants,
            Source soundAsphalt,
            Source soundGravel,
            Source soundWater,
            Source soundSand,
            Source soundSnow,
            ref int lastPlayerEngineVolumePercent,
            ref int lastPlayerEventsVolumePercent,
            ref int lastSurfaceLoopVolumePercent);

        void UpdateHorn(Source soundHorn, CarState state, bool horning);
        void UpdateRoad(
            TrackSurface surface,
            float speed,
            ref int surfaceFrequency,
            ref int prevSurfaceFrequency,
            Source soundAsphalt,
            Source soundGravel,
            Source soundWater,
            Source soundSand,
            Source soundSnow);

        void ApplyPan(
            TrackSurface surface,
            int pan,
            Source soundHorn,
            Source soundBrake,
            Source? soundBackfire,
            Source? soundWipers,
            Source soundAsphalt,
            Source soundGravel,
            Source soundWater,
            Source soundSand,
            Source soundSnow);

        int CalculatePan(float relativePosition);

        void Pause(
            TrackSurface surface,
            Source soundEngine,
            Source? soundThrottle,
            Source soundBrake,
            Source soundHorn,
            Source soundFuelWarning,
            Source? soundWipers,
            Source soundAsphalt,
            Source soundGravel,
            Source soundWater,
            Source soundSand,
            Source soundSnow,
            System.Action stopResetBackfireVariants);

        void Unpause(
            TrackSurface surface,
            bool resumeEngine,
            bool resumeThrottle,
            bool resumeWipers,
            bool resumeSurfaceLoops,
            Source soundEngine,
            Source? soundThrottle,
            Source? soundWipers,
            Source soundAsphalt,
            Source soundGravel,
            Source soundWater,
            Source soundSand,
            Source soundSnow);
    }
}


