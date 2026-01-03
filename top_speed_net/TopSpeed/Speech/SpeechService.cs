using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows.Forms;

namespace TopSpeed.Speech
{
    internal sealed class SpeechService : IDisposable
    {
        public enum SpeakFlag
        {
            None,
            NoInterrupt,
            NoInterruptButStop,
            Interruptable,
            InterruptableButStop
        }

        private readonly Stopwatch _watch = new Stopwatch();
        private readonly NvdaClient _nvda;
        private SpeechSynthesizer? _sapi;
        private long _timeRequiredMs;
        private string _lastSpoken = string.Empty;
        private Func<bool>? _isInputHeld;

        public SpeechService(Func<bool>? isInputHeld = null)
        {
            _isInputHeld = isInputHeld;
            _nvda = new NvdaClient();
        }

        public bool IsAvailable => _nvda.IsAvailable || _sapi != null;

        public float ScreenReaderRateMs { get; set; }

        public void BindInputProbe(Func<bool> isInputHeld)
        {
            _isInputHeld = isInputHeld;
        }

        public void Speak(string text)
        {
            Speak(text, SpeakFlag.None);
        }

        public void Speak(string text, SpeakFlag flag)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (flag == SpeakFlag.NoInterruptButStop || flag == SpeakFlag.InterruptableButStop)
                Purge();

            text = text.Trim();
            _lastSpoken = text;

            var spoke = false;
            if (_nvda.IsAvailable)
            {
                spoke = _nvda.Speak(text);
                if (spoke)
                    StartSpeakTimer(text);
            }

            if (!spoke)
            {
                EnsureSapi();
                _sapi!.SpeakAsync(text);
                while (!IsSpeaking())
                {
                    PumpMessages();
                    Thread.Sleep(0);
                }
            }

            PumpMessages();

            if (flag == SpeakFlag.None)
                return;

            if (flag == SpeakFlag.Interruptable || flag == SpeakFlag.InterruptableButStop)
            {
                while (IsInputHeld())
                {
                    if (!IsSpeaking())
                        break;
                    PumpMessages();
                    Thread.Sleep(0);
                }
            }

            while (IsSpeaking())
            {
                if ((flag == SpeakFlag.Interruptable || flag == SpeakFlag.InterruptableButStop) && IsInputHeld())
                    break;
                PumpMessages();
                Thread.Sleep(10);
            }
        }

        public bool IsSpeaking()
        {
            if (_watch.IsRunning)
                return _watch.ElapsedMilliseconds < _timeRequiredMs;
            return _sapi != null && _sapi.State == SynthesizerState.Speaking;
        }

        public void Purge()
        {
            _watch.Reset();
            _timeRequiredMs = 0;
            if (_sapi != null)
            {
                try
                {
                    _sapi.SpeakAsyncCancelAll();
                }
                catch (OperationCanceledException)
                {
                }
                while (IsSpeaking())
                {
                    PumpMessages();
                    Thread.Sleep(0);
                }
            }
            _nvda.Cancel();
        }

        public void Dispose()
        {
            Purge();
            _sapi?.Dispose();
            _nvda.Dispose();
        }

        private void EnsureSapi()
        {
            if (_sapi == null)
                _sapi = new SpeechSynthesizer();
        }

        private void StartSpeakTimer(string text)
        {
            if (ScreenReaderRateMs <= 0f)
            {
                _watch.Reset();
                _timeRequiredMs = 0;
                return;
            }

            var words = CountWords(text);
            _timeRequiredMs = (long)(words * ScreenReaderRateMs);
            _watch.Reset();
            _watch.Start();
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private bool IsInputHeld()
        {
            try
            {
                return _isInputHeld != null && _isInputHeld();
            }
            catch
            {
                return false;
            }
        }

        private static void PumpMessages()
        {
            if (Application.MessageLoop)
                Application.DoEvents();
        }

        private sealed class NvdaClient : IDisposable
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
            private delegate int NvdaSpeak([MarshalAs(UnmanagedType.LPWStr)] string text);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate int NvdaCancel();

            private IntPtr _module;
            private NvdaSpeak? _speak;
            private NvdaCancel? _cancel;

            public bool IsAvailable => _speak != null;

            public NvdaClient()
            {
                var names = Environment.Is64BitProcess
                    ? new[] { "nvda_client_64.dll", "nvdaControllerClient64.dll" }
                    : new[] { "nvda_client_32.dll", "nvdaControllerClient32.dll" };

                foreach (var name in names)
                {
                    _module = LoadLibrary(name);
                    if (_module != IntPtr.Zero)
                        break;
                }

                if (_module == IntPtr.Zero)
                    return;

                _speak = GetProc<NvdaSpeak>("nvdaController_speakText");
                _cancel = GetProc<NvdaCancel>("nvdaController_cancelSpeech");
                if (_speak == null)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }
            }

            public bool Speak(string text)
            {
                if (_speak == null)
                    return false;
                try
                {
                    return _speak(text) == 0;
                }
                catch
                {
                    return false;
                }
            }

            public void Cancel()
            {
                try
                {
                    _cancel?.Invoke();
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                if (_module != IntPtr.Zero)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }
            }

            private T? GetProc<T>(string name) where T : class
            {
                if (_module == IntPtr.Zero)
                    return null;
                var proc = GetProcAddress(_module, name);
                if (proc == IntPtr.Zero)
                    return null;
                return Marshal.GetDelegateForFunctionPointer(proc, typeof(T)) as T;
            }

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32", SetLastError = true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", SetLastError = true)]
            private static extern bool FreeLibrary(IntPtr hModule);
        }
    }
}
