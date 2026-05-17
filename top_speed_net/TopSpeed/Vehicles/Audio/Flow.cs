using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles.Audio
{
    internal sealed class Flow : IFlow
    {
        private const int MaxSurfaceFreq = 100000;
        private const float HornFadeSeconds = 0.005f;

        public void RefreshVolumes(
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
            ref int lastSurfaceLoopVolumePercent)
        {
            var enginePercent = settings.AudioVolumes?.PlayerVehicleEnginePercent ?? 100;
            var eventsPercent = settings.AudioVolumes?.PlayerVehicleEventsPercent ?? 100;
            var surfacePercent = settings.AudioVolumes?.SurfaceLoopsPercent ?? 70;

            if (!force &&
                enginePercent == lastPlayerEngineVolumePercent &&
                eventsPercent == lastPlayerEventsVolumePercent &&
                surfacePercent == lastSurfaceLoopVolumePercent)
            {
                return;
            }

            lastPlayerEngineVolumePercent = enginePercent;
            lastPlayerEventsVolumePercent = eventsPercent;
            lastSurfaceLoopVolumePercent = surfacePercent;

            SetPlayerEngineVolumePercent(settings, soundEngine, 90);
            SetPlayerEngineVolumePercent(settings, soundStart, 100);
            SetPlayerEngineVolumePercent(settings, soundThrottle, throttleVolume);
            SetPlayerEventVolumePercent(settings, soundHorn, 100);
            SetPlayerEventVolumePercent(settings, soundBrake, 100);
            SetPlayerEventVolumePercent(settings, soundMiniCrash, 100);
            SetPlayerEventVolumePercent(settings, soundBump, 100);
            SetPlayerEventVolumePercent(settings, soundBadSwitch, 100);
            SetPlayerEventVolumePercent(settings, soundFuelWarning, 100);
            SetPlayerEventVolumePercent(settings, soundWipers, 100);
            SetPlayerEventVolumePercent(settings, soundCrash, 100);
            SetPlayerEventVolumePercent(settings, soundBackfire, 100);
            for (var i = 0; i < soundCrashVariants.Length; i++)
                SetPlayerEventVolumePercent(settings, soundCrashVariants[i], 100);
            for (var i = 0; i < soundBackfireVariants.Length; i++)
                SetPlayerEventVolumePercent(settings, soundBackfireVariants[i], 100);

            SetSurfaceLoopVolumePercent(settings, soundAsphalt, 90);
            SetSurfaceLoopVolumePercent(settings, soundGravel, 90);
            SetSurfaceLoopVolumePercent(settings, soundWater, 90);
            SetSurfaceLoopVolumePercent(settings, soundSand, 90);
            SetSurfaceLoopVolumePercent(settings, soundSnow, 90);
        }

        public void UpdateHorn(Source soundHorn, CarState state, bool horning)
        {
            if (horning && state != CarState.Crashing)
            {
                if (!soundHorn.IsPlaying)
                    soundHorn.Play(loop: true, fadeInSeconds: HornFadeSeconds);
            }
            else if (soundHorn.IsPlaying)
            {
                soundHorn.Stop(HornFadeSeconds);
            }
        }

        public void UpdateRoad(
            TrackSurface surface,
            float speed,
            ref int surfaceFrequency,
            ref int prevSurfaceFrequency,
            Source soundAsphalt,
            Source soundGravel,
            Source soundWater,
            Source soundSand,
            Source soundSnow)
        {
            surfaceFrequency = (int)(speed * 500);
            if (surfaceFrequency == prevSurfaceFrequency)
                return;

            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Gravel:
                    soundGravel.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Water:
                    soundWater.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Sand:
                    soundSand.SetFrequency((int)(surfaceFrequency / 2.5f));
                    break;
                case TrackSurface.Snow:
                    soundSnow.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
            }

            prevSurfaceFrequency = surfaceFrequency;
        }

        public void ApplyPan(
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
            Source soundSnow)
        {
            soundHorn.SetPanPercent(pan);
            soundBrake.SetPanPercent(pan);
            soundBackfire?.SetPanPercent(pan);
            soundWipers?.SetPanPercent(pan);

            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.SetPanPercent(pan);
                    break;
                case TrackSurface.Gravel:
                    soundGravel.SetPanPercent(pan);
                    break;
                case TrackSurface.Water:
                    soundWater.SetPanPercent(pan);
                    break;
                case TrackSurface.Sand:
                    soundSand.SetPanPercent(pan);
                    break;
                case TrackSurface.Snow:
                    soundSnow.SetPanPercent(pan);
                    break;
            }
        }

        public int CalculatePan(float relativePosition)
        {
            var pan = (relativePosition - 0.5f) * 200.0f;
            if (pan < -100.0f)
                pan = -100.0f;
            if (pan > 100.0f)
                pan = 100.0f;
            return (int)pan;
        }

        public void Pause(
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
            Action stopResetBackfireVariants)
        {
            soundEngine.Stop();
            soundThrottle?.Stop();
            if (soundBrake.IsPlaying)
                soundBrake.Stop();
            if (soundHorn.IsPlaying)
                soundHorn.Stop();
            if (soundFuelWarning.IsPlaying)
                soundFuelWarning.Stop();
            stopResetBackfireVariants();
            soundWipers?.Stop();
            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.Stop();
                    break;
                case TrackSurface.Gravel:
                    soundGravel.Stop();
                    break;
                case TrackSurface.Water:
                    soundWater.Stop();
                    break;
                case TrackSurface.Sand:
                    soundSand.Stop();
                    break;
                case TrackSurface.Snow:
                    soundSnow.Stop();
                    break;
            }
        }

        public void Unpause(
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
            Source soundSnow)
        {
            if (resumeEngine)
                soundEngine.Play(loop: true);
            else if (soundEngine.IsPlaying)
                soundEngine.Stop();

            if (resumeThrottle)
                soundThrottle?.Play(loop: true);
            else if (soundThrottle?.IsPlaying == true)
                soundThrottle.Stop();

            if (resumeWipers)
                soundWipers?.Play(loop: true);
            else if (soundWipers?.IsPlaying == true)
                soundWipers.Stop();

            if (!resumeSurfaceLoops)
                return;

            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.Play(loop: true);
                    break;
                case TrackSurface.Gravel:
                    soundGravel.Play(loop: true);
                    break;
                case TrackSurface.Water:
                    soundWater.Play(loop: true);
                    break;
                case TrackSurface.Sand:
                    soundSand.Play(loop: true);
                    break;
                case TrackSurface.Snow:
                    soundSnow.Play(loop: true);
                    break;
            }
        }

        private static void SetPlayerEngineVolumePercent(DriveSettings settings, Source? sound, int percent)
        {
            sound.SetVolumePercent(settings, AudioVolumeCategory.PlayerVehicleEngine, percent);
        }

        private static void SetPlayerEventVolumePercent(DriveSettings settings, Source? sound, int percent)
        {
            sound.SetVolumePercent(settings, AudioVolumeCategory.PlayerVehicleEvents, percent);
        }

        private static void SetSurfaceLoopVolumePercent(DriveSettings settings, Source? sound, int percent)
        {
            sound.SetVolumePercent(settings, AudioVolumeCategory.SurfaceLoops, percent);
        }
    }
}


