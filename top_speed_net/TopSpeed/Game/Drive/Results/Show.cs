using System;
using TopSpeed.Menu;
using TopSpeed.Drive;

namespace TopSpeed.Game
{
    internal sealed class ResultShow
    {
        private readonly Action<Dialog> _show;
        private readonly Action _playWin;
        private readonly ResultDialogs _dialogs;

        public ResultShow(Action<Dialog> show, Action playWin, ResultDialogs dialogs)
        {
            _show = show ?? throw new ArgumentNullException(nameof(show));
            _playWin = playWin ?? throw new ArgumentNullException(nameof(playWin));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        }

        public void Show(DriveResultSummary? summary, Action? onClosed = null)
        {
            if (summary == null)
            {
                onClosed?.Invoke();
                return;
            }

            var plan = _dialogs.Build(summary, onClosed);
            _show(plan.Dialog);
            if (plan.PlayWin)
                _playWin();
        }
    }
}



