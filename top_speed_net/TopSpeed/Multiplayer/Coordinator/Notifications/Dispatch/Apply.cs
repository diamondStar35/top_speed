namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void ApplyPacketEffect(PacketEffect effect)
        {
            switch (effect.Kind)
            {
                case PacketEffectKind.PlaySound:
                    ApplyPlaySound(effect.Text);
                    break;

                case PacketEffectKind.Speak:
                    ApplySpeak(effect.Text);
                    break;

                case PacketEffectKind.AddConnectionHistory:
                    AddConnectionMessage(effect.Text);
                    break;

                case PacketEffectKind.AddGlobalChatHistory:
                    AddGlobalChatMessage(effect.Text);
                    break;

                case PacketEffectKind.AddRoomChatHistory:
                    AddRoomChatMessage(effect.Text);
                    break;

                case PacketEffectKind.AddRoomEventHistory:
                    AddRoomEventMessage(effect.Text);
                    break;

                case PacketEffectKind.ShowRootMenu:
                    _menu.ShowRoot(effect.MenuId);
                    break;

                case PacketEffectKind.PushMenu:
                    _menu.Push(effect.MenuId);
                    break;

                case PacketEffectKind.RebuildRoomControls:
                    RebuildRoomControlsMenu();
                    break;

                case PacketEffectKind.RebuildRoomOptions:
                    RebuildRoomOptionsMenu();
                    break;

                case PacketEffectKind.RebuildRoomGameRules:
                    RebuildRoomGameRulesMenu();
                    break;

                case PacketEffectKind.RebuildRoomPlayers:
                    RebuildRoomPlayersMenu();
                    break;

                case PacketEffectKind.UpdateRoomBrowser:
                    UpdateRoomBrowserMenu();
                    break;

                case PacketEffectKind.BeginRaceLoadout:
                    BeginRaceLoadoutSelection();
                    break;

                case PacketEffectKind.CancelRoomOptions:
                    CancelRoomOptionsChanges();
                    break;
            }
        }

        private void ApplyPlaySound(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
                PlayNetworkSound(fileName);
        }

        private void ApplySpeak(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                _speech.Speak(text);
        }
    }
}

