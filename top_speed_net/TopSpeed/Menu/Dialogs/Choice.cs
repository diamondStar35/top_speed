using System;
using System.Collections.Generic;

using TopSpeed.Input;
using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    [Flags]
    internal enum ChoiceDialogFlags
    {
        None = 0,
        Cancelable = 1
    }

    internal readonly struct ChoiceDialogResult
    {
        public ChoiceDialogResult(bool isCanceled, int choiceId)
        {
            IsCanceled = isCanceled;
            ChoiceId = choiceId;
        }

        public bool IsCanceled { get; }
        public int ChoiceId { get; }

        public static ChoiceDialogResult Canceled() => new ChoiceDialogResult(true, QuestionId.Cancel);
        public static ChoiceDialogResult Selected(int choiceId) => new ChoiceDialogResult(false, choiceId);
    }

    internal sealed class ChoiceDialog
    {
        public ChoiceDialog(
            string title,
            string? caption,
            IReadOnlyDictionary<int, string> items,
            Action<ChoiceDialogResult>? onResult,
            ChoiceDialogFlags flags = ChoiceDialogFlags.None,
            string? cancelLabel = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Choice dialog title is required.", nameof(title));
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (items.Count == 0)
                throw new ArgumentException("Choice dialog must contain at least one item.", nameof(items));

            Title = title.Trim();
            Caption = string.IsNullOrWhiteSpace(caption) ? string.Empty : (caption ?? string.Empty).Trim();
            Items = items;
            Flags = flags;
            CancelLabel = string.IsNullOrWhiteSpace(cancelLabel)
                ? LocalizationService.Mark("Cancel")
                : (cancelLabel ?? string.Empty).Trim();
            OnResult = onResult;
        }

        public string Title { get; }
        public string Caption { get; }
        public IReadOnlyDictionary<int, string> Items { get; }
        public ChoiceDialogFlags Flags { get; }
        public string CancelLabel { get; }
        public Action<ChoiceDialogResult>? OnResult { get; }
        public bool OpenAsOverlay { get; set; }

        // Keys that should not drive first-letter navigation while this dialog is open, because they
        // are bound to actions still live under the overlay (e.g. drive status/horn keys during a
        // pit stop). Null or empty leaves letter navigation fully enabled.
        public IReadOnlyCollection<InputKey>? LetterNavReservedKeys { get; set; }

        public bool IsCancelable => (Flags & ChoiceDialogFlags.Cancelable) != 0;
    }

    internal sealed class ChoiceDialogManager
    {
        private const string MenuId = "choice_dialog";
        private static readonly string NotCancelableMessage =
            LocalizationService.Mark("This dialog is not cancelable. You need to choose an option.");

        private readonly MenuManager _menu;
        private readonly Action<string> _speak;
        private ChoiceDialog? _activeDialog;

        public ChoiceDialogManager(MenuManager menu, Action<string> speak)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
            _menu.Register(_menu.CreateMenu(MenuId, new[] { new MenuItem(LocalizationService.Mark("Choice"), MenuAction.None) }, string.Empty));
            _menu.SetClose(MenuId, HandleClose);
        }

        public bool IsChoiceMenu(string? currentMenuId)
        {
            return string.Equals(currentMenuId, MenuId, StringComparison.Ordinal);
        }

        public bool HasActiveChoiceDialog => _activeDialog != null;

        public void Show(ChoiceDialog dialog)
        {
            if (dialog == null)
                throw new ArgumentNullException(nameof(dialog));

            _activeDialog = dialog;
            ApplyLetterNavigationReservation(dialog);
            var items = new List<MenuItem>
            {
                new MenuItem(dialog.Title, MenuAction.None)
            };

            if (!string.IsNullOrWhiteSpace(dialog.Caption))
                items.Add(new MenuItem(dialog.Caption, MenuAction.None));

            var firstChoiceIndex = items.Count;
            var choiceIds = new List<int>(dialog.Items.Keys);
            choiceIds.Sort();
            for (var i = 0; i < choiceIds.Count; i++)
            {
                var choiceId = choiceIds[i];
                if (!dialog.Items.TryGetValue(choiceId, out var rawText))
                    continue;
                var choiceText = rawText ?? string.Empty;
                items.Add(new MenuItem(choiceText, MenuAction.None, onActivate: () => Complete(dialog, ChoiceDialogResult.Selected(choiceId))));
            }

            if (dialog.IsCancelable)
            {
                items.Add(new MenuItem(dialog.CancelLabel, MenuAction.None, onActivate: () => Complete(dialog, ChoiceDialogResult.Canceled())));
            }

            _menu.UpdateItems(MenuId, items);
            var announcement = DialogAnnouncement.Compose(dialog.Title, dialog.Caption);
            _menu.Push(MenuId, announcement, firstChoiceIndex);
        }

        private bool HandleClose(CloseEvent _)
        {
            if (_activeDialog == null)
                return false;

            if (_activeDialog.IsCancelable)
            {
                Complete(_activeDialog, ChoiceDialogResult.Canceled());
                return true;
            }

            _speak(NotCancelableMessage);
            return true;
        }

        private void Complete(ChoiceDialog dialog, ChoiceDialogResult result)
        {
            if (!ReferenceEquals(_activeDialog, dialog))
                return;

            _activeDialog = null;
            _menu.SetLetterNavigationReservation(null);

            if (IsChoiceMenu(_menu.CurrentId) && _menu.CanPop)
                _menu.PopToPrevious();

            dialog.OnResult?.Invoke(result);
        }

        private void ApplyLetterNavigationReservation(ChoiceDialog dialog)
        {
            if (dialog.LetterNavReservedKeys is { Count: > 0 } reservedKeys)
            {
                var reserved = new HashSet<InputKey>(reservedKeys);
                _menu.SetLetterNavigationReservation(reserved.Contains);
            }
            else
            {
                _menu.SetLetterNavigationReservation(null);
            }
        }
    }
}




