using System;
using System.Numerics;
using System.Threading;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Structs;
using TopSpeed.Audio;
using TopSpeed.Runtime;
using TS.Audio;

namespace TopSpeed.Tests;

internal sealed class AudioRenderingHarness : IAudioSources, IDisposable
{
    private readonly MiniAudioEngine _backend;
    private readonly AssetLibrary _assets = new();
    private readonly SoundAsset _tone;
    private readonly AudioBus _vehicles;
    private readonly AudioBus _track;
    private readonly AudioBus _world;
    private readonly BalanceMeter _vehicleMeter;
    private readonly BalanceMeter _trackMeter;
    private readonly bool _useRecordings;

    public AudioOutput Output { get; }

    public AudioRenderingHarness(bool useHrtf, bool useRecordings = false)
    {
        _useRecordings = useRecordings;
        NativeLibraryBootstrap.Initialize();
        _backend = new MiniAudioEngine(new[] { MiniAudioBackend.Null });
        Output = new AudioOutput(_backend,
            new AudioOutputConfig { Name = "surface-test", Channels = 2, SampleRate = 44100 },
            new AudioSystemConfig { UseHrtf = useHrtf }, new AudioDiagnostics());
        Output.SetMasterVolume(0f);
        Output.UpdateListener(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, Vector3.Zero);
        _vehicles = Output.CreateBus(AudioEngineOptions.VehiclesBusName);
        _track = Output.CreateBus(AudioEngineOptions.TrackBusName);
        _world = Output.CreateBus(AudioEngineOptions.WorldBusName);
        _vehicleMeter = new BalanceMeter(Output.SoundFlowFormat);
        _trackMeter = new BalanceMeter(Output.SoundFlowFormat);
        _vehicles.Mixer.AddAnalyzer(_vehicleMeter);
        _track.Mixer.AddAnalyzer(_trackMeter);
        // Identical input isolates positional processing from each recording's stereo balance.
        _tone = _assets.CreateProcedural(FillTone, channels: 2, sampleRate: 44100);
    }

    public SoundAsset LoadAsset(string path, bool streamFromDisk = true) =>
        _useRecordings ? _assets.LoadClip(path, streamFromDisk) : _tone;

    public Source CreateSource(SoundAsset asset, string busName, bool? useHrtf = null) =>
        GetBus(busName).CreateSource(asset, spatialize: false, useHrtf: useHrtf);

    public Source CreateLoopingSource(SoundAsset asset, string busName, bool? useHrtf = null) =>
        CreateSource(asset, busName, useHrtf);

    public Source CreateSpatialSource(SoundAsset asset, string busName, bool? allowHrtf = null) =>
        GetBus(busName).CreateSpatialSource(asset, allowHrtf);

    public Source CreateLoopingSpatialSource(SoundAsset asset, string busName, bool? allowHrtf = null) =>
        CreateSpatialSource(asset, busName, allowHrtf);

    public float MeasureVehicleBalance() => Measure(_vehicleMeter);
    public float MeasureSurfaceBalance() => Measure(_trackMeter);
    public int MeasureSurfaceDelayFrames()
    {
        Measure(_trackMeter);
        return _trackMeter.ReadDelayFrames();
    }

    public void Dispose()
    {
        Output.Dispose();
        _tone.Dispose();
        _assets.Dispose();
        _backend.Dispose();
    }

    private AudioBus GetBus(string name) => name switch
    {
        AudioEngineOptions.VehiclesBusName => _vehicles,
        AudioEngineOptions.TrackBusName => _track,
        AudioEngineOptions.WorldBusName => _world,
        _ => throw new ArgumentException("Unexpected vehicle audio bus.", nameof(name))
    };

    private float Measure(BalanceMeter meter)
    {
        WaitForBlocks(meter, 8);
        meter.Reset();
        // Integrate the direction cue over time instead of one short noise buffer.
        WaitForBlocks(meter, 16);
        return meter.ReadBalance();
    }

    private void WaitForBlocks(BalanceMeter meter, int count)
    {
        var before = meter.Blocks;
        var rendered = SpinWait.SpinUntil(() =>
        {
            Output.Update(0.005);
            Thread.Sleep(1);
            return meter.Blocks >= before + count;
        }, 3000);
        rendered.Should().BeTrue("the silent backend should render audio");
    }

    private static void FillTone(float[] buffer, int frames, int channels, ref ulong frameIndex)
    {
        for (var frame = 0; frame < frames; frame++, frameIndex++)
        {
            var sample = 0.1f * (float)Math.Sin(2.0 * Math.PI * 440.0 * frameIndex / 44100.0);
            for (var channel = 0; channel < channels; channel++)
                buffer[frame * channels + channel] = sample;
        }
    }

    private sealed class BalanceMeter : AudioAnalyzer
    {
        private readonly object _sync = new();
        private double _left;
        private double _right;
        private int _blocks;
        private readonly float[] _leftSamples = new float[8192];
        private readonly float[] _rightSamples = new float[8192];
        private int _sampleCount;

        public int Blocks => Volatile.Read(ref _blocks);

        public BalanceMeter(AudioFormat format) : base(format) { }

        public void Reset()
        {
            lock (_sync)
            {
                _left = 0;
                _right = 0;
                _sampleCount = 0;
            }
        }

        public float ReadBalance()
        {
            lock (_sync)
            {
                (_left + _right).Should().BeGreaterThan(0, "the source should remain audible");
                return (float)((_right - _left) / (_right + _left));
            }
        }

        public int ReadDelayFrames()
        {
            lock (_sync)
            {
                var bestCorrelation = double.NegativeInfinity;
                var bestDelay = 0;
                for (var delay = -44; delay <= 44; delay++)
                {
                    double correlation = 0, leftEnergy = 0, rightEnergy = 0;
                    for (var i = 44; i < _sampleCount - 44; i++)
                    {
                        var left = _leftSamples[i];
                        var right = _rightSamples[i + delay];
                        correlation += left * right;
                        leftEnergy += left * left;
                        rightEnergy += right * right;
                    }
                    correlation /= Math.Sqrt(Math.Max(1e-30, leftEnergy * rightEnergy));
                    if (correlation > bestCorrelation)
                    {
                        bestCorrelation = correlation;
                        bestDelay = delay;
                    }
                }
                return bestDelay;
            }
        }

        protected override void Analyze(ReadOnlySpan<float> buffer, int channels)
        {
            double left = 0, right = 0;
            for (var i = 0; i + 1 < buffer.Length; i += channels)
            {
                left += buffer[i] * buffer[i];
                right += buffer[i + 1] * buffer[i + 1];
            }
            lock (_sync)
            {
                _left += left;
                _right += right;
                for (var i = 0; i + 1 < buffer.Length && _sampleCount < _leftSamples.Length; i += channels)
                {
                    _leftSamples[_sampleCount] = buffer[i];
                    _rightSamples[_sampleCount] = buffer[i + 1];
                    _sampleCount++;
                }
            }
            Interlocked.Increment(ref _blocks);
        }
    }
}
