using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void BindAudio(PlayerVehicleAudio audio)
        {
            _soundEngine = audio.Engine;
            _soundThrottle = audio.Throttle;
            _soundHorn = audio.Horn;
            _soundStart = audio.Start;
            _soundStop = audio.Stop;
            _soundBrake = audio.Brake;
            _soundCrashVariants = audio.CrashVariants;
            _soundCrash = _soundCrashVariants[0];
            _soundMiniCrash = audio.MiniCrash;
            _soundAsphalt = audio.Asphalt;
            _soundGravel = audio.Gravel;
            _soundWater = audio.Water;
            _soundSand = audio.Sand;
            _soundSnow = audio.Snow;
            _soundWipers = audio.Wipers;
            _soundBump = audio.Bump;
            _soundBadSwitch = audio.BadSwitch;
            _soundFuelWarning = audio.FuelWarning;
            _soundBackfireVariants = audio.BackfireVariants;
            _soundBackfire = _soundBackfireVariants.Length > 0 ? _soundBackfireVariants[0] : null;
            _hasWipers = audio.HasWipers ? 1 : 0;
        }

        private IVibrationDevice? InitializeVibration(IVibrationDevice? vibrationDevice)
        {
            if (vibrationDevice == null ||
                !vibrationDevice.IsAvailable ||
                !vibrationDevice.ForceFeedbackCapable ||
                !_settings.ForceFeedback ||
                !_settings.UseController)
            {
                return null;
            }
            vibrationDevice.Gain(VibrationEffectType.Gravel, 0);

            return vibrationDevice;
        }

        private void ConfigureInitialAudioState()
        {
            var enableStereoWidening = _settings.StereoWidening;

            for (var i = 0; i < _soundCrashVariants.Length; i++)
            {
                _soundCrashVariants[i].SetDopplerFactor(0f);
                _soundCrashVariants[i].SetStereoWidening(enableStereoWidening);
            }

            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                _soundBackfireVariants[i].SetStereoWidening(enableStereoWidening);
            }

            _soundEngine.SetDopplerFactor(0f);
            _soundThrottle?.SetDopplerFactor(0f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundAsphalt.SetDopplerFactor(0f);
            _soundGravel.SetDopplerFactor(0f);
            _soundWater.SetDopplerFactor(0f);
            _soundSand.SetDopplerFactor(0f);
            _soundSnow.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundFuelWarning.SetDopplerFactor(0f);
            _soundWipers?.SetDopplerFactor(0f);
            _soundStop?.SetDopplerFactor(0f);

            _soundEngine.SetStereoWidening(enableStereoWidening);
            _soundThrottle?.SetStereoWidening(enableStereoWidening);
            _soundHorn.SetStereoWidening(enableStereoWidening);
            _soundBrake.SetStereoWidening(enableStereoWidening);
            _soundBackfire?.SetStereoWidening(enableStereoWidening);
            _soundStart.SetStereoWidening(enableStereoWidening);
            _soundCrash.SetStereoWidening(enableStereoWidening);
            _soundMiniCrash.SetStereoWidening(enableStereoWidening);
            _soundBump.SetStereoWidening(enableStereoWidening);
            _soundBadSwitch.SetStereoWidening(enableStereoWidening);
            _soundFuelWarning.SetStereoWidening(enableStereoWidening);
            _soundWipers?.SetStereoWidening(enableStereoWidening);
            _soundStop?.SetStereoWidening(enableStereoWidening);
            _soundAsphalt.SetStereoWidening(enableStereoWidening);
            _soundGravel.SetStereoWidening(enableStereoWidening);
            _soundWater.SetStereoWidening(enableStereoWidening);
            _soundSand.SetStereoWidening(enableStereoWidening);
            _soundSnow.SetStereoWidening(enableStereoWidening);
            RefreshCategoryVolumes(force: true);
        }
    }
}

