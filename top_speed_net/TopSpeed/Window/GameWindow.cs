using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace TopSpeed.Windowing
{
    internal sealed class GameWindow : Form
    {
        private const int WmSysCommand = 0x0112;
        private const int ScKeyMenu = 0xF100;
        private readonly TextBox _inputBox;
        private bool _submitPending;
        private bool _cancelPending;
        private string _submittedText = string.Empty;

        public GameWindow()
        {
            Text = "Top Speed";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(640, 360);
            KeyPreview = true;

            _inputBox = new TextBox
            {
                Visible = false,
                AcceptsReturn = true,
                CausesValidation = false,
                ImeMode = ImeMode.NoControl,
                TabStop = false,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 400,
                Left = 12,
                Top = ClientSize.Height - 48,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _inputBox.KeyDown += OnInputKeyDown;
            Controls.Add(_inputBox);
        }

        public void ShowTextInput(string? initialText)
        {
            _submittedText = string.Empty;
            _submitPending = false;
            _cancelPending = false;
            RunOnUiThread(() =>
            {
                _inputBox.Text = initialText ?? string.Empty;
                _inputBox.Visible = true;
                _inputBox.Focus();
            });
        }

        public void HideTextInput()
        {
            RunOnUiThread(() =>
            {
                _inputBox.Visible = false;
                _inputBox.Clear();
                Focus();
            });
        }

        public bool TryConsumeTextInput(out TextInputResult result)
        {
            if (_submitPending)
            {
                _submitPending = false;
                result = TextInputResult.Submitted(_submittedText);
                return true;
            }
            if (_cancelPending)
            {
                _cancelPending = false;
                result = TextInputResult.CreateCancelled();
                return true;
            }
            result = default;
            return false;
        }

        public TextInputResult ReceiveTextInput(string? initialText, bool password = false)
        {
            _submittedText = string.Empty;
            _submitPending = false;
            _cancelPending = false;
            RunOnUiThread(() => _inputBox.PasswordChar = password ? '*' : '\0');
            ShowTextInput(initialText);
            Application.DoEvents();
            while (!_submitPending && !_cancelPending)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            RunOnUiThread(() => _inputBox.PasswordChar = '\0');
            if (_cancelPending)
            {
                _cancelPending = false;
                return TextInputResult.CreateCancelled();
            }
            _submitPending = false;
            return TextInputResult.Submitted(_submittedText);
        }

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _submittedText = _inputBox.Text;
                _submitPending = true;
                HideTextInput();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _cancelPending = true;
                HideTextInput();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void RunOnUiThread(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
                return;
            }

            action();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmSysCommand && ((int)m.WParam & 0xFFF0) == ScKeyMenu)
            {
                return;
            }

            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F10)
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    internal readonly struct TextInputResult
    {
        private TextInputResult(bool cancelled, string text)
        {
            Cancelled = cancelled;
            Text = text;
        }

        public bool Cancelled { get; }
        public string Text { get; }

        public static TextInputResult Submitted(string text) => new TextInputResult(false, text ?? string.Empty);

        public static TextInputResult CreateCancelled() => new TextInputResult(true, string.Empty);
    }
}
