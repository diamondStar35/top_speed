using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer.Chat
{
    internal enum HistoryBuffer
    {
        All = 0,
        GlobalChat = 1,
        RoomChat = 2,
        Connections = 3,
        RoomEvents = 4
    }

    internal readonly struct HistoryMoveResult
    {
        public HistoryMoveResult(string text, bool moved, bool wrapped, bool edgeReached)
        {
            Text = text ?? string.Empty;
            Moved = moved;
            Wrapped = wrapped;
            EdgeReached = edgeReached;
        }

        public string Text { get; }
        public bool Moved { get; }
        public bool Wrapped { get; }
        public bool EdgeReached { get; }
    }

    internal sealed class HistoryBuffers
    {
        private readonly Dictionary<HistoryBuffer, List<string>> _items = new Dictionary<HistoryBuffer, List<string>>();
        private readonly Dictionary<HistoryBuffer, int> _focusIndex = new Dictionary<HistoryBuffer, int>();
        private static readonly HistoryBuffer[] Order =
        {
            HistoryBuffer.All,
            HistoryBuffer.GlobalChat,
            HistoryBuffer.RoomChat,
            HistoryBuffer.Connections,
            HistoryBuffer.RoomEvents
        };
        private readonly int _maxEntries;
        private static readonly IReadOnlyList<string> EmptyItems = System.Array.Empty<string>();

        public HistoryBuffers(int maxEntries)
        {
            _maxEntries = maxEntries > 0 ? maxEntries : 100;
            for (var i = 0; i < Order.Length; i++)
            {
                _items[Order[i]] = new List<string>();
                _focusIndex[Order[i]] = -1;
            }
        }

        public HistoryBuffer Current { get; private set; } = HistoryBuffer.All;

        public void AddGlobalChat(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.GlobalChat, text);
        }

        public void AddRoomChat(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.RoomChat, text);
        }

        public void AddConnection(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.Connections, text);
        }

        public void AddRoomEvent(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.RoomEvents, text);
        }

        public IReadOnlyList<string> GetCurrentEntries()
        {
            var items = _items[Current];
            return items.Count > 0 ? items : EmptyItems;
        }

        public HistoryMoveResult MoveToNext(bool wrapNavigation)
        {
            return MoveCategory(1, wrapNavigation);
        }

        public HistoryMoveResult MoveToPrevious(bool wrapNavigation)
        {
            return MoveCategory(-1, wrapNavigation);
        }

        public string GetCurrentFocusedItemText()
        {
            var items = _items[Current];
            if (items.Count == 0)
                return NoMessagesYetText();

            var idx = GetNormalizedCurrentFocusIndex(items.Count);
            _focusIndex[Current] = idx;
            return items[idx];
        }

        public HistoryMoveResult MoveCurrentItem(int delta, bool wrapNavigation)
        {
            var items = _items[Current];
            if (items.Count == 0)
            {
                return new HistoryMoveResult(
                    NoMessagesYetText(),
                    moved: false,
                    wrapped: false,
                    edgeReached: false);
            }

            var idx = GetNormalizedCurrentFocusIndex(items.Count);
            var nextIndex = idx;
            var moved = false;
            var wrapped = false;
            var edgeReached = false;

            if (delta != 0)
            {
                if (wrapNavigation)
                {
                    var raw = idx + delta;
                    wrapped = raw < 0 || raw >= items.Count;
                    nextIndex = (raw % items.Count + items.Count) % items.Count;
                    moved = nextIndex != idx;
                }
                else
                {
                    var candidate = idx + delta;
                    if (candidate < 0 || candidate >= items.Count)
                    {
                        edgeReached = true;
                    }
                    else
                    {
                        nextIndex = candidate;
                        moved = nextIndex != idx;
                    }
                }
            }

            _focusIndex[Current] = nextIndex;
            return new HistoryMoveResult(
                items[nextIndex],
                moved,
                wrapped,
                edgeReached);
        }

        public HistoryMoveResult MoveCurrentItemToFirst()
        {
            var items = _items[Current];
            if (items.Count == 0)
            {
                return new HistoryMoveResult(
                    NoMessagesYetText(),
                    moved: false,
                    wrapped: false,
                    edgeReached: false);
            }

            var idx = GetNormalizedCurrentFocusIndex(items.Count);
            var nextIndex = 0;
            var moved = nextIndex != idx;
            _focusIndex[Current] = nextIndex;
            return new HistoryMoveResult(
                items[nextIndex],
                moved,
                wrapped: false,
                edgeReached: !moved);
        }

        public HistoryMoveResult MoveCurrentItemToLast()
        {
            var items = _items[Current];
            if (items.Count == 0)
            {
                return new HistoryMoveResult(
                    NoMessagesYetText(),
                    moved: false,
                    wrapped: false,
                    edgeReached: false);
            }

            var idx = GetNormalizedCurrentFocusIndex(items.Count);
            var nextIndex = items.Count - 1;
            var moved = nextIndex != idx;
            _focusIndex[Current] = nextIndex;
            return new HistoryMoveResult(
                items[nextIndex],
                moved,
                wrapped: false,
                edgeReached: !moved);
        }

        public string CategoryLabel()
        {
            return Current switch
            {
                HistoryBuffer.GlobalChat => LocalizationService.Mark("global chat"),
                HistoryBuffer.RoomChat => LocalizationService.Mark("room chat"),
                HistoryBuffer.Connections => LocalizationService.Mark("connections"),
                HistoryBuffer.RoomEvents => LocalizationService.Mark("room events"),
                _ => LocalizationService.Mark("all")
            };
        }

        public void Clear()
        {
            foreach (var entry in _items)
                entry.Value.Clear();
            for (var i = 0; i < Order.Length; i++)
                _focusIndex[Order[i]] = -1;
            Current = HistoryBuffer.All;
        }

        private void AddTo(HistoryBuffer buffer, string text)
        {
            var line = Normalize(text);
            if (line.Length == 0)
                return;

            var items = _items[buffer];
            items.Add(line);
            while (items.Count > _maxEntries)
                items.RemoveAt(0);
        }

        private static string Normalize(string text)
        {
            return (text ?? string.Empty).Trim();
        }

        private int FindCurrentIndex()
        {
            for (var i = 0; i < Order.Length; i++)
            {
                if (Order[i] == Current)
                    return i;
            }

            return 0;
        }

        private HistoryMoveResult MoveCategory(int delta, bool wrapNavigation)
        {
            var currentIndex = FindCurrentIndex();
            var nextIndex = currentIndex;
            var moved = false;
            var wrapped = false;
            var edgeReached = false;
            if (delta != 0)
            {
                if (wrapNavigation)
                {
                    var raw = currentIndex + delta;
                    wrapped = raw < 0 || raw >= Order.Length;
                    nextIndex = (raw % Order.Length + Order.Length) % Order.Length;
                    moved = nextIndex != currentIndex;
                }
                else
                {
                    var candidate = currentIndex + delta;
                    if (candidate < 0 || candidate >= Order.Length)
                    {
                        edgeReached = true;
                    }
                    else
                    {
                        nextIndex = candidate;
                        moved = nextIndex != currentIndex;
                    }
                }
            }

            Current = Order[nextIndex];
            if (moved)
                SetCurrentFocusToLatest();

            return new HistoryMoveResult(
                CategoryLabel(),
                moved,
                wrapped,
                edgeReached);
        }

        private int GetNormalizedCurrentFocusIndex(int itemCount)
        {
            if (!_focusIndex.TryGetValue(Current, out var idx))
                idx = -1;

            if (idx < 0 || idx >= itemCount)
                idx = itemCount - 1;

            return idx;
        }

        private void SetCurrentFocusToLatest()
        {
            _focusIndex[Current] = _items[Current].Count - 1;
        }

        private static string NoMessagesYetText()
        {
            return LocalizationService.Translate(LocalizationService.Mark("No messages yet."));
        }
    }
}




