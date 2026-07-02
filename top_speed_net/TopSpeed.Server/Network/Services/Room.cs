using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room : IRoomService
        {
            private readonly RaceServer _owner;

            public Room(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("room", Command.RoomListRequest, (player, _, _) => _owner._notify.SendRoomList(player));
                registry.Add("room", Command.RoomStateRequest, (player, _, _) => HandleStateRequest(player));
                registry.Add("room", Command.OnlinePlayersRequest, (player, _, _) => _owner.HandleOnlinePlayersRequest(player));
                registry.Add("room", Command.RoomGetRequest, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomGetRequest(payload, out var get))
                        HandleGetRequest(player, get);
                    else
                        _owner.PacketFail(endPoint, Command.RoomGetRequest);
                });
                registry.Add("room", Command.RoomCreate, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomCreate(payload, out var create))
                        Create(player, create);
                    else
                        _owner.PacketFail(endPoint, Command.RoomCreate);
                });
                registry.Add("room", Command.RoomJoin, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomJoin(payload, out var join))
                        Join(player, join);
                    else
                        _owner.PacketFail(endPoint, Command.RoomJoin);
                });
                registry.Add("room", Command.RoomLeave, (player, _, _) => Leave(player, true));
                registry.Add("room", Command.RoomSetTrackV2, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetTrack(payload, out var track))
                        SetTrack(player, track);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetTrackV2);
                });
                registry.Add("room", Command.TrackPackageUploadBegin, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadTrackPackageUploadBegin(payload, out var begin))
                        HandlePackageUploadBegin(player, begin);
                    else
                        _owner.PacketFail(endPoint, Command.TrackPackageUploadBegin);
                });
                registry.Add("room", Command.TrackPackageUploadChunk, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadTrackPackageUploadChunk(payload, out var chunk))
                        HandlePackageUploadChunk(player, chunk);
                    else
                        _owner.PacketFail(endPoint, Command.TrackPackageUploadChunk);
                });
                registry.Add("room", Command.TrackPackageUploadEnd, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadTrackPackageUploadEnd(payload, out var end))
                        HandlePackageUploadEnd(player, end);
                    else
                        _owner.PacketFail(endPoint, Command.TrackPackageUploadEnd);
                });
                registry.Add("room", Command.TrackPackageReady, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadTrackPackageReady(payload, out var ready))
                        HandlePackageReady(player, ready);
                    else
                        _owner.PacketFail(endPoint, Command.TrackPackageReady);
                });
                registry.Add("room", Command.TrackPackageCatalogRequest, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadTrackPackageCatalogRequest(payload, out var request))
                        HandlePackageCatalogRequest(player, request);
                    else
                        _owner.PacketFail(endPoint, Command.TrackPackageCatalogRequest);
                });
                registry.Add("room", Command.VehiclePackageCatalogRequest, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadVehiclePackageCatalogRequest(payload, out _))
                        _owner.SendVehiclePackageCatalog(player, _owner.BuildVehiclePackageCatalog());
                    else
                        _owner.PacketFail(endPoint, Command.VehiclePackageCatalogRequest);
                });
                registry.Add("room", Command.VehiclePackageReady, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadVehiclePackageReady(payload, out var vehicleReady))
                        HandleVehiclePackageReady(player, vehicleReady);
                    else
                        _owner.PacketFail(endPoint, Command.VehiclePackageReady);
                });
                registry.Add("room", Command.RoomSetLaps, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetLaps(payload, out var laps))
                        SetLaps(player, laps);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetLaps);
                });
                registry.Add("room", Command.RoomStartRace, (player, _, _) => StartRoomGame(player));
                registry.Add("room", Command.RoomSetPlayersToStart, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetPlayersToStart(payload, out var setPlayers))
                        SetPlayersToStart(player, setPlayers);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetPlayersToStart);
                });
                registry.Add("room", Command.RoomSetGameRules, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetGameRules(payload, out var gameRules))
                        SetGameRules(player, gameRules);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetGameRules);
                });
                registry.Add("room", Command.RoomAddBot, (player, _, _) => AddBot(player));
                registry.Add("room", Command.RoomRemoveBot, (player, _, _) => RemoveBot(player));
                registry.Add("room", Command.RoomPlayerReady, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomPlayerReady(payload, out var ready))
                        PlayerReady(player, ready);
                    else
                        _owner.PacketFail(endPoint, Command.RoomPlayerReady);
                });
                registry.Add("room", Command.RoomPlayerWithdraw, (player, _, _) => PlayerWithdraw(player));
                registry.Add("room", Command.RoomRaceControl, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomRaceControl(payload, out var control))
                        HandleRoomControl(player, control);
                    else
                        _owner.PacketFail(endPoint, Command.RoomRaceControl);
                });
            }
        }
    }
}


