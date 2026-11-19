using System;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void CheckCurrentPing()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (_state.Connection.IsPingPending)
            {
                _speech.Speak(LocalizationService.Mark("Ping check already in progress."));
                return;
            }

            _state.Connection.IsPingPending = true;
            _state.Connection.PingStartedAtTicks = DateTime.UtcNow.Ticks;
            PlayNetworkSound("ping_start.ogg");
            if (!TrySend(session.SendPing(), "ping request"))
            {
                _state.Connection.IsPingPending = false;
                return;
            }
        }

        public void HandlePingReply(long receivedUtcTicks = 0)
        {
            _connectionFlow.HandlePingReply(receivedUtcTicks);
        }

        internal void HandlePingReplyCore(long receivedUtcTicks = 0)
        {
            if (!_state.Connection.IsPingPending)
                return;

            _state.Connection.IsPingPending = false;
            var endTicks = receivedUtcTicks > 0 ? receivedUtcTicks : DateTime.UtcNow.Ticks;
            var elapsed = TimeSpan.FromTicks(endTicks - _state.Connection.PingStartedAtTicks).TotalMilliseconds;
            if (elapsed < 0)
                elapsed = 0;
            PlayNetworkSound("ping_stop.ogg");
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("The ping took {0} milliseconds."),
                (int)Math.Round(elapsed)));
        }
    }
}



