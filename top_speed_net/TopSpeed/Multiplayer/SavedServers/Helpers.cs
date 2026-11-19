using System;
using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private static SavedServerEntry CloneSavedServer(SavedServerEntry source)
        {
            if (source == null)
                return new SavedServerEntry();

            return new SavedServerEntry
            {
                Name = source.Name ?? string.Empty,
                Host = source.Host ?? string.Empty,
                Port = source.Port,
                DefaultCallSign = source.DefaultCallSign ?? string.Empty
            };
        }

        private static SavedServerEntry NormalizeSavedServerDraft(SavedServerEntry source)
        {
            var copy = CloneSavedServer(source);
            copy.Name = (copy.Name ?? string.Empty).Trim();
            copy.Host = (copy.Host ?? string.Empty).Trim();
            copy.DefaultCallSign = (copy.DefaultCallSign ?? string.Empty).Trim();
            if (copy.Port < 0 || copy.Port > 65535)
                copy.Port = 0;
            return copy;
        }

        private int ResolveSavedServerPort(SavedServerEntry server)
        {
            if (server != null && server.Port >= 1 && server.Port <= 65535)
                return server.Port;
            return ResolveServerPort();
        }
    }
}

