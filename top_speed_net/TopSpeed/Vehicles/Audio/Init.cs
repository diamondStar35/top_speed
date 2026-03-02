using System.IO;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void InitializeAudioAssets(VehicleDefinition definition)
        {
            _soundEngine = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Engine), looped: true, allowHrtf: true);
            _soundStart = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Start));
            _soundHorn = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Horn), looped: true);
            _soundThrottle = TryCreateSound(definition.GetSoundPath(VehicleAction.Throttle), looped: true, allowHrtf: true);
            _soundCrashVariants = CreateRequiredSoundVariants(
                definition.GetSoundPaths(VehicleAction.Crash),
                definition.GetSoundPath(VehicleAction.Crash));
            _soundCrash = _soundCrashVariants[0];
            _soundBrake = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Brake), looped: true, allowHrtf: false);
            _soundBackfireVariants = CreateOptionalSoundVariants(
                definition.GetSoundPaths(VehicleAction.Backfire),
                definition.GetSoundPath(VehicleAction.Backfire));
            _soundBackfire = _soundBackfireVariants.Length > 0 ? _soundBackfireVariants[0] : null;

            _hasWipers = definition.HasWipers == 1 ? 1 : 0;
            if (_hasWipers == 1)
                _soundWipers = CreateRequiredSound(Path.Combine(_legacyRoot, "wipers.wav"), looped: true, allowHrtf: false);

            _soundAsphalt = CreateRequiredSound(Path.Combine(_legacyRoot, "asphalt.wav"), looped: true, allowHrtf: false);
            _soundGravel = CreateRequiredSound(Path.Combine(_legacyRoot, "gravel.wav"), looped: true, allowHrtf: false);
            _soundWater = CreateRequiredSound(Path.Combine(_legacyRoot, "water.wav"), looped: true, allowHrtf: false);
            _soundSand = CreateRequiredSound(Path.Combine(_legacyRoot, "sand.wav"), looped: true, allowHrtf: false);
            _soundSnow = CreateRequiredSound(Path.Combine(_legacyRoot, "snow.wav"), looped: true, allowHrtf: false);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"));
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"), allowHrtf: false);
            _soundBadSwitch = CreateRequiredSound(Path.Combine(_legacyRoot, "badswitch.wav"), allowHrtf: false);
        }

        private IVibrationDevice? InitializeVibration(IVibrationDevice? vibrationDevice)
        {
            if (vibrationDevice == null ||
                !vibrationDevice.IsAvailable ||
                !vibrationDevice.ForceFeedbackCapable ||
                !_settings.ForceFeedback ||
                !_settings.UseJoystick)
            {
                return null;
            }

            vibrationDevice.LoadEffect(VibrationEffectType.Start, Path.Combine(_effectsRoot, "carstart.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.Crash, Path.Combine(_effectsRoot, "crash.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.Spring, Path.Combine(_effectsRoot, "spring.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.Engine, Path.Combine(_effectsRoot, "engine.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.CurbLeft, Path.Combine(_effectsRoot, "curbleft.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.CurbRight, Path.Combine(_effectsRoot, "curbright.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.Gravel, Path.Combine(_effectsRoot, "gravel.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.BumpLeft, Path.Combine(_effectsRoot, "bumpleft.ffe"));
            vibrationDevice.LoadEffect(VibrationEffectType.BumpRight, Path.Combine(_effectsRoot, "bumpright.ffe"));
            vibrationDevice.Gain(VibrationEffectType.Gravel, 0);

            return vibrationDevice;
        }

        private void ConfigureInitialAudioState()
        {
            for (var i = 0; i < _soundCrashVariants.Length; i++)
                _soundCrashVariants[i].SetDopplerFactor(0f);

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
            _soundWipers?.SetDopplerFactor(0f);

            if (_settings.StereoWidening)
            {
                _soundEngine.SetStereoWidening(true);
                _soundThrottle?.SetStereoWidening(true);
                _soundHorn.SetStereoWidening(true);
                _soundBrake.SetStereoWidening(true);
                for (var i = 0; i < _soundCrashVariants.Length; i++) _soundCrashVariants[i].SetStereoWidening(true);
                for (var i = 0; i < _soundBackfireVariants.Length; i++) _soundBackfireVariants[i].SetStereoWidening(true);
                _soundStart.SetStereoWidening(true);
                _soundMiniCrash.SetStereoWidening(true);
                _soundBump.SetStereoWidening(true);
                _soundBadSwitch.SetStereoWidening(true);
                _soundWipers?.SetStereoWidening(true);
                _soundAsphalt.SetStereoWidening(true);
                _soundGravel.SetStereoWidening(true);
                _soundWater.SetStereoWidening(true);
                _soundSand.SetStereoWidening(true);
                _soundSnow.SetStereoWidening(true);
            }

            RefreshCategoryVolumes(force: true);
        }
    }
}
