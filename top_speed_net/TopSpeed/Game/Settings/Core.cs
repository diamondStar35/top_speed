using System;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SaveSettings()
        {
            _settingsManager.Save(_settings);
        }

        private void RestoreDefaults()
        {
            _settings.RestoreDefaults();
            ApplyLanguage(_settings.Language, saveSettings: false, announceChange: false);
            _driveInput.SetDevice(_settings.DeviceMode);
            _input.SetDeviceMode(_settings.DeviceMode);
            _speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            _speech.OutputMode = _settings.SpeechMode;
            _speech.SpeechRate = _settings.SpeechRate;
            _speech.ScreenReaderInterrupt = _settings.ScreenReaderInterrupt;
            _speech.PreferredBackendId = _settings.SpeechBackendId;
            _speech.PreferredVoiceIndex = _settings.SpeechVoiceIndex;
            _needsCalibration = _settings.UsageHints && _settings.ScreenReaderRateMs <= 0f;
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _menu.SetMenuDigitActivation(_settings.MenuDigitActivation);
            _menu.SetMenuAutoFocus(_settings.MenuAutoFocus);
            _menu.ResetShortcutBindings();
            _menuRegistry.RefreshSpeechSettingsMenu();
            _menuRegistry.RefreshGeneralSettingsMenu();
            ApplyUpdateProxySettings();
            ApplyAudioSettings();
            SaveSettings();
            _speech.Speak(LocalizationService.Mark("Defaults restored."));
        }

        private void SetDevice(InputDeviceMode mode)
        {
            _settings.DeviceMode = mode;
            _driveInput.SetDevice(mode);
            _input.SetDeviceMode(mode);
            SaveSettings();
        }

        private void UpdateSetting(Action update)
        {
            update();
            SaveSettings();
        }

        private void SetSpeechBackend(ulong? backendId)
        {
            _settings.SpeechBackendId = backendId;
            _speech.PreferredBackendId = backendId;
            _menuRegistry.RefreshSpeechSettingsMenu();
            SaveSettings();
        }

        private void SetScreenReaderInterrupt(bool enabled)
        {
            _settings.ScreenReaderInterrupt = enabled;
            _speech.ScreenReaderInterrupt = enabled;
            SaveSettings();
        }

        private void SetSpeechMode(SpeechOutputMode mode)
        {
            _settings.SpeechMode = mode;
            _speech.OutputMode = mode;
            SaveSettings();
        }

        private void SetSpeechVoice(int? voiceIndex)
        {
            _settings.SpeechVoiceIndex = voiceIndex;
            _speech.PreferredVoiceIndex = voiceIndex;
            SaveSettings();
        }

        private void SetSpeechRate(float rate)
        {
            _settings.SpeechRate = rate;
            _speech.SpeechRate = rate;
            SaveSettings();
        }
    }
}


