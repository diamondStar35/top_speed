//#define NO_PROCESSING

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.EXT;
using SteamAudio;

namespace Tests
{
	public static unsafe class Program
	{
		public const int UpdateFrequency = 120;

		public const int SamplingRate = 44100;
		public const int AudioFrameSize = 4096; // The higher this is - the less glitchly but also more delayed will the playback be.
		public const int AudioFrameSizeInBytes = AudioFrameSize * sizeof(float);
		public const bool SoftwareAL = true;

		//OpenAL
		private static Device* alAudioDevice;
		private static Context* alAudioContext;
		private static AL al;
		private static ALContext alc;
		//Steam Audio
		private static IPL.Context iplContext;
		private static IPL.Hrtf iplHrtf;
		private static IPL.BinauralEffect iplBinauralEffect;
		private static IPL.AudioBuffer iplInputBuffer;
		private static IPL.AudioBuffer iplOutputBuffer;
		private static IPL.AudioSettings iplAudioSettings;

		private static unsafe void Main(string[] args)
		{
			Console.WriteLine($"Working Directory: '{Path.GetFullPath(".")}'.");
			Console.WriteLine($"Launch Arguments: '{string.Join(' ', args)}'.");

			IntPtr tempInterlacingBuffer = IntPtr.Zero;

			try {
				var stopwatch = new Stopwatch();
				byte[] bytes = LoadRawAudio(string.Join(' ', args));
				using var audioStream = new MemoryStream(bytes);

				PrepareOpenAL();
				PrepareSteamAudio();

				const int NumBuffers = 2;

				uint* alBuffers = stackalloc uint[NumBuffers];

				al.GenBuffers(NumBuffers, alBuffers);

				uint sourceId = al.GenSource();

				stopwatch.Start();

				// SteamAudio uses non-interlaced audio buffers, while OpenAL wants interlaced ones. This "array" is used for conversions.
				tempInterlacingBuffer = Marshal.AllocHGlobal(AudioFrameSizeInBytes * 2);

				void StreamBuffer(uint bufferId, Vector3 position)
				{
					float* inputBufferChannelPtr = ((float**)iplInputBuffer.Data)[0];
					var inputBufferByteSpan = new Span<byte>(inputBufferChannelPtr, AudioFrameSizeInBytes);
					//var inputBufferFloatSpan = new Span<float>(inputBufferChannelPtr, AudioFrameSize);
					int bytesRead = audioStream.Read(inputBufferByteSpan);

					// Loop the audio on stream end.
					if (bytesRead < AudioFrameSizeInBytes) {
						audioStream.Position = 0;

						audioStream.Read(inputBufferByteSpan.Slice(0, AudioFrameSizeInBytes - bytesRead));
					}

#if !NO_PROCESSING
					var binauralEffectParams = new IPL.BinauralEffectParams {
						Hrtf = iplHrtf,
						Direction = new IPL.Vector3(position.X, position.Y, position.Z),
						Interpolation = IPL.HrtfInterpolation.Nearest,
						SpatialBlend = 1f,
					};

					IPL.BinauralEffectApply(iplBinauralEffect, ref binauralEffectParams, ref iplInputBuffer, ref iplOutputBuffer);

					IPL.AudioBufferInterleave(iplContext, in iplOutputBuffer, in Unsafe.AsRef<float>((void*)tempInterlacingBuffer));

					al.BufferData(bufferId, (BufferFormat)FloatBufferFormat.Stereo, (void*)tempInterlacingBuffer, AudioFrameSizeInBytes * 2, iplAudioSettings.SamplingRate);
#else
					al.BufferData(bufferId, (BufferFormat)FloatBufferFormat.Mono, (void*)inputBufferChannelPtr, AudioFrameSizeInBytes, iplAudioSettings.SamplingRate);
#endif

					CheckALErrors();
				}

				Console.WriteLine();

				int cursorPosX = Console.CursorLeft;
				int cursorPosY = Console.CursorTop;

				Console.CursorVisible = false;

				while (true) {
					var position = GetRotatingAudioPosition((float)stopwatch.Elapsed.TotalSeconds);

					// Display information

					Console.SetCursorPosition(cursorPosX, cursorPosY);
					Console.WriteLine($"Sound position: {position: 0.00;-0.00; 0.00}");

					TimeSpan streamPositionTimeSpan = TimeSpan.FromSeconds((int)(audioStream.Position / sizeof(float) / SamplingRate));
					TimeSpan streamLengthTimeSpan = TimeSpan.FromSeconds((int)(audioStream.Length / sizeof(float) / SamplingRate));

					Console.WriteLine($"Stream position: {streamPositionTimeSpan.Minutes:D2}:{streamPositionTimeSpan.Seconds:D2} / {streamLengthTimeSpan.Minutes:D2}:{streamLengthTimeSpan.Seconds:D2}");

					// Update streamed audio

					al.GetSourceProperty(sourceId, GetSourceInteger.BuffersProcessed, out int numProcessedBuffers);
					al.GetSourceProperty(sourceId, GetSourceInteger.BuffersQueued, out int numQueuedBuffers);

					int buffersToAdd = NumBuffers - numQueuedBuffers + numProcessedBuffers;

					while (buffersToAdd > 0) {
						uint bufferId = alBuffers[buffersToAdd - 1];

						if (numProcessedBuffers > 0) {
							al.SourceUnqueueBuffers(sourceId, 1, &bufferId);

							numProcessedBuffers--;
						}

						StreamBuffer(bufferId, position);

						al.SourceQueueBuffers(sourceId, 1, &bufferId);
						CheckALErrors();

						buffersToAdd--;
					}

					// Start playback whenever it stops
					al.GetSourceProperty(sourceId, GetSourceInteger.SourceState, out int sourceStateInt);

					if ((SourceState)sourceStateInt != SourceState.Playing) {
						al.SourcePlay(sourceId);
					}

					CheckALErrors();

					// Sleep to not stress the CPU.
					Thread.Sleep(1);
				}
			}
			catch (Exception e) {
				Console.WriteLine($"{e.GetType().Name}: {e.Message}");
				Console.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}

			try {
				if (tempInterlacingBuffer != IntPtr.Zero) {
					Marshal.FreeHGlobal(tempInterlacingBuffer);
				}

				UnloadSteamAudio();
				UnloadOpenAL();
			}
			catch { }
		}

