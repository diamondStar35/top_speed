using System;
using System.Collections.Generic;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public void ResetSelection(int? preferredSelectionIndex = null, bool autoFocus = true)
        {
            SaveSelectionForActiveView();
            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = preferredSelectionIndex;
            _suppressAutoFocus = !autoFocus;
            _justEntered = true;
            if (autoFocus)
                QueueAutoFocusFirstItem();
            else
                ClearAutoFocusPending();
            CancelHint();
        }

        public void ReplaceItems(IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            if (_viewIndex != 0)
            {
                PrimaryView.ReplaceItems(items);
                if (PrimaryView.KeepSelection && PrimaryView.SavedSelection >= 0)
                    PrimaryView.SavedSelection = Math.Max(0, Math.Min(PrimaryView.SavedSelection, PrimaryView.Items.Count - 1));
                return;
            }

            var previousIndex = _index;
            var hadSelection = previousIndex != NoSelection;
            SaveSelectionForActiveView();

            PrimaryView.ReplaceItems(items);
            LoadActiveViewItems();
            CancelHint();

            if (preserveSelection && hadSelection && _items.Count > 0)
            {
                _index = Math.Max(0, Math.Min(previousIndex, _items.Count - 1));
                SaveSelectionForActiveView();
                _activeActionIndex = NoSelection;
                _pendingFocusIndex = null;
                _justEntered = false;
                ClearAutoFocusPending();
                return;
            }

            if (ActiveView.KeepSelection && ActiveView.SavedSelection != NoSelection && _items.Count > 0)
            {
                _index = Math.Max(0, Math.Min(ActiveView.SavedSelection, _items.Count - 1));
                SaveSelectionForActiveView();
                _activeActionIndex = NoSelection;
                _pendingFocusIndex = null;
                _justEntered = false;
                ClearAutoFocusPending();
                return;
            }

            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = null;
            _justEntered = true;
            QueueAutoFocusFirstItem();
        }

        public bool SwitchToNextScreen()
        {
            return SwitchScreen(+1);
        }

        public bool SwitchToPreviousScreen()
        {
            return SwitchScreen(-1);
        }

        private void HandleNavigation(UpdateInputState state)
        {
            if (_index == NoSelection)
            {
                if (state.MoveDown)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                    ClearAutoFocusPending();
                }
                else if (state.MoveUp)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                    ClearAutoFocusPending();
                }
                else if (state.MoveHome)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                    ClearAutoFocusPending();
                }
                else if (state.MoveEnd)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                    ClearAutoFocusPending();
                }

                return;
            }

            if (state.MoveUp)
            {
                _activeActionIndex = NoSelection;
                MoveSelectionAndAnnounce(-1);
            }
            else if (state.MoveDown)
            {
                _activeActionIndex = NoSelection;
                MoveSelectionAndAnnounce(1);
            }
            else if (state.MoveHome)
            {
                _activeActionIndex = NoSelection;
                MoveToIndex(0);
            }
            else if (state.MoveEnd)
            {
                _activeActionIndex = NoSelection;
                MoveToIndex(_items.Count - 1);
            }
        }

        private void MoveSelectionAndAnnounce(int delta)
        {
            var moved = MoveSelection(delta, out var wrapped, out var edgeReached);
            if (moved)
            {
                if (wrapped)
                {
                    PlayNavigateSound();
                    PlaySfx(_wrapSound);
                }
                else
                {
                    PlayNavigateSound();
                }
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
            }
            else if (wrapped)
            {
                PlaySfx(_wrapSound);
            }
            else if (edgeReached)
            {
                PlaySfx(_edgeSound);
            }
        }

        private void MoveToIndex(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _items.Count)
                return;
            if (_index == NoSelection)
            {
                _index = targetIndex;
                SaveSelectionForActiveView();
                PlayNavigateSound();
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
                return;
            }
            if (targetIndex == _index)
            {
                PlaySfx(WrapNavigation ? _wrapSound : _edgeSound);
                return;
            }
            _index = targetIndex;
            SaveSelectionForActiveView();
            PlayNavigateSound();
            AnnounceCurrent(!_justEntered);
            _justEntered = false;
        }

        private bool MoveSelection(int delta, out bool wrapped, out bool edgeReached)
        {
            wrapped = false;
            edgeReached = false;
            if (_items.Count == 0)
                return false;
            if (_index == NoSelection)
            {
                _index = delta >= 0 ? 0 : _items.Count - 1;
                return true;
            }
            var previous = _index;
            if (WrapNavigation)
            {
                var next = _index + delta;
                if (next < 0 || next >= _items.Count)
                    wrapped = true;
                _index = (next + _items.Count) % _items.Count;
                SaveSelectionForActiveView();
                return _index != previous;
            }

            var nextIndex = _index + delta;
            if (nextIndex < 0 || nextIndex >= _items.Count)
            {
                edgeReached = true;
                return false;
            }
            _index = nextIndex;
            SaveSelectionForActiveView();
            return _index != previous;
        }

        private void FocusFirstItem()
        {
            if (_items.Count == 0)
                return;
            _index = TakeFirstFocusIndex();
            SaveSelectionForActiveView();
            _activeActionIndex = NoSelection;
            PlayNavigateSound();
            AnnounceCurrent(purge: false, SpeechService.SpeakFlag.NoInterrupt);
            _justEntered = false;
        }

        // Focuses the first item but folds its text into the screen's opening announcement, so both are
        // spoken as a single utterance. See AnnounceTitle for why that matters.
        private bool TryFocusFirstItemWithOpening(string opening)
        {
            if (_items.Count == 0)
                return false;

            var targetIndex = TakeFirstFocusIndex();
            _index = targetIndex;
            SaveSelectionForActiveView();
            _activeActionIndex = NoSelection;

            // No navigate cue: nothing moved. The item is named as part of the opening announcement
            // rather than being stepped onto.
            var item = _items[targetIndex];
            var displayText = item.GetDisplayText();

            // Clear whatever is still being said (a settings confirmation, "Language set to ...") so the
            // new screen is heard from its first word rather than replacing it halfway through. Screens
            // that asked for interruptable speech keep their own flag.
            var flag = ResolveTitleSpeakFlag();
            if (flag == SpeechService.SpeakFlag.NoInterrupt)
                flag = SpeechService.SpeakFlag.NoInterruptButStop;
            _speech.Speak($"{EndOpeningSentence(opening)}  {displayText}", flag);
            ScheduleHint(item, targetIndex, displayText);
            _justEntered = false;
            ClearAutoFocusPending();
            return true;
        }

        // Punctuation is what tells a screen reader where to break. Without it the opening text runs
        // straight into the item name as one clause ("Main menu Race" rather than "Main menu. Race"),
        // which is the whole reason the two used to be separate utterances. A full stop keeps them
        // apart while they stay a single Speak call; text that already ends in punctuation is left
        // alone so nothing is said twice or read out as a stray character.
        private const string OpeningSentenceEndings = ".!?:;,\u2026\u3002\uFF01\uFF1F\uFF1A\uFF1B\u3001\uFF0C\u061F\u060C";

        private static string EndOpeningSentence(string opening)
        {
            var trimmed = opening.TrimEnd();
            if (trimmed.Length == 0)
                return trimmed;

            return OpeningSentenceEndings.IndexOf(trimmed[trimmed.Length - 1]) >= 0
                ? trimmed
                : trimmed + ".";
        }

        private int TakeFirstFocusIndex()
        {
            var targetIndex = 0;
            if (_pendingFocusIndex.HasValue)
                targetIndex = Math.Max(0, Math.Min(_items.Count - 1, _pendingFocusIndex.Value));
            else if (ActiveView.KeepSelection && ActiveView.SavedSelection != NoSelection)
                targetIndex = Math.Max(0, Math.Min(_items.Count - 1, ActiveView.SavedSelection));
            _pendingFocusIndex = null;
            return targetIndex;
        }

        private bool SwitchScreen(int delta)
        {
            if (_views.Count <= 1)
                return false;

            SaveSelectionForActiveView();
            _viewIndex = (_viewIndex + delta + _views.Count) % _views.Count;
            LoadActiveViewItems();
            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = null;
            _justEntered = true;
            QueueAutoFocusFirstItem();
            CancelHint();
            _titlePending = false;
            AnnounceTitle();
            return true;
        }

        private void SaveSelectionForActiveView()
        {
            if (!ActiveView.KeepSelection)
                return;

            ActiveView.SavedSelection = _index;
        }

        private void RefreshActiveViewItems(bool preserveSelection)
        {
            var previousIndex = _index;
            var hadSelection = previousIndex != NoSelection;
            SaveSelectionForActiveView();
            LoadActiveViewItems();
            CancelHint();

            if (preserveSelection && hadSelection && _items.Count > 0)
            {
                _index = Math.Max(0, Math.Min(previousIndex, _items.Count - 1));
                SaveSelectionForActiveView();
                _activeActionIndex = NoSelection;
                _pendingFocusIndex = null;
                _justEntered = false;
                ClearAutoFocusPending();
                return;
            }

            if (ActiveView.KeepSelection && ActiveView.SavedSelection != NoSelection && _items.Count > 0)
            {
                _index = Math.Max(0, Math.Min(ActiveView.SavedSelection, _items.Count - 1));
                SaveSelectionForActiveView();
                _activeActionIndex = NoSelection;
                _pendingFocusIndex = null;
                _justEntered = false;
                ClearAutoFocusPending();
                return;
            }

            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = null;
            _justEntered = true;
            QueueAutoFocusFirstItem();
        }
    }
}

