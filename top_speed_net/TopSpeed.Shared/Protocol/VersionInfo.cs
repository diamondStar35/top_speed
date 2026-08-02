namespace TopSpeed.Protocol
{
    // Edit release versioning values here (client/server app builds).
    public static class ReleaseVersionInfo
    {
        // Client release version used by updater checks and release packaging.
        public const ushort ClientYear = 2026;
        public const byte ClientMonth = 7;
        public const byte ClientDay = 28;
        public const byte ClientRevision = 1;

        // Server release version used by updater checks and packaging.
        public const ushort ServerYear = 2026;
        public const byte ServerMonth = 7;
        public const byte ServerDay = 28;
        public const byte ServerRevision = 1;
    }

    // Edit protocol compatibility values here (network handshake only).
    public static class ProtocolVersionInfo
    {
        // Packet envelope version (header byte).
        public const byte PacketVersion = 0x20;

        // Current protocol implementation version (year.month.day.revision).
        public const ushort CurrentYear = 2026;
        public const byte CurrentMonth = 8;
        public const byte CurrentDay = 1;
        public const byte CurrentRevision = 1;

        // Client supported protocol range (explicit values by design). The minimum stays where it
        // was: a new client can still use an older server, which sends vehicle packages unasked, so
        // the request this version adds simply goes unanswered and nothing breaks.
        public const ushort ClientMinYear = 2026;
        public const byte ClientMinMonth = 7;
        public const byte ClientMinDay = 13;
        public const byte ClientMinRevision = 1;
        public const ushort ClientMaxYear = 2026;
        public const byte ClientMaxMonth = 8;
        public const byte ClientMaxDay = 1;
        public const byte ClientMaxRevision = 1;

        // Server supported protocol range (explicit values by design). The minimum moves up to this
        // version because the server no longer sends vehicle packages unasked, and a client too old
        // to ask for one would never receive it and would silently race with the wrong car. Refusing
        // the connection says so plainly instead.
        public const ushort ServerMinYear = 2026;
        public const byte ServerMinMonth = 8;
        public const byte ServerMinDay = 1;
        public const byte ServerMinRevision = 1;
        public const ushort ServerMaxYear = 2026;
        public const byte ServerMaxMonth = 8;
        public const byte ServerMaxDay = 1;
        public const byte ServerMaxRevision = 1;
    }
}
