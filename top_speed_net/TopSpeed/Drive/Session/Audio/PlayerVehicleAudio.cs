using System;
using TS.Audio;

namespace TopSpeed.Drive.Session.Audio
{
    internal sealed class PlayerVehicleAudio : IDisposable
    {
        public PlayerVehicleAudio(
            Source engine,
            Source? throttle,
            Source horn,
            Source start,
            Source? stop,
            Source brake,
            Source[] crashVariants,
            Source miniCrash,
            Source asphalt,
            Source gravel,
            Source water,
            Source sand,
            Source snow,
            Source? wipers,
            Source bump,
            Source badSwitch,
            Source fuelWarning,
            Source[] backfireVariants)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Throttle = throttle;
            Horn = horn ?? throw new ArgumentNullException(nameof(horn));
            Start = start ?? throw new ArgumentNullException(nameof(start));
            Stop = stop;
            Brake = brake ?? throw new ArgumentNullException(nameof(brake));
            CrashVariants = crashVariants ?? throw new ArgumentNullException(nameof(crashVariants));
            MiniCrash = miniCrash ?? throw new ArgumentNullException(nameof(miniCrash));
            Asphalt = asphalt ?? throw new ArgumentNullException(nameof(asphalt));
            Gravel = gravel ?? throw new ArgumentNullException(nameof(gravel));
            Water = water ?? throw new ArgumentNullException(nameof(water));
            Sand = sand ?? throw new ArgumentNullException(nameof(sand));
            Snow = snow ?? throw new ArgumentNullException(nameof(snow));
            Wipers = wipers;
            Bump = bump ?? throw new ArgumentNullException(nameof(bump));
            BadSwitch = badSwitch ?? throw new ArgumentNullException(nameof(badSwitch));
            FuelWarning = fuelWarning ?? throw new ArgumentNullException(nameof(fuelWarning));
            BackfireVariants = backfireVariants ?? throw new ArgumentNullException(nameof(backfireVariants));
        }

        public Source Engine { get; }
        public Source? Throttle { get; }
        public Source Horn { get; }
        public Source Start { get; }
        public Source? Stop { get; }
        public Source Brake { get; }
        public Source[] CrashVariants { get; }
        public Source MiniCrash { get; }
        public Source Asphalt { get; }
        public Source Gravel { get; }
        public Source Water { get; }
        public Source Sand { get; }
        public Source Snow { get; }
        public Source? Wipers { get; }
        public Source Bump { get; }
        public Source BadSwitch { get; }
        public Source FuelWarning { get; }
        public Source[] BackfireVariants { get; }
        public bool HasWipers => Wipers != null;

        public void Dispose()
        {
            DisposeSound(Engine);
            DisposeSound(Throttle);
            DisposeSound(Horn);
            DisposeSound(Start);
            DisposeSound(Stop);
            DisposeSound(Brake);
            DisposeSounds(CrashVariants);
            DisposeSound(MiniCrash);
            DisposeSound(Asphalt);
            DisposeSound(Gravel);
            DisposeSound(Water);
            DisposeSound(Sand);
            DisposeSound(Snow);
            DisposeSound(Wipers);
            DisposeSound(Bump);
            DisposeSound(BadSwitch);
            DisposeSound(FuelWarning);
            DisposeSounds(BackfireVariants);
        }

        private static void DisposeSounds(Source[] sounds)
        {
            for (var i = 0; i < sounds.Length; i++)
                DisposeSound(sounds[i]);
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
