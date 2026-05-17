using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Input;
using TS.Audio;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void PlayConnectedSound()
        {
            PlayNetworkSound("connected.ogg");
        }

        private void StartConnectingPulse()
        {
            StopConnectingPulse();
            var sound = GetNetworkSound(ref _state.Audio.ConnectingSound, "connecting.ogg");
            if (sound == null)
                return;

            var token = _lifetime.BeginConnectingPulse();
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _audio.PlayOneShot(sound, AudioEngineOptions.UiBusName, configure: handle =>
                        {
                            handle.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                        });
                    }
                    catch
                    {
                    }

                    try
                    {
                        await Task.Delay(ConnectingPulseIntervalMs, token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        private void StopConnectingPulse()
        {
            _lifetime.StopConnectingPulse();
        }

        private void PlayNetworkSound(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            SoundAsset? handle;
            if (string.Equals(fileName, "online.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.OnlineSound, fileName);
            else if (string.Equals(fileName, "offline.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.OfflineSound, fileName);
            else if (string.Equals(fileName, "connecting.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.ConnectingSound, fileName);
            else if (string.Equals(fileName, "ping_start.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.PingStartSound, fileName);
            else if (string.Equals(fileName, "ping.ogg", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(fileName, "ping_stop.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.PingSound, fileName);
            else if (string.Equals(fileName, "room_created.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.RoomCreatedSound, fileName);
            else if (string.Equals(fileName, "room_join.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.RoomJoinSound, fileName);
            else if (string.Equals(fileName, "room_leave.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.RoomLeaveSound, fileName);
            else if (string.Equals(fileName, "chat.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.ChatSound, fileName);
            else if (string.Equals(fileName, "room_chat.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.RoomChatSound, fileName);
            else if (string.Equals(fileName, "buffer_switch.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.BufferSwitchSound, fileName);
            else if (string.Equals(fileName, "connected.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _state.Audio.ConnectedSound, fileName);
            else
                handle = null;

            if (handle == null)
                return;

            try
            {
                _audio.PlayOneShot(handle, AudioEngineOptions.UiBusName, configure: sound =>
                {
                    sound.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                });
            }
            catch
            {
            }
        }

        private void PlayCommunicatorFrequencyAdjustSound()
        {
            var sound = GetNetworkSound(ref _state.Audio.CommunicatorFrequencyAdjustSound, "comm/freq_adjust.wav");
            if (sound == null)
                return;

            try
            {
                _audio.PlayOneShot(sound, AudioEngineOptions.UiBusName, configure: handle =>
                {
                    handle.SetVolumePercent(_settings, AudioVolumeCategory.Communicator, 100);
                });
            }
            catch
            {
            }
        }

        private SoundAsset? GetNetworkSound(ref SoundAsset? cache, string fileName)
        {
            if (cache != null)
                return cache;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", fileName ?? string.Empty);
            if (!_audio.TryResolvePath(path, out var fullPath))
                return null;

            try
            {
                cache = _audio.LoadAsset(fullPath, streamFromDisk: false);
                return cache;
            }
            catch
            {
                return null;
            }
        }
    }
}



