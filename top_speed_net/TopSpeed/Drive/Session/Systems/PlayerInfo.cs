using System;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Tracks;

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
        private readonly Func<bool>? _isBrief;
        private readonly Func<int, float>? _getPlayerPosition;
        private readonly Track? _track;
        private readonly Func<int>? _getLapLimit;
        private readonly Func<bool>? _reportLapAndTurn;
        private readonly Func<int, bool>? _isPlayerFinished;
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
            Action? updateExtra = null,
            Func<bool>? isBrief = null,
            Func<int, float>? getPlayerPosition = null,
            Track? track = null,
            Func<int>? getLapLimit = null,
            Func<bool>? reportLapAndTurn = null,
            Func<int, bool>? isPlayerFinished = null)
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
            _isBrief = isBrief;
            _getPlayerPosition = getPlayerPosition;
            _track = track;
            _getLapLimit = getLapLimit;
            _reportLapAndTurn = reportLapAndTurn;
            _isPlayerFinished = isPlayerFinished;
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
            var brief = _isBrief?.Invoke() ?? false;
            var playerName = ResolvePlayerName(playerIndex);
            var playerNumber = playerIndex + 1;
            var vehicleName = LocalizationService.Translate(_getVehicleName(playerIndex));
            var positionText = ResolvePositionText(playerIndex, brief);
            if (positionText != null)
            {
                _speakText(brief
                    ? LocalizationService.Format(
                        LocalizationService.Mark("{0} {1} {2}"),
                        positionText,
                        playerName,
                        vehicleName)
                    : LocalizationService.Format(
                        LocalizationService.Mark("{0}, {1}, using {2}."),
                        playerName,
                        positionText,
                        vehicleName));
                return;
            }

            _speakText(brief
                ? LocalizationService.Format(
                    LocalizationService.Mark("{0} {1}"),
                    playerName,
                    vehicleName)
                : LocalizationService.Format(
                    LocalizationService.Mark("{0}: {1}, using {2}."),
                    playerName,
                    playerNumber,
                    vehicleName));
        }

        // The position slot of the number-row readout: either lap-and-turn (spotter style) or the
        // race percentage, depending on the setting. Returns null when there is no position info to
        // speak at all (e.g. time trial with percentage disabled), so the caller drops the slot.
        private string? ResolvePositionText(int playerIndex, bool brief)
        {
            // A finished car reports "finished" regardless of readout mode; its live position would
            // otherwise read as a stale lap/turn or a flat 100 percent while it waits out the race.
            if (_isPlayerFinished?.Invoke(playerIndex) == true)
                return SessionText.FormatFinished();

            if (_reportLapAndTurn?.Invoke() == true && _track != null && _getPlayerPosition != null)
            {
                var position = _getPlayerPosition(playerIndex);
                var lap = ClampLap(_track.Lap(position));
                if (!_track.TryGetTurn(position, out var turn, out var inTurn, out var approachingLapEnd))
                    return SessionText.FormatLapOnly(lap, brief);

                if (approachingLapEnd)
                {
                    // On the closing straight the only turn ahead is next lap's turn 1, so speak the
                    // lap rather than that turn; on the final lap there is no next lap to enter.
                    return IsFinalLap(lap)
                        ? SessionText.FormatApproachingFinish(lap, brief)
                        : SessionText.FormatCompletingLap(lap, brief);
                }

                return SessionText.FormatLapAndTurn(lap, turn, inTurn, brief);
            }

            if (_getPlayerPercent != null)
                return SessionText.FormatPlayerPercentage(_getPlayerPercent(playerIndex));

            return null;
        }

        private int ClampLap(int lap)
        {
            if (lap < 1)
                lap = 1;
            var limit = _getLapLimit?.Invoke() ?? 0;
            if (limit > 0 && lap > limit)
                lap = limit;
            return lap;
        }

        private bool IsFinalLap(int lap)
        {
            var limit = _getLapLimit?.Invoke() ?? 0;
            return limit > 0 && lap >= limit;
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
