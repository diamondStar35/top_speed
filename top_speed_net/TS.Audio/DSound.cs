using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public static class DSound
    {
        private static string rootDir = string.Empty;
        public static string SFileName = string.Empty;
        private static AudioSystem? system;
        private static AudioOutput? mainOutput;
        private static AudioOutput? musicOutput;
        private static AudioOutput? alwaysLoudOutput;
        private static AudioOutput? cutScenesOutput;

        private static Vector3 listenerPosition = Vector3.Zero;
        private static Vector3 listenerFront = new Vector3(0, 0, 1);
        private static Vector3 listenerTop = new Vector3(0, 1, 0);
        private static Vector3 listenerVelocity = Vector3.Zero;

        public static string SoundPath = string.Empty;
        public static string NSoundPath = string.Empty;
        public static string NumPath = string.Empty;

        public static AudioSystemConfig Config { get; } = new AudioSystemConfig();
        public static bool IsInitialized => system != null;
        public static bool IsHrtfActive => system != null && system.IsHrtfActive;

        public static void initialize(string root)
        {
            setRootDirectory(root);
            SoundPath = "s";
            NSoundPath = SoundPath + "\\n";
            NumPath = NSoundPath + "\\ns";

            system = new AudioSystem(Config);
            mainOutput = system.CreateOutput(new AudioOutputConfig { Name = "main" });
            musicOutput = system.CreateOutput(new AudioOutputConfig { Name = "music" });
            alwaysLoudOutput = system.CreateOutput(new AudioOutputConfig { Name = "alwaysLoud" });
            cutScenesOutput = system.CreateOutput(new AudioOutputConfig { Name = "cutScenes" });

            UpdateListenerAll();
        }

        public static void initialize(string root, string? hrtfSofaPath)
        {
            Config.HrtfSofaPath = hrtfSofaPath;
            initialize(root);
        }

        public static ExtendedAudioBuffer LoadSound(string FileName, AudioOutput device, bool notificationsSupport)
        {
            if (!File.Exists(FileName))
                throw new ArgumentException("The sound " + FileName + " could not be found.");

            if (device == null)
                device = mainOutput ?? throw new InvalidOperationException("Audio system not initialized.");

            var source = device.CreateSource(FileName, true);
            return new ExtendedAudioBuffer(source);
        }

        public static ExtendedAudioBuffer LoadSound(string FileName, object device, bool notificationsSupport)
        {
            return LoadSound(FileName, mainOutput ?? throw new InvalidOperationException("Audio system not initialized."), notificationsSupport);
        }

        public static ExtendedAudioBuffer LoadSound(string FileName)
        {
            return LoadSound(FileName, mainOutput ?? throw new InvalidOperationException("Audio system not initialized."), false);
        }

        public static ExtendedAudioBuffer LoadSound(string FileName, bool notificationsSupport)
        {
            return LoadSound(FileName, mainOutput ?? throw new InvalidOperationException("Audio system not initialized."), notificationsSupport);
        }

        public static ExtendedAudioBuffer LoadSoundAlwaysLoud(string FileName, bool notificationsSupport = false)
        {
            return LoadSound(FileName, alwaysLoudOutput ?? throw new InvalidOperationException("Audio system not initialized."), notificationsSupport);
        }

        public static ExtendedAudioBuffer LoadTone(byte[] tone)
        {
            if (tone == null || tone.Length == 0)
                throw new ArgumentException("Tone buffer is empty.");

            if (mainOutput == null)
                throw new InvalidOperationException("Audio system not initialized.");

            byte[] wavData = PcmWaveBuilder.BuildWavePcm16(tone, 44100, 1);
            var pinned = new PinnedAudioData(wavData);

            var sound = new MaSound();
            var result = sound.InitializeFromMemory(mainOutput.Engine, pinned.Pointer, (ulong)wavData.Length, 0, null!);
            if (result != MiniAudioEx.Native.ma_result.success)
            {
                pinned.Dispose();
                sound.Dispose();
                throw new InvalidOperationException("Failed to init tone: " + result);
            }

            var source = new AudioSourceHandle(mainOutput, sound, true, userData: pinned);
            if (Config.UseCurveDistanceScaler)
                source.ApplyCurveDistanceScaler(Config.CurveDistanceScaler);
            else
                source.SetDistanceModel(Config.DistanceModel, Config.MinDistance, Config.MaxDistance, Config.RollOff);
            source.SetDopplerFactor(Config.DopplerFactor);
            return new ExtendedAudioBuffer(source);
        }

        public static ExtendedAudioBuffer CreateProceduralSound(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100)
        {
            if (mainOutput == null)
                throw new InvalidOperationException("Audio system not initialized.");

            var source = mainOutput.CreateProceduralSource(callback, channels, sampleRate);
            return new ExtendedAudioBuffer(source);
        }

        public static void setListener()
        {
            listenerFront = new Vector3(0, 0, 1);
            listenerTop = new Vector3(0, 1, 0);
            listenerPosition = Vector3.Zero;
            listenerVelocity = Vector3.Zero;
            UpdateListenerAll();
        }

        public static Vector3 getListenerPosition()
        {
            return listenerPosition;
        }

        public static Vector3 getListenerOrientFront()
        {
            return listenerFront;
        }

        public static void setOrientation(float x1, float y1, float z1, float x2 = 0, float y2 = 1, float z2 = 0)
        {
            listenerFront = new Vector3(x1, y1, z1);
            listenerTop = new Vector3(x2, y2, z2);
            UpdateListenerAll();
        }

        public static void setVelocity(float x, float y, float z)
        {
            listenerVelocity = new Vector3(x, y, z);
            UpdateListenerAll();
        }

        public static void PlaySound(ExtendedAudioBuffer sound, bool stop, bool loop)
        {
            sound.play(stop, loop);
        }

        public static void PlaySound3d(ExtendedAudioBuffer sound, bool stop, bool loop, float x, float y, float z, float vx = 0, float vy = 0, float vz = 0, CalculateFlags flags = CalculateFlags.Matrix | CalculateFlags.Doppler | CalculateFlags.LpfDirect | CalculateFlags.LpfReverb, float curveDistanceScaler = 1.0f)
        {
            sound.SetPosition(new Vector3(x, y, z));
            sound.SetVelocity(new Vector3(vx, vy, vz));
            sound.ApplyCurveDistanceScaler(curveDistanceScaler);
            sound.play(stop, loop);
        }

        public static void SetCoordinates(float x, float y, float z)
        {
            listenerPosition = new Vector3(x, y, z);
            UpdateListenerAll();
        }

        public static OggBuffer loadOgg(AudioOutput device, params string[] fileNames)
        {
            if (device == null)
                device = cutScenesOutput ?? throw new InvalidOperationException("Audio system not initialized.");
            return new OggBuffer(device.CreateOggStream(fileNames));
        }

        public static OggBuffer loadOgg(object device, params string[] fileNames)
        {
            return loadOgg(cutScenesOutput ?? throw new InvalidOperationException("Audio system not initialized."), fileNames);
        }

        public static OggBuffer loadMusicFile(params string[] filenames)
        {
            return loadOgg(musicOutput ?? throw new InvalidOperationException("Audio system not initialized."), filenames);
        }

        public static OggBuffer loadOgg(params string[] filenames)
        {
            return loadOgg(cutScenesOutput ?? throw new InvalidOperationException("Audio system not initialized."), filenames);
        }

        public static void unloadSound(ref ExtendedAudioBuffer sound)
        {
            if (sound == null)
                return;

            sound.stop();
            sound.Dispose();
            sound = null!;
        }

        public static bool isPlaying(ExtendedAudioBuffer s)
        {
            return s.state == ExtendedAudioBuffer.State.playing;
        }

        public static void playAndWait(string fn)
        {
            ExtendedAudioBuffer s = LoadSound(fn);
            PlaySound(s, true, false);
            while (isPlaying(s))
                Thread.Sleep(100);
            s.Dispose();
            s = null!;
        }

        public static void cleanUp()
        {
            system?.Dispose();
            system = null;
            mainOutput = null;
            musicOutput = null;
            alwaysLoudOutput = null;
            cutScenesOutput = null;
        }

        public static void setRootDirectory(string root)
        {
            rootDir = root;
        }

        public static void setPan(ExtendedAudioBuffer sound, float pan)
        {
            sound.SetPan(pan);
        }

        public static void setVolumeOfMusic(float v)
        {
            if (musicOutput == null)
                return;
            if (v < 0.0f) v = 0.0f;
            if (v > 1.0f) v = 1.0f;
            musicOutput.Engine.SetVolume(v);
        }

        public static float getVolumeOfMusic()
        {
            if (musicOutput == null)
                return 0f;
            return musicOutput.Engine.GetVolume();
        }

        public static void setVolumeOfSounds(float v)
        {
            if (mainOutput == null)
                return;
            mainOutput.Engine.SetVolume(v);
        }

        public static void update()
        {
            system?.Update();
        }

        private static void UpdateListenerAll()
        {
            system?.UpdateListenerAll(listenerPosition, listenerFront, listenerTop, listenerVelocity);
        }
    }

    internal sealed class PinnedAudioData : IDisposable
    {
        private GCHandle _handle;
        public IntPtr Pointer => _handle.AddrOfPinnedObject();

        public PinnedAudioData(byte[] data)
        {
            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
                _handle.Free();
        }
    }

    internal static class PcmWaveBuilder
    {
        public static byte[] BuildWavePcm16(byte[] pcmData, int sampleRate, short channels)
        {
            int bytesPerSample = 2;
            int blockAlign = channels * bytesPerSample;
            int byteRate = sampleRate * blockAlign;
            int dataSize = pcmData.Length;
            int fileSize = 36 + dataSize;

            byte[] header = new byte[44];
            WriteAscii(header, 0, "RIFF");
            WriteInt32(header, 4, fileSize);
            WriteAscii(header, 8, "WAVE");
            WriteAscii(header, 12, "fmt ");
            WriteInt32(header, 16, 16);
            WriteInt16(header, 20, 1);
            WriteInt16(header, 22, channels);
            WriteInt32(header, 24, sampleRate);
            WriteInt32(header, 28, byteRate);
            WriteInt16(header, 32, (short)blockAlign);
            WriteInt16(header, 34, 16);
            WriteAscii(header, 36, "data");
            WriteInt32(header, 40, dataSize);

            byte[] wav = new byte[44 + dataSize];
            Buffer.BlockCopy(header, 0, wav, 0, 44);
            Buffer.BlockCopy(pcmData, 0, wav, 44, dataSize);
            return wav;
        }

        private static void WriteAscii(byte[] buffer, int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
                buffer[offset + i] = (byte)text[i];
        }

        private static void WriteInt16(byte[] buffer, int offset, short value)
        {
            buffer[offset] = (byte)(value & 0xff);
            buffer[offset + 1] = (byte)((value >> 8) & 0xff);
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xff);
            buffer[offset + 1] = (byte)((value >> 8) & 0xff);
            buffer[offset + 2] = (byte)((value >> 16) & 0xff);
            buffer[offset + 3] = (byte)((value >> 24) & 0xff);
        }
    }
}
