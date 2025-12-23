namespace TopSpeed.Core
{
    internal enum RaceMode
    {
        QuickStart,
        TimeTrial,
        SingleRace
    }

    internal enum TrackCategory
    {
        RaceTrack,
        StreetAdventure
    }

    internal enum TransmissionMode
    {
        Automatic,
        Manual
    }

    internal sealed class RaceSetup
    {
        public RaceMode Mode { get; set; } = RaceMode.QuickStart;
        public TrackCategory TrackCategory { get; set; } = TrackCategory.RaceTrack;
        public string? TrackNameOrFile { get; set; }
        public int? VehicleIndex { get; set; }
        public string? VehicleFile { get; set; }
        public TransmissionMode Transmission { get; set; } = TransmissionMode.Automatic;

        public void ClearSelection()
        {
            TrackNameOrFile = null;
            VehicleIndex = null;
            VehicleFile = null;
            Transmission = TransmissionMode.Automatic;
        }
    }
}
