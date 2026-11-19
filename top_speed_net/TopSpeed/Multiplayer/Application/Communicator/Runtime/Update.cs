using TopSpeed.Core.Multiplayer;
using TopSpeed.Runtime;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        public void Update(float _elapsed)
        {
            var session = _getSession();
            if (!ReferenceEquals(session, _boundSession))
                BindSession(session);

            if (session == null || !session.IsConnected)
            {
                ClearRemoteStreams();
                ClearRemoteMedia();
                PauseLocalMedia();
                ResetLocalMediaTransmissionState();
                Disarm();
                return;
            }

            var localAudibleFrequencyTenths = _multiplayer.CommunicatorEnabled
                ? _multiplayer.CommunicatorFrequencyTenths
                : (ushort)0;
            UpdateRemoteAudibility(localAudibleFrequencyTenths);
            UpdateRemoteMediaAudibility(localAudibleFrequencyTenths);
            CleanupTimedOutRemoteStreams();
            HandleCommunicatorMediaUpdate(session, localAudibleFrequencyTenths);

            if (!IsArmed())
            {
                Disarm();
                return;
            }

            var shouldTransmit = ShouldTransmit(out var pushToTalk);
            var hasMicrophonePermission = MicrophonePermissionRuntime.IsPermissionGranted();
            if (!hasMicrophonePermission && shouldTransmit)
                hasMicrophonePermission = MicrophonePermissionRuntime.EnsurePermissionGranted();

            if (!hasMicrophonePermission)
            {
                if (_transmitting)
                    EndTransmission();
                DiscardCapturedSamples();
                DisposeCapture();
                return;
            }

            // Arm: bring up the capture device the moment the communicator is enabled,
            // so VOX and PTT can transmit immediately instead of losing the first frames
            // to a cold-start of MiniAudio while the user is already talking.
            if (!EnsureCaptureInitialized())
            {
                Disarm();
                return;
            }

            var frequencyTenths = _multiplayer.CommunicatorFrequencyTenths;

            if (_transmitting &&
                (!shouldTransmit
                 || _activeFrequencyTenths != frequencyTenths
                 || _activePushToTalk != pushToTalk))
            {
                EndTransmission();
            }

            if (shouldTransmit && !_transmitting)
            {
                if (!BeginTransmission(session, frequencyTenths, pushToTalk))
                    return;
            }

            if (_transmitting)
                SendCapturedFrames(session);
            else
                DiscardCapturedSamples();
        }

        private bool IsArmed()
        {
            return _multiplayer.CommunicatorEnabled
                && _multiplayer.CommunicatorFrequencyTenths != 0;
        }

        // Single source of truth for "should we be transmitting right now?".
        // VOX mode: continuous transmission while voice activation is enabled. No
        // voice-activity detector — the user explicitly opted into open-mic.
        // PTT mode: transmit only while the configured push-to-talk shortcut is held.
        // The binding is fully remappable through menu shortcut mapping.
        private bool ShouldTransmit(out bool pushToTalk)
        {
            if (_multiplayer.CommunicatorVoiceActivationEnabled)
            {
                pushToTalk = false;
                return true;
            }

            pushToTalk = true;
            return _isShortcutHeld(CommunicatorShortcutIds.PushToTalk);
        }

        private uint NextStreamId()
        {
            var id = _nextStreamId++;
            if (id == 0)
                id = _nextStreamId++;

            return id;
        }

        private uint NextMediaId()
        {
            var id = _nextMediaId++;
            if (id == 0)
                id = _nextMediaId++;

            return id;
        }

        private bool WasShortcutPressed(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                return false;

            var isDown = _isShortcutHeld(actionId);
            if (!isDown)
            {
                _pressedShortcutActions.Remove(actionId);
                return false;
            }

            if (_pressedShortcutActions.Contains(actionId))
                return false;

            _pressedShortcutActions.Add(actionId);
            return true;
        }
    }
}
