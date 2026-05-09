namespace TopSpeed.Protocol
{
    // Edit release versioning values here (client/server app builds).
    public static class ReleaseVersionInfo
    {
        // Client release version used by updater checks and release packaging.
        public const ushort ClientYear = 2026;
        public const byte ClientMonth = 5;
        public const byte ClientDay = 9;
        public const byte ClientRevision = 1;

        // Server release version used by updater checks and packaging.
        public const ushort ServerYear = 2026;
        public const byte ServerMonth = 5;
        public const byte ServerDay = 5;
        public const byte ServerRevision = 2;
    }

    // Edit protocol compatibility values here (network handshake only).
    public static class ProtocolVersionInfo
    {
        // Packet envelope version (header byte).
        public const byte PacketVersion = 0x20;

        // Current protocol implementation version (year.month.day.revision).
        public const ushort CurrentYear = 2026;
        public const byte CurrentMonth = 5;
        public const byte CurrentDay = 5;
        public const byte CurrentRevision = 2;

        // Client supported protocol range (explicit values by design).
        public const ushort ClientMinYear = 2026;
        public const byte ClientMinMonth = 5;
        public const byte ClientMinDay = 5;
        public const byte ClientMinRevision = 1;
        public const ushort ClientMaxYear = 2026;
        public const byte ClientMaxMonth = 5;
        public const byte ClientMaxDay = 5;
        public const byte ClientMaxRevision = 2;

        // Server supported protocol range (explicit values by design).
        public const ushort ServerMinYear = 2026;
        public const byte ServerMinMonth = 5;
        public const byte ServerMinDay = 5;
        public const byte ServerMinRevision = 1;
        public const ushort ServerMaxYear = 2026;
        public const byte ServerMaxMonth = 5;
        public const byte ServerMaxDay = 5;
        public const byte ServerMaxRevision = 2;
    }
}
