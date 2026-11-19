using System;
using System.Threading;
using System.Threading.Tasks;

namespace TopSpeed.Network.Session
{
    internal sealed class Loop : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Task _pollTask;
        private readonly Task _keepAliveTask;
        private readonly Action _drain;
        private int _disposed;

        public Loop(Action poll, Action drain, Action keepAliveSend)
        {
            _drain = drain ?? throw new ArgumentNullException(nameof(drain));
            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoop(poll, _cts.Token));
            _keepAliveTask = Task.Run(() => KeepAliveLoop(keepAliveSend, _cts.Token));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            WaitForStop(_pollTask);
            WaitForStop(_keepAliveTask);
            try
            {
                _cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void PollLoop(Action poll, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                poll();
                _drain();
                Thread.Sleep(1);
            }

            _drain();
        }

        private static async Task KeepAliveLoop(Action keepAliveSend, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                keepAliveSend();
                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private static void WaitForStop(Task task)
        {
            try
            {
                task.Wait(250);
            }
            catch (AggregateException ex) when (IsCancellationOnly(ex))
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static bool IsCancellationOnly(AggregateException ex)
        {
            if (ex == null)
                return false;

            var list = ex.Flatten().InnerExceptions;
            if (list.Count == 0)
                return false;
            for (var i = 0; i < list.Count; i++)
            {
                if (!(list[i] is TaskCanceledException) && !(list[i] is OperationCanceledException))
                    return false;
            }

            return true;
        }
    }
}

