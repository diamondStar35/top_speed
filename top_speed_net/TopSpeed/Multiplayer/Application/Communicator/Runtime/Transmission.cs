using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private bool BeginTransmission(Network.MultiplayerSession session, ushort frequencyTenths, bool pushToTalk)
        {
            var streamId = NextStreamId();
            if (!session.SendVoiceStart(streamId, _encoder.Profile, frequencyTenths, pushToTalk))
                return false;

            _encoder.Reset();
            _transmitting = true;
            _streamId = streamId;
            _activeFrequencyTenths = frequencyTenths;
            _activePushToTalk = pushToTalk;
            _timestampMs = 0;
            // Don't replay whatever ambient noise was captured while the runtime was
            // idle; this lets remote players hear the speaker from the first frame.
            DiscardCapturedSamples();
            UpdateMicCue(open: true);
            return true;
        }

        private void SendCapturedFrames(Network.MultiplayerSession session)
        {
            while (TryReadCapturedFrame(_captureFrame))
            {
                if (!_encoder.TryEncode(_captureFrame, _timestampMs, out var frame))
                {
                    // Encoder failures are isolated to a single 20 ms window; advancing the
                    // timestamp keeps the receiver's playout clock aligned so subsequent
                    // frames don't appear to be in the past.
                    _timestampMs = unchecked(_timestampMs + (uint)ProtocolConstants.VoiceFrameMs);
                    continue;
                }

                if (!session.SendVoiceFrame(_streamId, in frame))
                {
                    EndTransmission();
                    return;
                }

                _timestampMs = unchecked(_timestampMs + (uint)ProtocolConstants.VoiceFrameMs);
            }
        }

        private void EndTransmission()
        {
            if (!_transmitting)
                return;

            var session = _boundSession;
            var streamId = _streamId;
            _transmitting = false;
            _streamId = 0;
            _activeFrequencyTenths = 0;
            _activePushToTalk = false;
            _timestampMs = 0;
            _encoder.Reset();
            session?.SendVoiceStop(streamId);
            UpdateMicCue(open: false);
        }

        // "Disarm" tears the runtime back down to the disabled-communicator state:
        // stop transmitting, drop any captured samples, and release the mic so the
        // OS doesn't keep the capture device open when the user has turned the
        // communicator off (or the session has disconnected).
        private void Disarm()
        {
            EndTransmission();
            DiscardCapturedSamples();
            DisposeCapture();
        }
    }
}
