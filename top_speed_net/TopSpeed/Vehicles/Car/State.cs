namespace TopSpeed.Vehicles
{
    internal enum CarState
    {
        Stopped,
        Starting,
        Running,
        Slipping,
        Crashing,
        Crashed,  // Crash animation complete, awaiting manual restart
        Stopping
    }
}

