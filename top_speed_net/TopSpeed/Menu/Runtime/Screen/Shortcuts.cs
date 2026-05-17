using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        private bool TryHandleNumberActivation(IInputService input, out MenuUpdateResult result)
        {
            result = MenuUpdateResult.None;
            if (_items.Count == 0)
                return false;

            if (!MenuInputUtil.TryGetPressedDigit(input, out var digit))
                return false;

            var count = 0;
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.IsHidden || (item.OnActivate == null && item.NextMenuId == null && item.Action == MenuAction.None))
                    continue;

                count++;
                if (count != digit)
                    continue;

                _activeActionIndex = NoSelection;
                MoveToIndex(i);
                result = HandleActivation();
                return true;
            }

            return false;
        }

        private bool TryHandleLetterNavigation(in MenuLetterPress letterPress)
        {
            if (!letterPress.HasLetter || letterPress.ReservedByShortcut)
                return false;
            if (_items.Count == 0)
                return false;

            var letter = letterPress.Letter;
            var start = _index == NoSelection ? 0 : (_index + 1) % _items.Count;
            for (var i = 0; i < _items.Count; i++)
            {
                var idx = (start + i) % _items.Count;
                if (!MenuInputUtil.ItemStartsWithLetter(_items[idx], letter))
                    continue;

                _activeActionIndex = NoSelection;
                MoveToIndex(idx);
                return true;
            }

            return false;
        }
    }
}
