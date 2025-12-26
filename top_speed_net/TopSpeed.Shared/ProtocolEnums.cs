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
        Finished = 4
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
        PlayerFinished = 8,
        PlayerFinalize = 9,
        PlayerStarted = 10,
        PlayerCrashed = 11,
        PlayerBumped = 12,
        PlayerDisconnected = 13,
        LoadCustomTrack = 14,
        PlayerHello = 15,
        ServerInfo = 16,
        KeepAlive = 17,
        PlayerJoined = 18
    }
}
