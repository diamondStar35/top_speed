using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        /// <summary>Matches the local car's engine loop fade (Car fields: EngineShutdownFadeSeconds).</summary>
        private const float EngineShutdownFadeSeconds = 0.12f;

        /// <summary>
        /// Begins the same engine shutdown the player's own car runs.
        /// <para>
        /// <see cref="Car.Stop"/> puts the car into its stopping state, where
        /// the engine keeps being synced with combustion off so it winds down on its real friction
        /// and inertia until it dies, and only then is the loop faded out. Other cars never ran that
        /// sequence - their engine loop was simply stopped - so a finishing rival went from a full
        /// racing note to silence in one frame.
        /// </para>
        /// </summary>
        private void BeginEngineShutdown(float seedSpeedKph)
        {
            if (_engineShutdownActive || !_soundEngine.IsPlaying)
                return;

            SeedEngineForShutdown(seedSpeedKph);
            _engineShutdownActive = true;
        }

        private void CancelEngineShutdown()
        {
            _engineShutdownActive = false;
        }

        /// <summary>
        /// Seeds the audio engine model from a speed the car was actually doing.
        /// <para>
        /// Networked cars never advance an engine model - their pitch arrives from the server - so
        /// their rpm is stale. Without seeding, the shutdown would start from that stale value and
        /// the note would jump before winding down.
        /// </para>
        /// </summary>
        private void SeedEngineForShutdown(float speedKph)
        {
            var driveRatioOverride = _effectiveDriveRatio > 0f ? _effectiveDriveRatio : (float?)null;
            var rpm = Calculator.RpmAtSpeed(
                _physicsConfig.Powertrain,
                Math.Max(0f, speedKph) / 3.6f,
                _gear,
                driveRatioOverride);
            _engine.OverrideRpm(rpm);
        }

        /// <summary>
        /// Advances an in-progress shutdown. Driven every frame by both hosts: locally simulated
        /// cars from <see cref="Run(float, float, float, TopSpeed.Bots.BotVehicleObservation[])"/>,
        /// networked cars from <see cref="UpdateRemoteAudio"/>.
        /// </summary>
        private void AdvanceEngineShutdown(float elapsed)
        {
            if (!_engineShutdownActive)
                return;

            // Exactly what Car.RunStoppingDynamics does: keep syncing the engine with combustion
            // off, which drops its minimum operational rpm to zero and lets it wind down and die.
            SyncEngineFromMotion(elapsed, throttleInput: 0, combustionEnabled: false);
            UpdateEngineFreq();

            // Matches Car.UpdateEngineRotationState's "stopped" threshold.
            if (_engine.Rpm > 1f)
                return;

            CancelEngineShutdown();
            _state = ComputerState.Stopped;
            if (_soundEngine.IsPlaying)
                _soundEngine.Stop(EngineShutdownFadeSeconds);
        }
    }
}
