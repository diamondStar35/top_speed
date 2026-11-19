namespace TopSpeed.Core.Multiplayer
{
    internal enum PacketEffectKind
    {
        None,
        PlaySound,
        Speak,
        AddConnectionHistory,
        AddGlobalChatHistory,
        AddRoomChatHistory,
        AddRoomEventHistory,
        ShowRootMenu,
        PushMenu,
        RebuildRoomControls,
        RebuildRoomOptions,
        RebuildRoomGameRules,
        RebuildRoomPlayers,
        UpdateRoomBrowser,
        BeginRaceLoadout,
        CancelRoomOptions
    }
}

