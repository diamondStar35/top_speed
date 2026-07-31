using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Drive;
using TopSpeed.Drive.Multiplayer;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed class MultiplayerRaceRuntime
        {
            private const int HostRacePauseChoiceId = 3001;
            private const int HostRaceStopChoiceId = 3002;
            private const int HostRaceQuitChoiceId = 3003;

            private readonly Game _owner;
            private readonly MultiplayerRaceBinding _binding;
            private MultiplayerSession? _mode;
            private bool _quitConfirmActive;
            private bool _pitMenuShown;

            public MultiplayerRaceRuntime(Game owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _binding = new MultiplayerRaceBinding();
            }

            public MultiplayerSession? Mode => _mode;
            public bool PendingStart => _binding.PendingStart;

            public void ResetSession()
            {
                _binding.ResetSession();
            }

            public void ResetPending()
            {
                _binding.ResetPending();
            }

            public void ReplaceNetworkSession(TopSpeed.Network.MultiplayerSession session)
            {
                _mode?.ReplaceNetwork(session);
            }

            public void SetLoadout(int vehicleIndex, bool automaticTransmission)
            {
                _binding.SetLoadout(VehicleCatalog.VehicleCount, vehicleIndex, automaticTransmission);
            }

            public void SetTrack(TrackData track, string trackName, int laps)
            {
                _binding.SetTrack(track, trackName, laps);
            }

            public void ApplyRoomState(TopSpeed.Protocol.PacketRoomState roomState)
            {
                _binding.ApplyRoomState(roomState);
                _mode?.SetHostPaused(roomState.RacePaused);
                _mode?.UpdateRaceInstanceId(_binding.RaceInstanceId);
            }

            public bool ApplyRaceState(TopSpeed.Protocol.PacketRoomRaceStateChanged changed)
            {
                var applied = _binding.ApplyRaceState(changed);
                _mode?.UpdateRaceInstanceId(_binding.RaceInstanceId);
                return applied;
            }

            public bool MatchesRoom(uint roomId)
            {
                return _binding.MatchesRoom(roomId);
            }

            public bool MatchesContext(uint roomId, uint raceInstanceId, bool allowBindRaceInstance)
            {
                return _binding.MatchesContext(roomId, raceInstanceId, allowBindRaceInstance);
            }

            public bool AcceptRaceEvent(uint roomId, uint raceInstanceId, uint eventSequence, bool allowBindRaceInstance)
            {
                return _binding.AcceptRaceEvent(roomId, raceInstanceId, eventSequence, allowBindRaceInstance);
            }

            public bool ShouldRequestResync(uint roomId, uint raceInstanceId, uint eventSequence)
            {
                return _binding.ShouldRequestResync(roomId, raceInstanceId, eventSequence);
            }

            public void Run(float elapsed)
            {
                if (_mode == null)
                {
                    End();
                    return;
                }

                _owner.ProcessMultiplayerPackets();
                if (_mode == null)
                    return;

                _mode.Run(elapsed);
                if (_mode.WantsExit)
                {
                    End(_mode.ConsumeResultSummary());
                    return;
                }

                if (_mode.WantsPitStopMenu && !_pitMenuShown)
                {
                    _pitMenuShown = true;
                    _owner._choices.Show(PitStopMenu.Create(result =>
                    {
                        _pitMenuShown = false;
                        if (!result.IsCanceled)
                            _mode?.AcceptPitStopChoice(result.ChoiceId);
                    }, _owner._driveInput.GetOverlayReservedKeys()));
                }

                if (_quitConfirmActive)
                {
                    var action = _owner._menu.Update(_owner._input);
                    _owner.HandleMenuAction(action);
                    return;
                }

                if (_owner._choices.HasActiveChoiceDialog)
                {
                    var action = _owner._menu.Update(_owner._input);
                    _owner.HandleMenuAction(action);
                    return;
                }

                if (!_owner._textInputPromptActive && !_owner._dialogs.HasActiveOverlayDialog && !_owner._choices.HasActiveChoiceDialog)
                {
                    if (_owner._multiplayerCoordinator.TryHandleRaceLoopHistoryShortcuts(_owner._input))
                        return;

                    if (_owner._input.WasPressed(TopSpeed.Input.InputKey.Slash))
                    {
                        _owner._multiplayerCoordinator.OpenGlobalChatHotkey();
                        return;
                    }

                    if (_owner._input.WasPressed(TopSpeed.Input.InputKey.Backslash))
                    {
                        _owner._multiplayerCoordinator.OpenRoomChatHotkey();
                        return;
                    }
                }

                if (_owner._input.WasPressed(TopSpeed.Input.InputKey.Escape) || _owner.ConsumeDriveTouchExitRequest())
                    OpenQuitConfirmation();
            }

            public void Start()
            {
                if (_owner._session == null)
                    return;
                if (_mode != null)
                    return;
                if (_binding.WaitForTrack())
                    return;

                _binding.ClearPendingStart();
                _owner.FadeOutMenuMusic();
                var trackName = string.IsNullOrWhiteSpace(_binding.PendingTrackName) ? "custom" : _binding.PendingTrackName;
                var laps = _binding.PendingLaps > 0 ? _binding.PendingLaps : _owner._settings.NrOfLaps;
                var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _binding.VehicleIndex));

                // Prefer the effective rules delivered with this race instance (room rules minus any
                // host per-race disable); fall back to the room's current rules if not present.
                var effectiveRules = _binding.HasEffectiveGameRules
                    ? _binding.EffectiveGameRules
                    : _owner._multiplayerCoordinator.GetCurrentRaceEffectiveGameRules();
                var physicsToggles = new RacePhysicsToggles(
                    tireWearEnabled: (effectiveRules & (uint)RoomGameRules.TireWear) != 0u,
                    fuelConsumptionEnabled: (effectiveRules & (uint)RoomGameRules.FuelConsumption) != 0u);

                DisposeMode();
                _mode = _owner._driveSessionFactory.CreateMultiplayer(
                    _binding.PendingTrack!,
                    trackName,
                    _binding.AutomaticTransmission,
                    laps,
                    vehicleIndex,
                    null,
                    _owner._input.VibrationDevice,
                    _owner._session,
                    _binding.RaceInstanceId,
                    physicsToggles,
                    number => _owner._multiplayerCoordinator.ResolvePlayerName(number));
                _mode.Initialize();
                _binding.BindStartedRace();
                _owner._state = AppState.MultiplayerRace;
            }

            public void End(DriveResultSummary? resultSummary = null)
            {
                DisposeMode();
                _quitConfirmActive = false;
                _binding.ClearRaceBinding();

                if (_owner._session != null)
                {
                    _owner._input.ResetState();
                    _owner._state = AppState.Menu;
                    _owner._multiplayerCoordinator.ShowMultiplayerMenuAfterRace();
                    if (resultSummary != null)
                        _owner.ShowRaceResultDialog(resultSummary);
                }
                else
                {
                    _owner._input.ResetState();
                    _owner._state = AppState.Menu;
                    _owner._menu.ShowRoot("main");
                    _owner._menu.FadeInMenuMusic();
                    if (resultSummary != null)
                        _owner.ShowRaceResultDialog(resultSummary);
                }
            }

            public void Disconnect()
            {
                DisposeMode();
                _quitConfirmActive = false;
                _pitMenuShown = false;
                _binding.ClearRaceBinding();
                _binding.ResetPending();
            }

            public void OpenQuitConfirmation()
            {
                if (_mode == null)
                    return;
                if (_quitConfirmActive)
                    return;
                if (_owner._multiplayerCoordinator.Questions.IsQuestionMenu(_owner._menu.CurrentId))
                    return;
                if (_owner._choices.HasActiveChoiceDialog)
                    return;

                if (_owner._multiplayerCoordinator.IsCurrentRoomHost)
                {
                    OpenHostRaceActionDialog();
                    return;
                }

                _quitConfirmActive = true;

                var question = new Question(LocalizationService.Mark("Quit race?"),
                    LocalizationService.Mark("Are you sure you want to quit this multiplayer race?"),
                    QuestionId.No,
                    HandleQuitQuestionResult,
                    new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, quit the race")),
                    new QuestionButton(QuestionId.No, LocalizationService.Mark("No, continue racing"), flags: QuestionButtonFlags.Default))
                {
                    OpenAsOverlay = true
                };
                _owner._multiplayerCoordinator.Questions.Show(question);
            }

            private void OpenHostRaceActionDialog()
            {
                _quitConfirmActive = true;
                var pauseLabel = _owner._multiplayerCoordinator.IsCurrentRacePaused
                    ? LocalizationService.Mark("Resume the game")
                    : LocalizationService.Mark("Pause the game");
                var items = new Dictionary<int, string>
                {
                    [HostRacePauseChoiceId] = pauseLabel,
                    [HostRaceStopChoiceId] = LocalizationService.Mark("Stop the game"),
                    [HostRaceQuitChoiceId] = LocalizationService.Mark("Quit the race")
                };

                var dialog = new ChoiceDialog(
                    LocalizationService.Mark("What would you like to do?"),
                    LocalizationService.Mark("Choose how to proceed with the current game."),
                    items,
                    HandleHostRaceActionResult,
                    ChoiceDialogFlags.Cancelable,
                    cancelLabel: LocalizationService.Mark("Go back"))
                {
                    OpenAsOverlay = true
                };
                _owner._choices.Show(dialog);
            }

            private void HandleHostRaceActionResult(ChoiceDialogResult result)
            {
                if (result.IsCanceled)
                {
                    CancelQuitConfirmation();
                    return;
                }

                switch (result.ChoiceId)
                {
                    case HostRacePauseChoiceId:
                        SendHostRaceControl(
                            _owner._multiplayerCoordinator.IsCurrentRacePaused
                                ? RoomRaceControlAction.Resume
                                : RoomRaceControlAction.Pause,
                            _owner._multiplayerCoordinator.IsCurrentRacePaused
                                ? "race resume request"
                                : "race pause request");
                        break;

                    case HostRaceStopChoiceId:
                        SendHostRaceControl(RoomRaceControlAction.Stop, "race stop request");
                        break;

                    case HostRaceQuitChoiceId:
                        ConfirmQuit();
                        break;

                    default:
                        CancelQuitConfirmation();
                        break;
                }
            }

            public void HandleQuitQuestionResult(int resultId)
            {
                if (resultId == QuestionId.Yes)
                    ConfirmQuit();
                else if (resultId == QuestionId.No || resultId == QuestionId.Cancel || resultId == QuestionId.Close)
                    CancelQuitConfirmation();
            }

            public void CancelQuitConfirmation()
            {
                if (!_quitConfirmActive)
                    return;

                _quitConfirmActive = false;
            }

            public void ConfirmQuit()
            {
                if (!_quitConfirmActive)
                    return;

                _quitConfirmActive = false;
                if (_owner._session != null)
                    _owner.TrySendSession(_owner._session.SendRoomLeave(), "room leave request");

                DisposeMode();
                _binding.ClearRaceBinding();
                _binding.ResetPending();
                _owner._input.ResetState();
                _owner._state = AppState.Menu;
                _owner._menu.ShowRoot("multiplayer_lobby");
            }

            private void SendHostRaceControl(RoomRaceControlAction action, string requestName)
            {
                CancelQuitConfirmation();
                if (_owner._session == null)
                    return;
                _owner.TrySendSession(_owner._session.SendRoomRaceControl(action), requestName);
            }

            private void DisposeMode()
            {
                _mode?.FinalizeSession();
                _mode?.Dispose();
                _mode = null;
            }
        }
    }
}



