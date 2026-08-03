using System;
using System.Runtime.ExceptionServices;
using Eto.Forms;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing.Eto
{
    /// <summary>
    /// Runs screen reader operations on the UI thread. libprism's macOS backends
    /// marshal their work onto the AppKit main queue with a blocking dispatch when
    /// called from any other thread, so speech driven from a dedicated worker thread
    /// deadlocks whenever the main thread is itself waiting on that worker (as it is
    /// during startup). Invoking on the UI thread lets prism run inline instead.
    /// </summary>
    internal sealed class UiThreadSpeechDispatcher : ISpeechThreadDispatcher
    {
        public T Invoke<T>(Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var application = Application.Instance;
            if (application == null)
                return action();

            T result = default!;
            Exception? error = null;
            application.Invoke(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

            if (error != null)
                ExceptionDispatchInfo.Capture(error).Throw();

            return result;
        }
    }
}
