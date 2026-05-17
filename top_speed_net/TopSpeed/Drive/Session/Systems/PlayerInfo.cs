using System;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class PlayerInfo : Subsystem
    {
        private readonly DriveInput _input;
        private readonly Func<int> _getMaxPlayerIndex;
        private readonly Func<int, bool> _hasPlayer;
        private readonly Func<int, string>? _getPlayerName;
        private readonly Func<int, string> _getVehicleName;
        private readonly Func<bool> _isStarted;
        private readonly Func<int, int>? _getPlayerPercent;
        private readonly Action<string> _speakText;
        private readonly Action? _updateExtra;
        private int _focusedPlayer = -1;

        public PlayerInfo(
            string name,
            int order,
            DriveInput input,
            Func<int> getMaxPlayerIndex,
            Func<int, bool> hasPlayer,
            Func<int, string>? getPlayerName,
            Func<int, string> getVehicleName,
            Func<bool> isStarted,
            Action<string> speakText,
            Func<int, int>? getPlayerPercent = null,
            Action? updateExtra = null)
            : base(name, order)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _getMaxPlayerIndex = getMaxPlayerIndex ?? throw new ArgumentNullException(nameof(getMaxPlayerIndex));
            _hasPlayer = hasPlayer ?? throw new ArgumentNullException(nameof(hasPlayer));
            _getPlayerName = getPlayerName;
            _getVehicleName = getVehicleName ?? throw new ArgumentNullException(nameof(getVehicleName));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
            _getPlayerPercent = getPlayerPercent;
            _updateExtra = updateExtra;
        }

        public override void Update(SessionContext context, float elapsed)
        {
            _updateExtra?.Invoke();

            var maxPlayerIndex = _getMaxPlayerIndex();
            if (!_isStarted())
                return;

            if (_input.TryGetPlayerPosition(out var positionPlayer)
                && positionPlayer >= 0
                && positionPlayer <= maxPlayerIndex
                && _hasPlayer(positionPlayer))
            {
                SpeakPlayerDetails(positionPlayer);
            }

            if (_input.GetPreviousPlayerInfoRequest())
                SelectAndSpeakPlayer(maxPlayerIndex, -1);
            if (_input.GetNextPlayerInfoRequest())
                SelectAndSpeakPlayer(maxPlayerIndex, 1);
            if (_input.GetRepeatPlayerInfoRequest())
                SpeakFocusedPlayer(maxPlayerIndex);
        }

        private void SelectAndSpeakPlayer(int maxPlayerIndex, int direction)
        {
            if (!TryStepFocusedPlayer(maxPlayerIndex, direction, out var player))
                return;

            SpeakPlayerDetails(player);
        }

        private void SpeakFocusedPlayer(int maxPlayerIndex)
        {
            if (!TryResolveFocusedPlayer(maxPlayerIndex, out var player))
                return;

            SpeakPlayerDetails(player);
        }

        private bool TryResolveFocusedPlayer(int maxPlayerIndex, out int player)
        {
            if (_focusedPlayer >= 0
                && _focusedPlayer <= maxPlayerIndex
                && _hasPlayer(_focusedPlayer))
            {
                player = _focusedPlayer;
                return true;
            }

            for (var i = 0; i <= maxPlayerIndex; i++)
            {
                if (!_hasPlayer(i))
                    continue;

                _focusedPlayer = i;
                player = i;
                return true;
            }

            player = 0;
            return false;
        }

        private bool TryStepFocusedPlayer(int maxPlayerIndex, int direction, out int player)
        {
            if (!TryResolveFocusedPlayer(maxPlayerIndex, out var current))
            {
                player = 0;
                return false;
            }

            var candidate = current;
            for (var i = 0; i <= maxPlayerIndex; i++)
            {
                candidate += direction;
                if (candidate < 0)
                    candidate = maxPlayerIndex;
                else if (candidate > maxPlayerIndex)
                    candidate = 0;

                if (!_hasPlayer(candidate))
                    continue;

                _focusedPlayer = candidate;
                player = candidate;
                return true;
            }

            player = current;
            return true;
        }

        private void SpeakPlayerDetails(int playerIndex)
        {
            var playerName = ResolvePlayerName(playerIndex);
            var playerNumber = playerIndex + 1;
            var vehicleName = LocalizationService.Translate(_getVehicleName(playerIndex));
            if (_getPlayerPercent != null)
            {
                var percent = SessionText.FormatPlayerPercentage(_getPlayerPercent(playerIndex));
                _speakText(LocalizationService.Format(
                    LocalizationService.Mark("{0}: {1}, {2}, using {3}."),
                    playerName,
                    playerNumber,
                    percent,
                    vehicleName));
                return;
            }

            _speakText(LocalizationService.Format(
                LocalizationService.Mark("{0}: {1}, using {2}."),
                playerName,
                playerNumber,
                vehicleName));
        }

        private string ResolvePlayerName(int playerIndex)
        {
            if (_getPlayerName != null)
            {
                var resolved = _getPlayerName(playerIndex);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved.Trim();
            }

            return LocalizationService.Format(
                LocalizationService.Mark("Player {0}"),
                playerIndex + 1);
        }
    }
}
