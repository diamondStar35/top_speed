using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomDraftState
    {
        public bool IsRoomBrowserOpenPending;
        public bool IsOnlinePlayersOpenPending;
        public GameRoomType CreateRoomType = GameRoomType.BotsRace;
        public byte CreateRoomPlayersToStart = 2;
        public string CreateRoomName = string.Empty;
        public int PendingLoadoutVehicleIndex;
        public bool RoomOptionsDraftActive;
        public string RoomOptionsTrackName = string.Empty;
        public TrackPackageRef RoomOptionsTrack = TrackPackageRef.BuiltIn(string.Empty);
        public string RoomOptionsTrackDisplayName = string.Empty;
        public bool RoomOptionsTrackRandom;
        public byte RoomOptionsLaps = 1;
        public byte RoomOptionsPlayersToStart = 2;
        public uint RoomOptionsGameRulesFlags;
        public uint RoomOptionsAppliedGameRulesFlags;
        public bool RoomTrackTypeOpenPending;
        public bool RoomTrackCatalogOpenPending;
        public bool RoomTrackUploadReturnToCatalog;
        public PacketTrackPackageCatalogEntry[] RoomTrackCatalog = System.Array.Empty<PacketTrackPackageCatalogEntry>();

        // Custom vehicle loadout (server catalog + the player's pending custom pick).
        public bool LoadoutVehicleCatalogOpenPending;
        public PacketVehiclePackageCatalogEntry[] LoadoutVehicleCatalog = System.Array.Empty<PacketVehiclePackageCatalogEntry>();
        public VehiclePackageRef? PendingLoadoutVehicle;
        public string PendingLoadoutVehicleDisplay = string.Empty;
        public bool PendingLoadoutVehicleSupportsAutomatic = true;
        public bool PendingLoadoutVehicleSupportsManual = true;
    }
}
