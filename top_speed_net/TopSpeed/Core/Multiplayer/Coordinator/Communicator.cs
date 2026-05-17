using System.Globalization;
using TopSpeed.Localization;
using TopSpeed.Runtime;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void ToggleCommunicator()
        {
            _state.Communicator.Enabled = !_state.Communicator.Enabled;
            if (!_state.Communicator.Enabled)
                _state.Communicator.VoiceActivationEnabled = false;
            _speech.Speak(_state.Communicator.Enabled
                ? LocalizationService.Mark("Communicator on")
                : LocalizationService.Mark("Communicator off"));
        }

        private void ToggleCommunicatorVoiceActivation()
        {
            if (!_state.Communicator.Enabled)
            {
                _state.Communicator.VoiceActivationEnabled = false;
                return;
            }

            _state.Communicator.VoiceActivationEnabled = !_state.Communicator.VoiceActivationEnabled;
            _speech.Speak(_state.Communicator.VoiceActivationEnabled
                ? LocalizationService.Mark("Voice activation on")
                : LocalizationService.Mark("Voice activation off"));
        }

        private void BeginCommunicatorFrequencyInput()
        {
            _promptTextInput(
                LocalizationService.Mark("Set frequency"),
                FormatCommunicatorFrequency(_state.Communicator.FrequencyTenths),
                SpeechService.SpeakFlag.None,
                true,
                HandleCommunicatorFrequencyInput);
        }

        private void HandleCommunicatorFrequencyInput(TextInputResult result)
        {
            if (result.Cancelled)
                return;

            if (!TryParseCommunicatorFrequency(result.Text, out var frequencyTenths))
            {
                _speech.Speak(LocalizationService.Mark("Frequency must be between 0.0 and 1000.0 in 0.1 steps."));
                return;
            }

            _state.Communicator.FrequencyTenths = frequencyTenths;
            PlayCommunicatorFrequencyAdjustSound();
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("Frequency set to {0} MHz."),
                FormatCommunicatorFrequency(frequencyTenths)));
        }

        private void AnnounceCommunicatorFrequency()
        {
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("{0} MHz"),
                FormatCommunicatorFrequency(_state.Communicator.FrequencyTenths)));
        }

        private static string FormatCommunicatorFrequency(ushort tenths)
        {
            return (tenths / 10m).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static bool TryParseCommunicatorFrequency(string? text, out ushort tenths)
        {
            tenths = 0;
            var valueText = (text ?? string.Empty).Trim();
            if (valueText.Length == 0)
                return false;

            if (!decimal.TryParse(valueText, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
                && !decimal.TryParse(valueText, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out value))
            {
                return false;
            }

            if (value < 0m || value > 1000m)
                return false;

            var scaled = value * 10m;
            if (decimal.Truncate(scaled) != scaled)
                return false;

            if (scaled < 0m || scaled > 10000m)
                return false;

            tenths = (ushort)scaled;
            return true;
        }
    }
}
