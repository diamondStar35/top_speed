using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal static class OnlineMap
    {
        private static readonly string MainRoomName = LocalizationService.Mark("main room");

        public static OnlineListInfo ToList(PacketOnlinePlayers? packet)
        {
            if (packet == null || packet.Players == null || packet.Players.Length == 0)
                return new OnlineListInfo();

            var players = new OnlinePlayerInfo[packet.Players.Length];
            for (var i = 0; i < packet.Players.Length; i++)
            {
                var src = packet.Players[i] ?? new PacketOnlinePlayer();
                players[i] = new OnlinePlayerInfo
                {
                    PlayerId = src.PlayerId,
                    PlayerNumber = src.PlayerNumber,
                    Name = ResolveDisplayName(src.Name, src.PlayerNumber),
                    PresenceState = NormalizePresence(src.PresenceState),
                    RoomName = ResolveRoomName(src.RoomName)
                };
            }

            Array.Sort(players, CompareOnlinePlayers);
            return new OnlineListInfo
            {
                Players = players
            };
        }

        private static int CompareOnlinePlayers(OnlinePlayerInfo a, OnlinePlayerInfo b)
        {
            var nameCompare = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0)
                return nameCompare;
            return a.PlayerNumber.CompareTo(b.PlayerNumber);
        }

        private static string ResolveDisplayName(string name, byte playerNumber)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return name;
            return LocalizationService.Translate(LocalizationService.Mark("Player "))
                   + (playerNumber + 1);
        }

        private static string ResolveRoomName(string roomName)
        {
            if (!string.IsNullOrWhiteSpace(roomName))
                return string.Equals(roomName.Trim(), MainRoomName, StringComparison.Ordinal)
                    ? LocalizationService.Translate(MainRoomName)
                    : roomName;
            return LocalizationService.Translate(MainRoomName);
        }

        private static OnlinePresenceState NormalizePresence(OnlinePresenceState state)
        {
            return state == OnlinePresenceState.PreparingToRace || state == OnlinePresenceState.Racing
                ? state
                : OnlinePresenceState.Available;
        }
    }
}

