using TS.Audio;

namespace TopSpeed.Audio
{
    internal interface IAudioSources
    {
        SoundAsset LoadAsset(string path, bool streamFromDisk = true);
        Source CreateSource(SoundAsset asset, string busName, bool? useHrtf = null);
        Source CreateLoopingSource(SoundAsset asset, string busName, bool? useHrtf = null);
        Source CreateSpatialSource(SoundAsset asset, string busName, bool? allowHrtf = null);
        Source CreateLoopingSpatialSource(SoundAsset asset, string busName, bool? allowHrtf = null);
    }
}
