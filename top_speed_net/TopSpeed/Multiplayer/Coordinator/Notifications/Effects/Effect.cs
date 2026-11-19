namespace TopSpeed.Core.Multiplayer
{
    internal readonly struct PacketEffect
    {
        private PacketEffect(PacketEffectKind kind, string text, MenuId menuId)
        {
            Kind = kind;
            Text = text;
            MenuId = menuId;
        }

        public PacketEffectKind Kind { get; }
        public string Text { get; }
        public MenuId MenuId { get; }

        public static PacketEffect PlaySound(string fileName) => new PacketEffect(PacketEffectKind.PlaySound, fileName ?? string.Empty, default);
        public static PacketEffect Speak(string text) => new PacketEffect(PacketEffectKind.Speak, text ?? string.Empty, default);
        public static PacketEffect AddConnectionHistory(string text) => new PacketEffect(PacketEffectKind.AddConnectionHistory, text ?? string.Empty, default);
        public static PacketEffect AddGlobalChatHistory(string text) => new PacketEffect(PacketEffectKind.AddGlobalChatHistory, text ?? string.Empty, default);
        public static PacketEffect AddRoomChatHistory(string text) => new PacketEffect(PacketEffectKind.AddRoomChatHistory, text ?? string.Empty, default);
        public static PacketEffect AddRoomEventHistory(string text) => new PacketEffect(PacketEffectKind.AddRoomEventHistory, text ?? string.Empty, default);
        public static PacketEffect ShowRoot(MenuId menuId) => new PacketEffect(PacketEffectKind.ShowRootMenu, string.Empty, menuId);
        public static PacketEffect Push(MenuId menuId) => new PacketEffect(PacketEffectKind.PushMenu, string.Empty, menuId);
        public static PacketEffect RebuildRoomControls() => new PacketEffect(PacketEffectKind.RebuildRoomControls, string.Empty, default);
        public static PacketEffect RebuildRoomOptions() => new PacketEffect(PacketEffectKind.RebuildRoomOptions, string.Empty, default);
        public static PacketEffect RebuildRoomGameRules() => new PacketEffect(PacketEffectKind.RebuildRoomGameRules, string.Empty, default);
        public static PacketEffect RebuildRoomPlayers() => new PacketEffect(PacketEffectKind.RebuildRoomPlayers, string.Empty, default);
        public static PacketEffect UpdateRoomBrowser() => new PacketEffect(PacketEffectKind.UpdateRoomBrowser, string.Empty, default);
        public static PacketEffect BeginRaceLoadout() => new PacketEffect(PacketEffectKind.BeginRaceLoadout, string.Empty, default);
        public static PacketEffect CancelRoomOptions() => new PacketEffect(PacketEffectKind.CancelRoomOptions, string.Empty, default);
    }
}

