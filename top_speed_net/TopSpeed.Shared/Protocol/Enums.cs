namespace TopSpeed.Protocol
{
    public enum CarType : byte
    {
        Vehicle1 = 0,
        Vehicle2 = 1,
        Vehicle3 = 2,
        Vehicle4 = 3,
        Vehicle5 = 4,
        Vehicle6 = 5,
        Vehicle7 = 6,
        Vehicle8 = 7,
        Vehicle9 = 8,
        Vehicle10 = 9,
        Vehicle11 = 10,
        Vehicle12 = 11,
        CustomVehicle = 12
    }

    public enum PlayerState : byte
    {
        Undefined = 0,
        NotReady = 1,
        AwaitingStart = 2,
        Racing = 3,
        Finished = 4,
        Pitting = 5
    }

    public enum OnlinePresenceState : byte
    {
        Available = 0,
        PreparingToRace = 1,
        Racing = 2
    }

    public enum VehicleAction : byte
    {
        Engine = 0,
        Start = 1,
        Horn = 2,
        Throttle = 3,
        Crash = 4,
        Brake = 5,
        Backfire = 6,
        Stop = 7
    }

    public enum LiveCodec : byte
    {
        None = 0,
        Opus = 1
    }

    public enum Command : byte
    {
        Disconnect = 0,
        PlayerNumber = 1,
        PlayerData = 2,
        PlayerState = 3,
        StartRace = 4,
        StopRace = 5,
        RaceAborted = 6,
        PlayerDataToServer = 7,
        PlayerFinalize = 9,
        PlayerStarted = 10,
        PlayerCrashed = 11,
        PlayerBumped = 12,
        PlayerDisconnected = 13,
        LoadCustomTrack = 14,
        PlayerHello = 15,
        ServerInfo = 16,
        KeepAlive = 17,
        Ping = 18,
        Pong = 19,
        PlayerJoined = 20,
        RoomListRequest = 21,
        RoomList = 22,
        RoomCreate = 23,
        RoomJoin = 24,
        RoomLeave = 25,
        RoomState = 26,
        RoomSetTrack = 27,
        RoomSetLaps = 28,
        RoomStartRace = 29,
        ProtocolMessage = 30,
        RoomSetPlayersToStart = 31,
        RoomAddBot = 32,
        RoomRemoveBot = 33,
        RoomPrepareRace = 34,
        RoomPlayerReady = 35,
        PlayerMediaBegin = 36,
        PlayerMediaChunk = 37,
        PlayerMediaEnd = 38,
        RaceSnapshot = 39,
        RoomStateRequest = 40,
        RoomEvent = 41,
        RoomGetRequest = 42,
        RoomGet = 43,
        ProtocolHello = 44,
        ProtocolWelcome = 45,
        PlayerLiveStart = 46,
        PlayerLiveFrame = 47,
        PlayerLiveStop = 48,
        RoomPlayerWithdraw = 49,
        OnlinePlayersRequest = 50,
        OnlinePlayers = 51,
        RoomSetGameRules = 52,
        RoomRaceStateChanged = 53,
        RoomRacePlayerFinished = 54,
        RoomRaceCompleted = 55,
        RoomRaceAborted = 56,
        RoomRaceControl = 57,
        RoomSetTrackV2 = 58,
        TrackPackageUploadBegin = 59,
        TrackPackageUploadChunk = 60,
        TrackPackageUploadEnd = 61,
        TrackPackageUploadResult = 62,
        TrackPackageTransferBegin = 63,
        TrackPackageTransferChunk = 64,
        TrackPackageTransferEnd = 65,
        TrackPackageReady = 66,
        TrackPackageCatalogRequest = 67,
        TrackPackageCatalog = 68,
        ClientHeartbeat = 69,
        ServerHeartbeat = 70,
        PlayerVoiceStart = 71,
        PlayerVoiceFrame = 72,
        PlayerVoiceStop = 73,
        PlayerCommunicatorMediaBegin = 74,
        PlayerCommunicatorMediaChunk = 75,
        PlayerCommunicatorMediaEnd = 76,
        PlayerCommunicatorMediaState = 77,
        VehiclePackageTransferBegin = 78,
        VehiclePackageTransferChunk = 79,
        VehiclePackageTransferEnd = 80,
        VehiclePackageReady = 81,
        VehiclePackageCatalogRequest = 82,
        VehiclePackageCatalog = 83,
        RoomPlayerVehicle = 84,
        // Client asking for a vehicle package it does not already hold. Packages are sent only in
        // reply to this, so a client that already has one never receives it again.
        VehiclePackageRequest = 85
    }

    public enum ProtocolMessageCode : byte
    {
        None = 0,
        Ok = 1,
        Failed = 2,
        NotHost = 3,
        RoomFull = 4,
        RoomNotFound = 5,
        InvalidTrack = 6,
        InvalidLaps = 7,
        NotInRoom = 8,
        InvalidPlayersToStart = 9,
        ServerPlayerConnected = 10,
        ServerPlayerDisconnected = 11,
        Chat = 12,
        RoomChat = 13
    }

    public enum GameRoomType : byte
    {
        BotsRace = 0,
        OneOnOne = 1,
        PlayersRace = 2
    }

    public enum RoomEventKind : byte
    {
        None = 0,
        RoomCreated = 1,
        RoomRemoved = 2,
        RoomSummaryUpdated = 3,
        HostChanged = 4,
        TrackChanged = 5,
        LapsChanged = 6,
        PlayersToStartChanged = 7,
        ParticipantJoined = 8,
        ParticipantLeft = 9,
        ParticipantStateChanged = 10,
        BotAdded = 11,
        BotRemoved = 12,
        PrepareStarted = 13,
        PrepareCancelled = 14,
        RaceStarted = 15,
        RaceStopped = 16,
        GameRulesChanged = 17,
        RacePaused = 18,
        RaceResumed = 19
    }

    public enum RoomRaceState : byte
    {
        Lobby = 0,
        Preparing = 1,
        Racing = 2,
        Completed = 3,
        Aborted = 4
    }

    public enum RoomRaceResultStatus : byte
    {
        None = 0,
        Pending = 1,
        Finished = 2,
        Dnf = 3
    }

    public enum RoomRaceAbortReason : byte
    {
        None = 0,
        InvalidTrack = 1,
        InternalError = 2
    }

    public enum RoomRaceControlAction : byte
    {
        None = 0,
        CancelPrepare = 1,
        Pause = 2,
        Resume = 3,
        Stop = 4
    }

    public enum MediaTransferState : byte
    {
        Idle = 0,
        Receiving = 1,
        Complete = 2,
        Cancelled = 3,
        Expired = 4
    }

    public enum ConnectionLifecycleState : byte
    {
        TransportConnected = 0,
        ProtocolNegotiated = 1,
        PlayerIdentified = 2,
        SessionReady = 3,
        InRoom = 4,
        Active = 5,
        SuspectedLost = 6,
        Reconnecting = 7,
        Resumed = 8,
        Expired = 9,
        Closed = 10,

        // Backward-compatible aliases for previous lifecycle labels.
        Connected = Active,
        Suspended = SuspectedLost
    }

    public enum MultiplayerConnectionState : byte
    {
        Connecting = 0,
        Connected = 1,
        ConnectionLostSuspected = 2,
        DisconnectedCleanly = 3,
        TimedOut = 4,
        Kicked = 5,
        ServerShutdown = 6,
        ProtocolError = 7
    }

    public enum MultiplayerDisconnectReason : byte
    {
        Unknown = 0,
        DisconnectedCleanly = 1,
        TimedOut = 2,
        Kicked = 3,
        ServerShutdown = 4,
        ProtocolError = 5,
        ConnectionFailed = 6,
        ConnectionRejected = 7,
        NetworkError = 8,
        HostUnreachable = 9,
        UnknownHost = 10,
        PeerNotFound = 11,
        LocalDisconnect = 12,
        ReconnectRequested = 13
    }

    public enum ProtocolCompatStatus : byte
    {
        None = 0,
        Exact = 1,
        CompatibleDowngrade = 2,
        ClientTooOld = 3,
        ClientTooNew = 4,
        NoCommonVersion = 5
    }
}
