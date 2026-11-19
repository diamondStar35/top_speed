using System;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Drive.Session.Audio
{
    internal sealed class RemoteVehicleAudio : IDisposable
    {
        public RemoteVehicleAudio(
            Source engine,
            Source horn,
            Source start,
            Source crash,
            Source brake,
            Source miniCrash,
            Source bump,
            Source? backfire)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Horn = horn ?? throw new ArgumentNullException(nameof(horn));
            Start = start ?? throw new ArgumentNullException(nameof(start));
            Crash = crash ?? throw new ArgumentNullException(nameof(crash));
            Brake = brake ?? throw new ArgumentNullException(nameof(brake));
            MiniCrash = miniCrash ?? throw new ArgumentNullException(nameof(miniCrash));
            Bump = bump ?? throw new ArgumentNullException(nameof(bump));
            Backfire = backfire;
        }

        public Source Engine { get; }
        public Source Horn { get; }
        public Source Start { get; }
        public Source Crash { get; }
        public Source Brake { get; }
        public Source MiniCrash { get; }
        public Source Bump { get; }
        public Source? Backfire { get; }

        public void UpdateBraking(bool braking, TrackSurface surface)
        {
            if (braking && surface == TrackSurface.Asphalt)
            {
                if (!Brake.IsPlaying)
                    Brake.Play(loop: true);
            }
            else if (Brake.IsPlaying)
            {
                Brake.Stop();
            }
        }

        public void Dispose()
        {
            DisposeSound(Engine);
            DisposeSound(Horn);
            DisposeSound(Start);
            DisposeSound(Crash);
            DisposeSound(Brake);
            DisposeSound(MiniCrash);
            DisposeSound(Bump);
            DisposeSound(Backfire);
        }

        private static void DisposeSound(Source? sound)
        {
            if (sound == null)
                return;

            sound.Stop();
            sound.Dispose();
        }

    }
}
