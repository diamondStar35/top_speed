using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public bool GetToggleShiftOnDemand() => _allowAuxiliaryInput && WasPressed(Key.M);

        public bool TryGetPlayerPosition(out int player)
        {
            if (!_allowAuxiliaryInput)
            {
                player = 0;
                return false;
            }

            if (WasPressed(_kbPlayerPos1)) { player = 0; return true; }
            if (WasPressed(_kbPlayerPos2)) { player = 1; return true; }
            if (WasPressed(_kbPlayerPos3)) { player = 2; return true; }
            if (WasPressed(_kbPlayerPos4)) { player = 3; return true; }
            if (WasPressed(_kbPlayerPos5)) { player = 4; return true; }
            if (WasPressed(_kbPlayerPos6)) { player = 5; return true; }
            if (WasPressed(_kbPlayerPos7)) { player = 6; return true; }
            if (WasPressed(_kbPlayerPos8)) { player = 7; return true; }
            if (WasPressed(_kbPlayerPos9)) { player = 8; return true; }
            if (WasPressed(_kbPlayerPos0)) { player = 9; return true; }
            player = 0;
            return false;
        }

        public bool GetPlayerNumber() => _allowAuxiliaryInput && WasPressed(_kbPlayerNumber);

        public bool GetPreviousPlayerInfoRequest() => _allowAuxiliaryInput && !_overlayInputBlocked && _touchPreviousPlayerInfo;

        public bool GetNextPlayerInfoRequest() => _allowAuxiliaryInput && !_overlayInputBlocked && _touchNextPlayerInfo;

        public bool GetRepeatPlayerInfoRequest() => _allowAuxiliaryInput && !_overlayInputBlocked && _touchRepeatPlayerInfo;

        public bool GetFlush() => !_overlayInputBlocked && IsKeyDown(_lastState, _kbFlush);

        public bool GetNextPanelRequest() => WasPressed(Key.Tab) && IsCtrlDown() && !IsShiftDown();

        public bool GetPreviousPanelRequest() => WasPressed(Key.Tab) && IsCtrlDown() && IsShiftDown();

        public bool GetOpenRadioMediaRequest() => WasPressed(Key.O);

        public bool GetOpenRadioFolderRequest() => WasPressed(Key.F);

        public bool GetToggleRadioPlaybackRequest() => WasPressed(Key.P);

        public bool GetRadioVolumeUpRequest() => WasPressed(Key.Up);

        public bool GetRadioVolumeDownRequest() => WasPressed(Key.Down);

        public bool GetRadioNextTrackRequest() => WasPressed(Key.PageDown);

        public bool GetRadioPreviousTrackRequest() => WasPressed(Key.PageUp);

        public bool GetRadioToggleShuffleRequest() => WasPressed(Key.S);

        public bool GetRadioToggleLoopRequest() => WasPressed(Key.L);

        private bool WasPressed(Key key)
        {
            if (_overlayInputBlocked)
                return false;
            if (IsInputTrappedByMenu(key))
                return false;
            return WasPressedRaw(key);
        }

        // While a menu or dialog is navigating during a race, the inputs it navigates with belong to
        // the menu, not to driving: the menu-navigation keys (arrows, Enter, etc.) and every reserved
        // key (the number row the pit menu selects with, and so on), plus the controller's d-pad /
        // stick / accept button. Any drive intent mapped onto one of those reads as not pressed, so it
        // cannot fire alongside the navigation. Every other key keeps working, so status reports and
        // the horn stay available while a menu is open.
        private bool IsInputTrappedByMenu(Key key)
        {
            if (!_menuNavigationActive)
                return false;
            return _menuNavigationKeys.Contains(key) || KeyMapManager.IsReservedKey(key);
        }

        private bool IsInputTrappedByMenu(AxisOrButton axis)
        {
            return _menuNavigationActive && _menuNavigationControllerInputs.Contains(axis);
        }

        // Edge detection without the overlay guard. Intent evaluation applies its own overlay policy
        // in EvaluateIntentTriggerCore (see IsAllowedDuringOverlay), so whitelisted press intents such
        // as the fuel/tire reports must use this directly; routing them through WasPressed would
        // double-block them whenever an overlay (e.g. the pit-stop menu) is open.
        private bool WasPressedRaw(Key key)
        {
            return IsKeyDown(_lastState, key) && !IsKeyDown(_prevState, key);
        }

        private DriveIntentState CaptureIntentState()
        {
            var triggered = new bool[Enum.GetValues(typeof(DriveIntent)).Length];
            foreach (var pair in _intentBindings)
                triggered[(int)pair.Key] = EvaluateIntentTrigger(pair.Key);

            var steering = ComputeSteering();
            var throttle = ComputeThrottle();
            var brake = ComputeBrake();
            var clutch = ComputeClutch();

            // Steering is derived from the left/right mappings.
            triggered[(int)DriveIntent.Steering] = steering != 0;

            return new DriveIntentState(steering, throttle, brake, clutch, triggered);
        }

        private bool EvaluateIntentTrigger(DriveIntent intent)
        {
            if (intent == DriveIntent.Horn)
                return EvaluateHornIntentTrigger();

            return EvaluateIntentTriggerCore(intent);
        }

        private bool EvaluateHornIntentTrigger()
        {
            if (EvaluateIntentTriggerCore(DriveIntent.Horn))
                return true;
            if (_touchHorn && _allowDrivingInput && (!_overlayInputBlocked || IsAllowedDuringOverlay(DriveIntent.Horn)))
                return true;
            if (!_pausedHornInputAllowed || _overlayInputBlocked)
                return false;

            var meta = GetIntentMeta(DriveIntent.Horn);
            return IsIntentActiveOnKeyboard(DriveIntent.Horn, meta)
                || IsIntentActiveOnController(DriveIntent.Horn, meta)
                || _touchHorn;
        }

        // Read-only status reports (and the horn) stay available even when an overlay such as the
        // pit-stop choice dialog is up, so the player can still query fuel, tires, lap, etc. and honk
        // while deciding. Driving and one-shot actions (pause, pit, start engine) remain blocked.
        private static bool IsAllowedDuringOverlay(DriveIntent intent)
        {
            switch (intent)
            {
                case DriveIntent.Horn:
                case DriveIntent.RequestInfo:
                case DriveIntent.CurrentGear:
                case DriveIntent.CurrentLapNr:
                case DriveIntent.CurrentRacePerc:
                case DriveIntent.CurrentLapPerc:
                case DriveIntent.CurrentRaceTime:
                case DriveIntent.ReportDistance:
                case DriveIntent.ReportSpeed:
                case DriveIntent.ReportFuel:
                case DriveIntent.ReportTireState:
                case DriveIntent.TrackName:
                    return true;
                default:
                    return false;
            }
        }

        // Keys currently bound to drive actions that stay live while an overlay (e.g. the pit-stop
        // menu) is open. The menu consults this so a key doing its drive job — a status report or the
        // horn — doesn't also trigger first-letter navigation and move the menu cursor. It is derived
        // from the live bindings, so it always matches whatever the player has mapped.
        public IReadOnlyCollection<Key> GetOverlayReservedKeys()
        {
            var keys = new HashSet<Key>();
            foreach (DriveIntent intent in Enum.GetValues(typeof(DriveIntent)))
            {
                if (!IsAllowedDuringOverlay(intent))
                    continue;
                var key = GetKeyMapping(intent);
                if (key != Key.Unknown)
                    keys.Add(key);
            }
            return keys;
        }

        private bool EvaluateIntentTriggerCore(DriveIntent intent)
        {
            if (_overlayInputBlocked && !IsAllowedDuringOverlay(intent))
                return false;

            var meta = GetIntentMeta(intent);
            if (!IsScopeEnabled(meta.Scope))
                return false;

            var keyboard = IsIntentActiveOnKeyboard(intent, meta);
            var controller = IsIntentActiveOnController(intent, meta);
            var touch = IsIntentActiveOnTouch(intent, meta);
            return keyboard || controller || touch;
        }

        private bool IsIntentActiveOnKeyboard(DriveIntent intent, DriveIntentMeta meta)
        {
            if (!UseKeyboard)
                return false;

            var key = GetKeyMapping(intent);
            if (key == Key.Unknown)
                return false;
            if (IsInputTrappedByMenu(key))
                return false;

            var active = meta.KeyboardMode == TriggerMode.Hold
                ? IsKeyDown(_lastState, key)
                : WasPressedRaw(key);
            if (!active && meta.AllowNumpadEnterAlias && key == Key.Return)
                active = WasPressedRaw(Key.NumberPadEnter);

            return active;
        }

        private bool IsIntentActiveOnController(DriveIntent intent, DriveIntentMeta meta)
        {
            if (!UseController)
                return false;

            var axis = GetAxisMapping(intent);
            if (axis == AxisOrButton.AxisNone)
                return false;
            if (IsInputTrappedByMenu(axis))
                return false;

            return meta.ControllerMode == TriggerMode.Hold
                ? GetAxis(axis) > 50
                : AxisPressed(axis);
        }

        private bool IsScopeEnabled(InputScope scope)
        {
            return scope switch
            {
                InputScope.Driving => _allowDrivingInput,
                InputScope.Auxiliary => _allowAuxiliaryInput,
                _ => false
            };
        }

        private bool IsIntentActiveOnTouch(DriveIntent intent, DriveIntentMeta meta)
        {
            if (!IsScopeEnabled(meta.Scope))
                return false;

            switch (intent)
            {
                case DriveIntent.SteerLeft:
                    return _touchSteering < 0;
                case DriveIntent.SteerRight:
                    return _touchSteering > 0;
                case DriveIntent.Throttle:
                    return _touchThrottle > 0;
                case DriveIntent.Brake:
                    return _touchBrake < 0;
                case DriveIntent.Clutch:
                    return _touchClutch > 0;
                case DriveIntent.GearUp:
                    return _touchGearUp;
                case DriveIntent.GearDown:
                    return _touchGearDown;
                case DriveIntent.StartEngine:
                    return _touchStartEngine;
                case DriveIntent.RequestInfo:
                    return _touchRequestInfo;
                case DriveIntent.ReportDistance:
                    return _touchReportDistance;
                case DriveIntent.ReportSpeed:
                    return _touchReportSpeed;
                case DriveIntent.ReportFuel:
                    return _touchReportFuel;
                case DriveIntent.ReportTireState:
                    return _touchReportTireState;
                case DriveIntent.CurrentGear:
                    return _touchCurrentGear;
                case DriveIntent.CurrentLapNr:
                    return _touchCurrentLapNr;
                case DriveIntent.CurrentRacePerc:
                    return _touchCurrentRacePerc;
                case DriveIntent.CurrentLapPerc:
                    return _touchCurrentLapPerc;
                case DriveIntent.CurrentRaceTime:
                    return _touchCurrentRaceTime;
                case DriveIntent.Pause:
                    return _touchPause;
                case DriveIntent.Pit:
                    return _touchPit;
                default:
                    return false;
            }
        }

        private DriveIntentMeta GetIntentMeta(DriveIntent intent)
        {
            if (_intentBindings.TryGetValue(intent, out var binding))
                return binding.Meta;

            return new DriveIntentMeta(InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press);
        }

        private static bool IsKeyDown(InputState state, Key key)
        {
            if (ModifierKeys.TryGetGroup(key, out var group) && ModifierKeys.IsBothKey(key))
            {
                return state.IsDown(ModifierKeys.GetLeftKey(group))
                    || state.IsDown(ModifierKeys.GetRightKey(group));
            }

            return state.IsDown(key);
        }
    }
}