		internal static byte[] LoadRawAudio(string rawAudioPath)
		{
			if (string.IsNullOrWhiteSpace(rawAudioPath)) {
				throw new ArgumentException($"Provide a path to a raw audio file in command line arguments to play it.");
			}

			if (!File.Exists(rawAudioPath)) {
				throw new ArgumentException($"Invalid audio path: '{rawAudioPath}'.");
			}

			return File.ReadAllBytes(rawAudioPath);
		}

		internal static Vector3 GetRotatingAudioPosition(float time)
		{
			Quaternion rotation = Quaternion.Identity;

			rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, time * 1 / 3f);
			rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, time * 1 / 2f);
			rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, time);

			return Vector3.Transform(new Vector3(1f, 0f, 0f), rotation);
		}

		// OpenAL

		private static void PrepareOpenAL()
		{
			al = AL.GetApi(soft: SoftwareAL);
			alc = ALContext.GetApi(soft: SoftwareAL);

			alAudioDevice = alc.OpenDevice(null);
			alAudioContext = alc.CreateContext(alAudioDevice, null);

			if (!alc.MakeContextCurrent(alAudioContext)) {
				throw new InvalidOperationException("Unable to make context current");
			}

			Console.WriteLine("Created OpenAL context.");

			// Require float32 support.
			const string Float32Extension = "AL_EXT_float32";

			if (!al.IsExtensionPresent(Float32Extension)) {
				string extensions = al.GetStateProperty(StateString.Extensions);

				throw new Exception($"This program requires '{Float32Extension}' OpenAL extension to function.\r\nAvailable extensions: {extensions}");
			}

			CheckALErrors();

			Console.WriteLine("OpenAL is ready.");
		}

		private static void UnloadOpenAL()
		{
			alc.DestroyContext(alAudioContext);
			alc.CloseDevice(alAudioDevice);
		}

		private static void CheckALErrors()
		{
			var error = al.GetError();

			if (error != AudioError.NoError) {
				throw new Exception($"OpenAL Error: {error}");
			}
		}

		// SteamAudio

		private static void PrepareSteamAudio()
		{
			// Steam Audio Initialization

			var contextSettings = new IPL.ContextSettings {
				Version = IPL.Version,
			};

			IPL.ContextCreate(in contextSettings, out iplContext);

			Console.WriteLine("Created SteamAudio context.");

			iplAudioSettings = new IPL.AudioSettings {
				SamplingRate = SamplingRate,
				FrameSize = AudioFrameSize
			};

			// HRTF

			var hrtfSettings = new IPL.HrtfSettings {
				Type = IPL.HrtfType.Default,
				Volume = 1.0f,
				NormType = IPL.HrtfNormType.None
			};

			IPL.HrtfCreate(iplContext, in iplAudioSettings, in hrtfSettings, out iplHrtf);

			// Binaural Effect

			var binauralEffectSettings = new IPL.BinauralEffectSettings {
				Hrtf = iplHrtf
			};

			IPL.BinauralEffectCreate(iplContext, in iplAudioSettings, in binauralEffectSettings, out iplBinauralEffect);

			// Audio Buffers

			// Input is mono, output is stereo.
			IPL.AudioBufferAllocate(iplContext, 1, iplAudioSettings.FrameSize, ref iplInputBuffer);
			IPL.AudioBufferAllocate(iplContext, 2, iplAudioSettings.FrameSize, ref iplOutputBuffer);

			// Celebrate!

			Console.WriteLine("SteamAudio is ready.");
		}

		private static void UnloadSteamAudio()
		{
			IPL.AudioBufferFree(iplContext, ref iplInputBuffer);
			IPL.AudioBufferFree(iplContext, ref iplOutputBuffer);
			IPL.BinauralEffectRelease(ref iplBinauralEffect);
			IPL.HrtfRelease(ref iplHrtf);
			IPL.ContextRelease(ref iplContext);
		}
	}
}
