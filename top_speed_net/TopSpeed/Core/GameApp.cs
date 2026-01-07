using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using TopSpeed.Windowing;

namespace TopSpeed.Core
{
    internal sealed class GameApp : IDisposable
    {
        private const int FrameDelayMs = 5;
        private const uint TimerResolutionMs = 1;
        private readonly GameWindow _window;
        private Game? _game;
        private readonly Stopwatch _stopwatch;
        private long _lastTicks;
        private bool _timerResolutionSet;

        public GameApp()
        {
            _window = new GameWindow();
            _window.FormClosed += OnFormClosed;
            _window.Load += OnLoad;
            _stopwatch = new Stopwatch();
        }

        public void Run()
        {
            TryEnableHighResolutionTimer();
            Application.Idle += OnIdle;
            Application.Run(_window);
            Application.Idle -= OnIdle;
            DisableHighResolutionTimer();
        }

        private void OnLoad(object? sender, EventArgs e)
        {
            _game = new Game(_window);
            _game.ExitRequested += async () =>
            {
                _game.FadeOutMenuMusic(500);
                await Task.Delay(500).ConfigureAwait(true);
                _window.Close();
            };
            _game.Initialize();
            _stopwatch.Start();
            _lastTicks = _stopwatch.ElapsedTicks;
        }

        private void OnIdle(object? sender, EventArgs e)
        {
            while (AppStillIdle)
            {
                if (_game == null)
                    return;
                if (_game.IsModalInputActive)
                    return;

                var now = _stopwatch.ElapsedTicks;
                var deltaSeconds = (float)(now - _lastTicks) / Stopwatch.Frequency;
                _lastTicks = now;
                _game.Update(deltaSeconds);
                Thread.Sleep(FrameDelayMs);
            }
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            _game?.Dispose();
            _game = null;
            DisableHighResolutionTimer();
        }

        public void Dispose()
        {
            _window.Dispose();
            _game?.Dispose();
            DisableHighResolutionTimer();
        }

        private static bool AppStillIdle
        {
            get
            {
                return !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Handle;
            public uint Msg;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public System.Drawing.Point Point;
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);

        private void TryEnableHighResolutionTimer()
        {
            if (_timerResolutionSet)
                return;
            if (timeBeginPeriod(TimerResolutionMs) == 0)
                _timerResolutionSet = true;
        }

        private void DisableHighResolutionTimer()
        {
            if (!_timerResolutionSet)
                return;
            timeEndPeriod(TimerResolutionMs);
            _timerResolutionSet = false;
        }
    }
}
