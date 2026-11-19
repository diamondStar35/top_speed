namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CommunicatorState
    {
        public const ushort DefaultFrequencyTenths = 10; // 1.0 MHz

        public bool Enabled;
        public bool VoiceActivationEnabled;
        public ushort FrequencyTenths = DefaultFrequencyTenths;

        public void Reset()
        {
            Enabled = false;
            VoiceActivationEnabled = false;
            FrequencyTenths = DefaultFrequencyTenths;
        }
    }
}
