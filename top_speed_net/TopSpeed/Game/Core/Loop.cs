namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public void Update(float deltaSeconds)
        {
            _input.Update();
            UpdateDriveTouchControls(deltaSeconds);
            UpdateMultiplayerMenuTouchControls();
            _driveInput.Run(_input.CaptureDriveInputFrame(), deltaSeconds);

            TryShowDeviceChoiceDialog();

            var overlayDialogActive =
                _multiplayerCoordinator.Questions.HasActiveOverlayQuestion
                || _dialogs.HasActiveOverlayDialog
                || _choices.HasActiveChoiceDialog;
            // The full driving block is multiplayer-only, but the lighter menu-navigation trap applies
            // in every race mode so a menu that opens mid-race (e.g. the pit-stop choice dialog) always
            // keeps its navigation keys/buttons for itself. See DriveInput.IsInputTrappedByMenu.
            _driveInput.SetOverlayInputBlocked(_state == AppState.MultiplayerRace && overlayDialogActive);
            _driveInput.SetMenuNavigationActive(
                (_state == AppState.SingleRace
                 || _state == AppState.TimeTrial
                 || _state == AppState.MultiplayerRace)
                && overlayDialogActive);

            HandleGlobalVolumeShortcuts();
            UpdateTextInputPrompt();
            UpdateSessionReconnect();
            _stateMachine.Update(deltaSeconds);
            _multiplayerCommunicatorRuntime.Update(deltaSeconds);

            if (_pendingDriveStart)
            {
                _pendingDriveStart = false;
                StartDrive(_pendingMode);
            }
        }
    }
}


