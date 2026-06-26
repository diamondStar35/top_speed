using System;

namespace TopSpeed.Drive.Session
{
    internal enum EventId
    {
        PhaseChanged,
        VehicleStart,
        ProgressStart,
        ProgressFinish,
        FinalizeResults,
        PlaySound,
        PlayInfoSound,
        PlayTrackInfoSound,
        PlayUnkey
    }

    internal sealed class Event
    {
        public Event(EventId id, object? data = null)
        {
            Id = id;
            Data = data;
        }

        public EventId Id { get; }
        public object? Data { get; }
    }
}

