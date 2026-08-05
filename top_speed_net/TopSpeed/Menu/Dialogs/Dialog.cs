using System;
using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    [Flags]
    internal enum DialogButtonFlags
    {
        None = 0,
        Default = 1
    }

    internal sealed class DialogButton
    {
        public DialogButton(int id, string text, Action? onClick = null, DialogButtonFlags flags = DialogButtonFlags.None)
        {
            Id = id;
            Text = text ?? string.Empty;
            OnClick = onClick;
            Flags = flags;
        }

        public int Id { get; }
        public string Text { get; }
        public Action? OnClick { get; }
        public DialogButtonFlags Flags { get; }
    }

    internal sealed class DialogItem
    {
        public DialogItem(string text)
        {
            Text = text ?? string.Empty;
        }

        public string Text { get; }
    }

    internal sealed class Dialog
    {
        public Dialog(string title, IEnumerable<DialogItem>? items, Action<int>? onResult, params DialogButton[] buttons)
            : this(title, null, QuestionId.Close, items, onResult, buttons)
        {
        }

        public Dialog(string title, string? caption, int closeResultId, IEnumerable<DialogItem>? items, Action<int>? onResult, params DialogButton[] buttons)
        {
            Title = title ?? string.Empty;
            Caption = caption ?? string.Empty;
            CloseResultId = closeResultId;
            OnResult = onResult;
            Items = items == null ? Array.Empty<DialogItem>() : new List<DialogItem>(items);
            Buttons = buttons ?? Array.Empty<DialogButton>();
        }

        public string Title { get; }
        public string Caption { get; }
        public int CloseResultId { get; }
        public Action<int>? OnResult { get; }
        public bool OpenAsOverlay { get; set; }
        public bool IsCancelable { get; set; } = true;
        public bool FocusFirstButtonByDefault { get; set; }
        public IReadOnlyList<DialogItem> Items { get; }
        public IReadOnlyList<DialogButton> Buttons { get; }
    }

    internal sealed class DialogManager
    {
        private const string MenuId = "dialog";
        private static readonly string NotCancelableMessage =
            LocalizationService.Mark("This dialog is not cancelable. You need to choose an option.");
        private readonly MenuManager _menu;
        private readonly Action<string> _speak;
        private Dialog? _activeDialog;

        public DialogManager(MenuManager menu, Action<string> speak)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
            _menu.Register(_menu.CreateMenu(MenuId, new[] { new MenuItem(LocalizationService.Mark("Dialog"), MenuAction.None) }, string.Empty));
            _menu.SetClose(MenuId, HandleDialogClose);
        }

        public bool IsDialogMenu(string? currentMenuId)
        {
            return string.Equals(currentMenuId, MenuId, StringComparison.Ordinal);
        }

        public bool HasActiveOverlayDialog => _activeDialog != null && _activeDialog.OpenAsOverlay;

        public void CloseActive()
        {
            if (_activeDialog == null)
                return;

            Complete(_activeDialog, _activeDialog.CloseResultId, null);
        }

        public void Show(Dialog dialog)
        {
            if (dialog == null)
                throw new ArgumentNullException(nameof(dialog));

            var updateInPlace = _activeDialog != null && IsDialogMenu(_menu.CurrentId);
            _activeDialog = dialog;
            var items = BuildItems(dialog, out var defaultIndex);
            _menu.UpdateItems(MenuId, items, preserveSelection: updateInPlace);
            if (updateInPlace)
                return;

            var announcement = DialogAnnouncement.Compose(dialog.Title, dialog.Caption, dialog.Items);
            var autoFocus = defaultIndex >= 0;
            _menu.Push(MenuId, announcement, autoFocus ? defaultIndex : null, autoFocus: autoFocus);
        }

        private List<MenuItem> BuildItems(Dialog dialog, out int defaultIndex)
        {
            var items = new List<MenuItem>
            {
                new MenuItem(dialog.Title, MenuAction.None)
            };

            if (!string.IsNullOrWhiteSpace(dialog.Caption))
                items.Add(new MenuItem(dialog.Caption, MenuAction.None));

            for (var i = 0; i < dialog.Items.Count; i++)
            {
                var item = dialog.Items[i];
                items.Add(new MenuItem(item.Text, MenuAction.None));
            }

            var firstDialogItemIndex = string.IsNullOrWhiteSpace(dialog.Caption) ? 1 : 2;
            var firstButtonIndex = items.Count;
            defaultIndex = dialog.FocusFirstButtonByDefault && dialog.Buttons.Count > 0
                ? firstButtonIndex
                : -1;
            var firstDefaultFound = false;
            for (var i = 0; i < dialog.Buttons.Count; i++)
            {
                var button = dialog.Buttons[i];
                if (!firstDefaultFound && (button.Flags & DialogButtonFlags.Default) != 0)
                {
                    defaultIndex = firstButtonIndex + i;
                    firstDefaultFound = true;
                }

                var buttonCopy = button;
                items.Add(new MenuItem(button.Text, MenuAction.None, onActivate: () => Complete(dialog, buttonCopy.Id, buttonCopy.OnClick)));
            }

            if (dialog.Buttons.Count == 0)
                defaultIndex = 0;
            return items;
        }

        private bool HandleDialogClose(CloseEvent _)
        {
            if (_activeDialog == null)
                return false;

            if (!_activeDialog.IsCancelable)
            {
                _speak(NotCancelableMessage);
                return true;
            }

            Complete(_activeDialog, _activeDialog.CloseResultId, null);
            return true;
        }

        private void Complete(Dialog dialog, int resultId, Action? buttonAction)
        {
            if (!ReferenceEquals(_activeDialog, dialog))
                return;

            _activeDialog = null;

            if (IsDialogMenu(_menu.CurrentId) && _menu.CanPop)
                _menu.PopToPrevious();

            dialog.OnResult?.Invoke(resultId);
            buttonAction?.Invoke();
        }
    }
}




